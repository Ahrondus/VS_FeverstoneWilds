using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace FeverstoneWilds.Nesting
{
    // Creature identifiers and assets come from JSON rather than species tests.
    public sealed class NestingConfig
    {
        public string NestType, ReadyNest, EggItem, EggEntity, MateCode;
        public string[] Construction, ChickCodes;
        public int ClutchSize, MaxNests, SearchRadius;
        public float MateRange, LaySeconds, DigSeconds, MoveSpeed, ExcursionMin, ExcursionMax;
        public double IncubationDays;
        public string BroodAnimation, DigAnimation, SearchAnimation;

        public NestingConfig(JsonObject config)
        {
            NestType = config["nestType"].AsString();
            ReadyNest = config["readyNest"].AsString();
            Construction = config["constructionBlocks"].AsArray<string>(Array.Empty<string>());
            ChickCodes = config["chickCodes"].AsArray<string>(Array.Empty<string>());
            EggItem = config["eggItem"].AsString();
            EggEntity = config["eggEntity"].AsString();
            MateCode = config["mateCode"].AsString();
            ClutchSize = Math.Max(1, config["clutchSize"].AsInt(3));
            MaxNests = Math.Max(1, config["maxNestsInArea"].AsInt(3));
            SearchRadius = Math.Max(2, Math.Min(32, config["nestAreaRadius"].AsInt(16)));
            MateRange = config["mateRange"].AsFloat(10);
            LaySeconds = Math.Max(0.1f, config["laySeconds"].AsFloat(5));
            DigSeconds = Math.Max(0.1f, config["digSeconds"].AsFloat(3));
            MoveSpeed = config["movespeed"].AsFloat(0.025f);
            ExcursionMin = config["minSearchDistance"].AsFloat(4);
            ExcursionMax = Math.Max(ExcursionMin, config["maxSearchDistance"].AsFloat(6));
            IncubationDays = Math.Max(0.001, config["incubationDays"].AsDouble(12));
            BroodAnimation = config["broodAnimation"].AsString("idle");
            DigAnimation = config["digAnimation"].AsString("eat");
            SearchAnimation = config["searchAnimation"].AsString("walk");
        }

        public bool Matches(Block block)
        {
            string code = block?.Code?.ToString();
            return code == ReadyNest || Array.IndexOf(Construction, code) >= 0;
        }

        public bool AssetsExist(IWorldAccessor world)
        {
            if (string.IsNullOrEmpty(NestType) || string.IsNullOrEmpty(ReadyNest) ||
                string.IsNullOrEmpty(EggItem) || string.IsNullOrEmpty(EggEntity) ||
                string.IsNullOrEmpty(MateCode) || ChickCodes.Length == 0) return false;
            Block ready = world.GetBlock(new AssetLocation(ReadyNest));
            if (ready == null || ready.Id == 0 || ready.Attributes?["quantitySlots"].AsInt() != ClutchSize) return false;
            foreach (string code in Construction)
            {
                Block block = world.GetBlock(new AssetLocation(code));
                if (block == null || block.Id == 0) return false;
            }
            foreach (string code in ChickCodes)
                if (world.GetEntityType(new AssetLocation(code)) == null) return false;
            return world.GetItem(new AssetLocation(EggItem)) != null && world.GetEntityType(new AssetLocation(EggEntity)) != null;
        }
    }
}
