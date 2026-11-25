using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

public class BehaviorPlantSapling : EntityBehavior
{

    private float timer;
    private const float Interval = 600f;
    private const int SurveyRadius = 24;
    private const int SurveyVerticalRange = 6;
    private const int MaxTreeColumns = 16;
    private const int MinimumTreeSpacing = 6;
    private Dictionary<string, List<Block>> saplingsBySpecies;
    public BehaviorPlantSapling(Entity entity) : base(entity) { }

    public override string PropertyName() => "plantSapling";

    public override void OnGameTick(float deltaTime)
    {
        if (entity.World.Side != EnumAppSide.Server || !entity.Alive) return;

        timer += deltaTime;
        if (timer >= Interval)
        {
            timer = 0f;
            TryPlant();
        }
    }
    public void OnReceivedServerPacket(int packetid)
    {
        if (packetid != 1515) return;

        if (entity is EntityAgent agent)
        {
            agent.AnimManager?.StartAnimation(new AnimationMetaData()
            {
                Animation = "Plant",
                Code = "plant",
                BlendMode = EnumAnimationBlendMode.Average,
                EaseInSpeed = 6f,
                EaseOutSpeed = 6f,
                AnimationSpeed = 1f
            });
        }
    }

    private void TryPlant()
    {
        BlockPos plantPos = entity.Pos.AsBlockPos.Copy();
        BlockPos groundPos = plantPos.DownCopy();
        IBlockAccessor ba = entity.World.BlockAccessor;

        if (!ba.IsValidPos(groundPos) || !ba.IsValidPos(plantPos)
            || ba.GetChunkAtBlockPos(groundPos) == null || ba.GetChunkAtBlockPos(plantPos) == null) return;

        // Default access checks both solid and fluid layers: never overwrite plants or water.
        if (!IsNaturalSoil(ba.GetBlock(groundPos)) || ba.GetBlock(plantPos).Id != 0
            || ba.GetBlock(groundPos, BlockLayersAccess.Fluid).Id != 0) return;

        if (!TrySurveyForest(ba, plantPos, out Dictionary<string, int> speciesCounts)) return;

        Block sapling = SelectSapling(speciesCounts);
        if (sapling == null) return;

        ba.SetBlock(sapling.Id, plantPos);
        (entity.Api as ICoreServerAPI)?.Network.BroadcastEntityPacket(entity.EntityId, 1515, null);
    }

    private static bool IsNaturalSoil(Block block)
    {
        // An allowlist also excludes farmland, paths, chiseled blocks and future masonry types.
        if (block.Code?.Domain != "game" || block.BlockMaterial != EnumBlockMaterial.Soil
            || block.Fertility <= 0) return false;

        string type = block.Code.FirstCodePart();
        return type == "soil" || type == "forestfloor";
    }

    private bool TrySurveyForest(IBlockAccessor ba, BlockPos center, out Dictionary<string, int> speciesCounts)
    {
        speciesCounts = new Dictionary<string, int>();
        int treeColumns = 0;
        int minY = Math.Max(1, center.Y - SurveyVerticalRange);
        int maxY = Math.Min(ba.MapSizeY - 1, center.Y + SurveyVerticalRange);
        BlockPos pos = center.Copy();
        BlockPos below = center.Copy();

        for (int dx = -SurveyRadius; dx <= SurveyRadius; dx++)
        {
            for (int dz = -SurveyRadius; dz <= SurveyRadius; dz++)
            {
                int distanceSquared = dx * dx + dz * dz;
                if (distanceSquared > SurveyRadius * SurveyRadius) continue;

                pos.X = below.X = center.X + dx;
                pos.Z = below.Z = center.Z + dz;
                for (int y = minY; y <= maxY; y++)
                {
                    pos.Y = y;
                    below.Y = y - 1;
                    // Do not mistake unloaded terrain for an empty forest.
                    if (!ba.IsValidPos(pos) || ba.GetChunkAtBlockPos(pos) == null
                        || ba.GetChunkAtBlockPos(below) == null) return false;

                    string species = GetTreeSpecies(ba.GetBlock(pos));
                    if (species == null || !IsNaturalSoil(ba.GetBlock(below))) continue;

                    if (distanceSquared < MinimumTreeSpacing * MinimumTreeSpacing) return false;
                    if (++treeColumns >= MaxTreeColumns) return false;

                    speciesCounts.TryGetValue(species, out int count);
                    speciesCounts[species] = count + 1;
                    // Count rooted columns, not every log up a trunk or across a canopy.
                    // Wide trunks intentionally occupy more of the planting budget.
                    break;
                }
            }
        }

        return true;
    }

