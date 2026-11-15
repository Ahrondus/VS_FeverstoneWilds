using System;
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
        string eggItem, brokenEntity;
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
            eggItem = attributes["eggItem"].AsString();
            brokenEntity = attributes["brokenEntity"].AsString();
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
        }

        public override void OnGameTick(float dt)
        {
            if (transforming || !entity.Alive || entity.World.Side != EnumAppSide.Server) return;
            checkTimer += dt;
            if (checkTimer < 1) return;
            checkTimer = 0;
            double elapsed = Math.Max(0, entity.World.Calendar.TotalHours - entity.WatchedAttributes.GetDouble(StartKey));
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

        public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition,
            EnumInteractMode mode, ref EnumHandling handled)
        {
            if (mode != EnumInteractMode.Interact || transforming || !entity.Alive || !(byEntity is EntityPlayer player)) return;
            handled = EnumHandling.PreventDefault;
            if (entity.World.Side != EnumAppSide.Server || player.Player == null) return;
            Item item = string.IsNullOrEmpty(eggItem) ? null : entity.World.GetItem(new AssetLocation(eggItem));
            if (item == null) return;
            transforming = true;
            ItemStack stack = new ItemStack(item);
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
            entity.Die(EnumDespawnReason.Removed);
            return true;
        }
    }
}
