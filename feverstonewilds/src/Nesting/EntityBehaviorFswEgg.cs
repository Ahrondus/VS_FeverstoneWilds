using System;
using System.Text;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace FeverstoneWilds.Nesting
{
    // Configurable equivalent of PaleoWilds' canonical loose raptor egg.
    public class EntityBehaviorFswEgg : EntityBehavior
    {
        public const string ChickKey = "fswEggChick";
        public const string StartKey = "fswEggStartHours";
        public const string ProgressKey = "fswEggIncubatedHours";
        public const string HeatKey = "fswEggHasHeat";
        bool requiresHeat, wasWarm;
        int heatRadius;
        double lastUpdateHours;
        string eggItem, brokenEntity;
        string hatchingSound;
        string[] stages, chicks;
        double incubationHours;
        int stageIndex;
        float checkTimer;
        bool transforming;

        public EntityBehaviorFswEgg(Entity entity) : base(entity) { }
        public override string PropertyName() => "fswegg";

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);
            requiresHeat = attributes["requiresHeat"].AsBool(false);
            heatRadius = Math.Clamp(attributes["heatRadius"].AsInt(3), 1, 8);
            eggItem = attributes["eggItem"].AsString();
            brokenEntity = attributes["brokenEntity"].AsString();
            hatchingSound = attributes["hatchingSound"].AsString();
            stages = attributes["stageEntities"].AsArray<string>(Array.Empty<string>());
            chicks = attributes["chickCodes"].AsArray<string>(Array.Empty<string>());
            stageIndex = Math.Max(0, Array.IndexOf(stages, entity.Code.ToString()));
            incubationHours = Math.Max(0.024, attributes["incubationDays"].AsDouble(12) * 24);
            if (entity.World.Side == EnumAppSide.Server && !entity.WatchedAttributes.HasAttribute(StartKey))
            {
                // Existing hatching-stage eggs keep their visible progress.
                entity.WatchedAttributes.SetDouble(StartKey, entity.World.Calendar.TotalHours -
                    incubationHours * stageIndex / Math.Max(1, stages.Length));
                entity.WatchedAttributes.MarkPathDirty(StartKey);
            }
            if (requiresHeat && entity.World.Side == EnumAppSide.Server)
            {
                lastUpdateHours = entity.World.Calendar.TotalHours;
                if (!entity.WatchedAttributes.HasAttribute(ProgressKey))
                    entity.WatchedAttributes.SetDouble(ProgressKey, Math.Max(0, lastUpdateHours - entity.WatchedAttributes.GetDouble(StartKey)));
                wasWarm = EggHeat.HasNearbyHeat(entity.World, entity.Pos.AsBlockPos, heatRadius);
                entity.WatchedAttributes.SetBool(HeatKey, wasWarm);
            }
        }

        public override void OnEntitySpawn()
        {
            if (stageIndex > 0) PlayHatchingSound();
        }

        void PlayHatchingSound()
        {
            if (entity.World.Side == EnumAppSide.Server && !string.IsNullOrEmpty(hatchingSound))
                entity.World.PlaySoundAt(new AssetLocation(hatchingSound), entity.Pos.X, entity.Pos.Y,
                    entity.Pos.Z, null, true, 12, 0.4f);
        }

        public override void OnGameTick(float dt)
        {
            if (transforming || !entity.Alive || entity.World.Side != EnumAppSide.Server) return;
            checkTimer += dt;
            if (checkTimer < 1) return;
            checkTimer = 0;
            if (requiresHeat) UpdateWarmProgress();
            if (requiresHeat && !entity.WatchedAttributes.GetBool(HeatKey)) return;
            double elapsed = IncubatedHours();
            if (elapsed >= incubationHours)
            {
                ITreeAttribute data = entity.WatchedAttributes.GetTreeAttribute(ChickKey);
                string chick = data?.GetString("code") ?? (chicks.Length == 0 ? null : chicks[entity.World.Rand.Next(chicks.Length)]);
                Replace(chick, true);
                return;
            }
            int next = Math.Min(stages.Length - 1, (int)(elapsed / incubationHours * stages.Length));
            if (next > stageIndex) Replace(stages[next], false);
        }

        void UpdateWarmProgress()
        {
            double now = entity.World.Calendar.TotalHours;
            bool warm = EggHeat.HasNearbyHeat(entity.World, entity.Pos.AsBlockPos, heatRadius);
            double progress = EggHeat.Advance(entity.WatchedAttributes.GetDouble(ProgressKey), now - lastUpdateHours, wasWarm, warm);
            entity.WatchedAttributes.SetDouble(ProgressKey, progress);
            if (entity.WatchedAttributes.GetBool(HeatKey) != warm)
            {
                entity.WatchedAttributes.SetBool(HeatKey, warm);
                entity.WatchedAttributes.MarkPathDirty(HeatKey);
            }
            lastUpdateHours = now;
            wasWarm = warm;
        }

        double IncubatedHours() => requiresHeat ? entity.WatchedAttributes.GetDouble(ProgressKey)
            : Math.Max(0, entity.World.Calendar.TotalHours - entity.WatchedAttributes.GetDouble(StartKey));

        public override void GetInfoText(StringBuilder infotext)
        {
            base.GetInfoText(infotext);
            if (requiresHeat && entity.Alive && !entity.WatchedAttributes.GetBool(HeatKey))
                infotext.AppendLine(Lang.Get("feverstonewilds:egg-needs-heat-to-hatch"));
        }

        public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition,
            EnumInteractMode mode, ref EnumHandling handled)
        {
            if (mode != EnumInteractMode.Interact || transforming || !entity.Alive || !(byEntity is EntityPlayer player)) return;
            handled = EnumHandling.PreventDefault;
            if (entity.World.Side != EnumAppSide.Server || player.Player == null) return;
            Item item = string.IsNullOrEmpty(eggItem) ? null : entity.World.GetItem(new AssetLocation(eggItem));
            if (item == null) return;
            if (requiresHeat) UpdateWarmProgress();
            transforming = true;
            ItemStack stack = new ItemStack(item);
            stack.Attributes.SetDouble(ProgressKey, IncubatedHours());
            IAttribute chick = entity.WatchedAttributes[ChickKey];
            if (chick != null) stack.Attributes["chick"] = chick.Clone();
            bool given = player.Player.InventoryManager.TryGiveItemstack(stack, true);
            if (!given && stack.StackSize > 0) entity.World.SpawnItemEntity(stack, entity.Pos.XYZ.Add(0, 0.2, 0));
            entity.Die(EnumDespawnReason.Removed);
        }

        public override void OnEntityReceiveDamage(DamageSource source, ref float damage)
        {
            if (transforming) { damage = 0; return; }
            if (damage > 0 && entity.World.Side == EnumAppSide.Server && Replace(brokenEntity, false)) damage = 0;
        }

        bool Replace(string code, bool hatching)
        {
            if (string.IsNullOrEmpty(code)) return false;
            EntityProperties type = entity.World.GetEntityType(new AssetLocation(code));
            if (type == null) return false;
            Entity replacement = entity.World.ClassRegistry.CreateEntity(type);
            if (replacement == null) return false;
            replacement.Pos.SetFrom(entity.Pos);
            replacement.Pos.Motion.Set(0, 0, 0);

            replacement.WatchedAttributes.SetDouble(StartKey, entity.WatchedAttributes.GetDouble(StartKey));
            if (requiresHeat) replacement.WatchedAttributes.SetDouble(ProgressKey, IncubatedHours());
            ITreeAttribute chick = entity.WatchedAttributes.GetTreeAttribute(ChickKey);
            if (chick != null) replacement.WatchedAttributes[ChickKey] = chick.Clone();
            if (hatching)
            {
                replacement.Attributes.SetString("origin", "reproduction");
                replacement.WatchedAttributes.SetInt("generation", chick?.GetInt("generation") ?? 0);
                if (replacement is EntityAgent agent) agent.HerdId = chick?.GetLong("herdID") ?? 0;
            }
            transforming = true;
            entity.World.SpawnEntity(replacement);
            if (hatching) PlayHatchingSound();
            entity.Die(EnumDespawnReason.Removed);
            return true;
        }
    }
}