    private static string GetTreeSpecies(Block block)
    {
        if (block.Code == null) return null;
        bool isSapling = IsSapling(block);
        string type = block.Code.FirstCodePart();
        string fellingGroup = block.Attributes?["treeFellingGroupCode"].AsString();
        if (!isSapling)
        {
            bool isLog = block is BlockLog || type == "log" || !string.IsNullOrEmpty(fellingGroup);
            bool isGrown = block.Variant["type"] == "grown"
                || (block.Variant["type"] == null && !string.IsNullOrEmpty(fellingGroup));
            string rotation = block.Variant["rotation"];
            if (!isLog || block.BlockMaterial != EnumBlockMaterial.Wood || !isGrown
                || (rotation != null && rotation != "ud")) return null;
        }

        string wood = block.Variant["wood"];
        if (string.IsNullOrEmpty(wood) && !isSapling) wood = fellingGroup;
        if (!string.IsNullOrEmpty(wood))
        {
            // Preserve explicit cross-domain species references; otherwise use the block's domain.
            return (wood.Contains(":") ? new AssetLocation(wood)
                : new AssetLocation(block.Code.Domain, wood)).ToString();
        }

        // A sapling without a wood variant can still propagate itself when observed nearby.
        // Normalize snow/free variants without guessing which unrelated log belongs to it.
        if (!isSapling) return null;
        AssetLocation code = block.Variant["cover"] == null ? block.Code : block.CodeWithVariant("cover", "free");
        return "sapling|" + code;
    }

    private static bool IsSapling(Block block)
    {
        return block.Code != null && (block is BlockSapling || block.Class == "BlockSapling"
            || block.EntityClass == "Sapling" || block.Code.FirstCodePart() == "sapling");
    }

    private void EnsureSaplingIndex()
    {
        if (saplingsBySpecies != null) return;
        saplingsBySpecies = new Dictionary<string, List<Block>>();
        foreach (Block block in entity.World.Blocks)
        {
            if (block == null || !IsSapling(block)) continue;
            string cover = block.Variant["cover"];
            if (cover != null && cover != "free") continue;

            string species = GetTreeSpecies(block);
            if (species == null) continue;
            if (!saplingsBySpecies.TryGetValue(species, out List<Block> saplings))
            {
                saplings = new List<Block>();
                saplingsBySpecies.Add(species, saplings);
            }
            saplings.Add(block);
        }
    }

    private Block SelectSapling(Dictionary<string, int> speciesCounts)
    {
        EnsureSaplingIndex();
        var candidates = new List<(List<Block> Saplings, int Weight)>();
        int totalWeight = 0;
        foreach (var species in speciesCounts)
        {
            if (!saplingsBySpecies.TryGetValue(species.Key, out List<Block> saplings)) continue;

            // Squaring abundance favors the dominant species but keeps local minorities possible.
            int weight = species.Value * species.Value;
            candidates.Add((saplings, weight));
            totalWeight += weight;
        }

        // Only surveyed species are eligible; an empty survey cannot introduce new species.
        if (totalWeight == 0) return null;
        int choice = entity.World.Rand.Next(totalWeight);
        foreach (var candidate in candidates)
        {
            if (choice < candidate.Weight)
            {
                // Extra asset variants must not increase a species' selection weight.
                return candidate.Saplings.Count == 1 ? candidate.Saplings[0]
                    : candidate.Saplings[entity.World.Rand.Next(candidate.Saplings.Count)];
            }
            choice -= candidate.Weight;
        }

        return null;
    }
}
