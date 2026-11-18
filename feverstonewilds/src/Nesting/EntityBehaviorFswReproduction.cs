using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace FeverstoneWilds.Nesting
{
    // Integrates with vanilla trough feeding without ever giving live birth.
    public class EntityBehaviorFswReproduction : EntityBehaviorMultiplyBase
    {
        public const string RemainingKey = "fswEggsRemaining";
        public const string NestPosKey = "fswBuiltNest";
        public NestingConfig Config { get; private set; }
        float checkTimer;
        bool? validAssets;

        public EntityBehaviorFswReproduction(Entity entity) : base(entity) { }
        public override string PropertyName() => "fsweggreproduction";
        public int Remaining => entity.WatchedAttributes.GetInt(RemainingKey);
        public bool CanBuild => Remaining > 0 || GetSaturation() >= PortionsEatenForMultiply;
        public override bool ShouldEat => Remaining == 0 && base.ShouldEat;

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);
            Config = new NestingConfig(attributes["nesting"]);
        }

        public override void OnGameTick(float dt)
        {
            if (entity.World.Side != EnumAppSide.Server || !entity.Alive) return;
            checkTimer += dt;
            if (checkTimer < 3) return;
            checkTimer = 0;
            if (!validAssets.HasValue)
            {
                validAssets = Config.AssetsExist(entity.World);
                if (!validAssets.Value) entity.World.Logger.Error("[FeverstoneWilds] Invalid nesting assets for {0}; breeding disabled.", entity.Code);
            }
            if (!validAssets.Value || !NestingRules.CanMate(entity.Alive, Remaining, GetSaturation(), PortionsEatenForMultiply,
                entity.World.Calendar.TotalDays, TotalDaysCooldownUntil)) return;

            Entity mate = entity.World.GetNearestEntity(entity.Pos.XYZ, Config.MateRange, Config.MateRange,
                e => e != entity && e.Alive && e.Code != null && WildcardUtil.Match(new AssetLocation(Config.MateCode), e.Code));
            if (mate == null) return;
            ITreeAttribute hunger = entity.WatchedAttributes.GetTreeAttribute("hunger");
            if (hunger == null) return;
            hunger.SetFloat("saturation", Math.Max(0, GetSaturation() - PortionsEatenForMultiply));
            entity.WatchedAttributes.MarkPathDirty("hunger");
            entity.WatchedAttributes.SetInt(RemainingKey, Config.ClutchSize);
            entity.WatchedAttributes.MarkPathDirty(RemainingKey);
        }

        public ItemStack MakeEgg()
        {
            if (Remaining <= 0) return null;
            Item item = entity.World.GetItem(new AssetLocation(Config.EggItem));
            if (item == null || Config.ChickCodes.Length == 0) return null;
            string chick = Config.ChickCodes[entity.World.Rand.Next(Config.ChickCodes.Length)];
            ItemStack egg = new ItemStack(item);
            TreeAttribute data = new TreeAttribute();
            data.SetString("code", chick);
            data.SetInt("generation", entity.WatchedAttributes.GetInt("generation") + 1);
            data.SetDouble("incubationDays", Config.IncubationDays);
            data.SetLong("herdID", (entity as EntityAgent)?.HerdId ?? 0);
            egg.Attributes["chick"] = data;
            return egg;
        }

        public void EggLaid()
        {
            int previous = Remaining;
            int remaining = NestingRules.RemainingAfterLay(previous, true);
            entity.WatchedAttributes.SetInt(RemainingKey, remaining);
            entity.WatchedAttributes.MarkPathDirty(RemainingKey);
            if (previous > 0 && remaining == 0)
                TotalDaysCooldownUntil = entity.World.Calendar.TotalDays + MultiplyCooldownDaysMin +
                    entity.World.Rand.NextDouble() * Math.Max(0, MultiplyCooldownDaysMax - MultiplyCooldownDaysMin);
        }
    }
}
