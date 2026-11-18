using System;
using FeverstoneWilds.Flight.Behavior;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace FeverstoneWilds.Nesting
{
    // PaleoWilds' construction -> laying -> brooding sequence, configured per
    // creature. Clutch counters live on the mother and incubation on the nest.
    public class AiTaskFswNesting : AiTaskBase
    {
        enum Work { None, Build, Lay, Brood, GroundLay }
        Work work;
        EntityBehaviorFswReproduction reproduction;
        NestingConfig config;
        BlockPos nestPos;
        string expectedBlock;
        int constructionIndex, phase;
        float timer, travelSeconds;
        bool reached, failed;
        long nextSearchMs, buildRetryMs;
        BlockEntityFswBreedingNest nest;

        public AiTaskFswNesting(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
            : base(entity, taskConfig, aiConfig) { }

        public override bool ShouldExecute()
        {
            if (!entity.Alive || entity.Swimming || entity.GetInterface<IMountable>()?.IsBeingControlled() == true) return false;
            reproduction = entity.GetBehavior<EntityBehaviorFswReproduction>();
            if (reproduction == null || entity.World.ElapsedMilliseconds < nextSearchMs) return false;
            nextSearchMs = entity.World.ElapsedMilliseconds + 3000;
            config = reproduction.Config;
            if (config == null || !config.AssetsExist(entity.World)) return false;
            work = Work.None;
            nest = FindNest(true);
            if (nest != null) work = Work.Brood;
            else if (reproduction.CanBuild && TryPrepareBuild()) work = Work.Build;
            else if (reproduction.Remaining > 0)
            {
                nest = FindNest(false);
                work = nest != null ? Work.Lay : Work.GroundLay;
            }
            if (work == Work.None) return false;

            // A flier must land normally before ground navigation or egg laying.
            BehaviorFlight flight = entity.GetBehavior<BehaviorFlight>();
            flight?.HoldGroundForNesting();
            return entity.OnGround && (flight == null || !flight.IsFlying);
        }

        public override void StartExecute()
        {
            base.StartExecute();
            timer = travelSeconds = 0;
            reached = failed = false;
            if (work == Work.Build)
            {
                if (nest == null)
                {
                    // Recheck after selection: another female may have placed a nest.
                    if (!CanPlace(nestPos) || CountNests() >= config.MaxNests) { failed = true; return; }
                    Block first = entity.World.GetBlock(new AssetLocation(config.Construction[0]));
                    entity.World.BlockAccessor.SetBlock(first.Id, nestPos);
                    nest = entity.World.BlockAccessor.GetBlockEntity(nestPos) as BlockEntityFswBreedingNest;
                    SaveNestPosition();
                }
                if (nest == null || !nest.Claim(entity)) { failed = true; return; }
                constructionIndex = Array.IndexOf(config.Construction, nest.Block.Code.ToString());
                expectedBlock = nest.Block.Code.ToString();
                phase = 0;
                StartExcursion();
            }
            else if (work == Work.GroundLay)
            {
                nest = null;
                reached = true;
            }
            else
            {
                if (nest == null || !nest.Claim(entity)) { failed = true; return; }
                nestPos = nest.Pos.Copy();
                Navigate(nestPos.ToVec3d().Add(0.5, 0, 0.5), 1.2f);
            }
        }

        public override bool ContinueExecute(float dt)
        {
            if (failed)
            {
                if (work == Work.Build) buildRetryMs = entity.World.ElapsedMilliseconds + 30000;
                return false;
            }
            if (!entity.Alive || entity.Swimming || entity.GetInterface<IMountable>()?.IsBeingControlled() == true) return false;
            entity.GetBehavior<BehaviorFlight>()?.HoldGroundForNesting();
            if (!entity.OnGround) return false;
            if (work != Work.GroundLay)
            {
                BlockEntityFswBreedingNest current = entity.World.BlockAccessor.GetBlockEntity(nestPos) as BlockEntityFswBreedingNest;
                if (current != nest || !config.Matches(current?.Block) || !nest.Claim(entity)) return false;
            }
            if (work == Work.Build) return ContinueBuild(dt);

            if (work != Work.GroundLay && !AtNest())
            {
                travelSeconds += dt;
                return !reached && travelSeconds < 30;
            }
            pathTraverser.Stop();
            entity.ServerControls.StopAllMovement();
            if (nestPos != null && work != Work.GroundLay) FaceNest();
            entity.AnimManager?.StartAnimation(config.BroodAnimation);
            if (work == Work.Brood)
            {
                if (!nest.Ready || !nest.Full) return false;
                nest.Brood(entity);
                return true;
            }

            if (reproduction.Remaining <= 0 || (nest != null && (!nest.Ready || nest.Full))) return false;
            timer += dt;
            if (timer < config.LaySeconds) return true;
            timer = 0;
            ItemStack egg = reproduction.MakeEgg();
            bool laid = egg != null && (work == Work.GroundLay ? SpawnGroundEgg(egg) : nest.TryAddEgg(egg));
            if (!laid) return false;
            reproduction.EggLaid();
            return reproduction.Remaining > 0 && (nest == null || !nest.Full);
        }

        bool ContinueBuild(float dt)
        {
            if (constructionIndex < 0 || nest.Block.Code.ToString() != expectedBlock) return false;
            travelSeconds += dt;
            if (travelSeconds > 30) { buildRetryMs = entity.World.ElapsedMilliseconds + 30000; return false; }
            if (phase == 0)
            {
                if (!reached) return true;
                phase = 1;
                Navigate(nestPos.ToVec3d().Add(0.5, 0, 0.5), 1.2f);
                return true;
            }
            if (phase == 1)
            {
                if (!AtNest()) return !reached;
                phase = 2;
                pathTraverser.Stop();
                entity.ServerControls.StopAllMovement();
                entity.AnimManager?.StopAnimation(config.SearchAnimation);
            }
            FaceNest();
            entity.AnimManager?.StartAnimation(config.DigAnimation);
            timer += dt;
            if (timer < config.DigSeconds) return true;
            entity.AnimManager?.StopAnimation(config.DigAnimation);
            constructionIndex++;
            string nextCode = constructionIndex < config.Construction.Length ? config.Construction[constructionIndex] : config.ReadyNest;
            Block next = entity.World.GetBlock(new AssetLocation(nextCode));
            if (next == null || next.Id == 0) return false;
            // Exchange keeps the inventory/POI instance through construction stages.
            entity.World.BlockAccessor.ExchangeBlock(next.Id, nestPos);
            nest.Block = next;
            nest.MarkDirty(true);
            expectedBlock = nextCode;
            if (constructionIndex >= config.Construction.Length) return false;
            phase = 0;
            StartExcursion();
            return true;
        }

        public override void FinishExecute(bool cancelled)
        {
            base.FinishExecute(cancelled);
            pathTraverser.Stop();
            entity.ServerControls.StopAllMovement();
            if (config != null)
            {
                entity.AnimManager?.StopAnimation(config.BroodAnimation);
                entity.AnimManager?.StopAnimation(config.DigAnimation);
                entity.AnimManager?.StopAnimation(config.SearchAnimation);
            }
            nest?.Release(entity);
            nextSearchMs = entity.World.ElapsedMilliseconds + 3000;
        }

        BlockEntityFswBreedingNest FindNest(bool full)
        {
            BlockPos own = LoadNestPosition();
            BlockEntityFswBreedingNest preferred = SuitableNest(own, full);
            if (preferred != null) return preferred;
            BlockEntityFswBreedingNest best = null;
            double bestDistance = double.MaxValue;
            Scan(pos =>
            {
                BlockEntityFswBreedingNest candidate = SuitableNest(pos, full);
                if (candidate == null) return;
                double distance = candidate.Position.SquareDistanceTo(entity.Pos.XYZ);
                if (distance < bestDistance) { best = candidate; bestDistance = distance; }
            });
            return best;
        }

        BlockEntityFswBreedingNest SuitableNest(BlockPos pos, bool full)
        {
            if (pos == null || pos.dimension != entity.Pos.Dimension) return null;
            var candidate = entity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityFswBreedingNest;
            if (candidate == null || candidate.Block.Code.ToString() != config.ReadyNest || !candidate.Ready ||
                candidate.Full != full || !candidate.AvailableTo(entity)) return null;
            if (full)
            {
                bool fertile = false;
                foreach (ItemSlot slot in candidate.Inventory)
                    if (slot.Itemstack?.Attributes.GetTreeAttribute("chick")?.GetString("code") != null) fertile = true;
                if (!fertile) return null;
            }
            return candidate;
        }

        bool TryPrepareBuild()
        {
            nest = null;
            if (config.Construction.Length == 0 || entity.World.ElapsedMilliseconds < buildRetryMs) return false;
            BlockPos own = LoadNestPosition();
            if (own != null)
            {
                Block current = entity.World.BlockAccessor.GetBlock(own);
                if (config.Matches(current))
                {
                    if (current.Code.ToString() == config.ReadyNest) return false;
                    nest = entity.World.BlockAccessor.GetBlockEntity(own) as BlockEntityFswBreedingNest;
                    if (nest == null || !nest.AvailableTo(entity)) return false;
                    nestPos = own;
                    return true;
                }
            }
            if (CountNests() >= config.MaxNests) return false;
            BlockPos origin = entity.Pos.AsBlockPos;
            for (int radius = 2; radius <= 5; radius++)
            for (int i = 0; i < 16; i++)
            {
                double angle = entity.World.Rand.NextDouble() * Math.PI * 2;
                for (int y = origin.Y + 2; y >= origin.Y - 3; y--)
                {
                    BlockPos pos = new BlockPos(origin.X + (int)Math.Round(Math.Cos(angle) * radius), y,
                        origin.Z + (int)Math.Round(Math.Sin(angle) * radius), entity.Pos.Dimension);
                    if (!CanPlace(pos)) continue;
                    nestPos = pos;
                    return true;
                }
            }
            buildRetryMs = entity.World.ElapsedMilliseconds + 30000;
            return false;
        }

        bool CanPlace(BlockPos pos)
        {
            var accessor = entity.World.BlockAccessor;
            // Do not replace player blocks, crops or fluids while building.
            return accessor.GetBlock(pos).Id == 0 && accessor.GetBlock(pos.UpCopy()).Id == 0 &&
                accessor.GetBlock(pos, BlockLayersAccess.Fluid).Id == 0 &&
                accessor.GetBlock(pos.DownCopy()).SideSolid[BlockFacing.UP.Index];
        }

        int CountNests()
        {
            int count = 0;
            Scan(pos => { if (config.Matches(entity.World.BlockAccessor.GetBlock(pos))) count++; });
            return count;
        }

        void Scan(Action<BlockPos> action)
        {
            BlockPos origin = entity.Pos.AsBlockPos;
            for (int x = origin.X - config.SearchRadius; x <= origin.X + config.SearchRadius; x++)
            for (int y = origin.Y - 3; y <= origin.Y + 3; y++)
            for (int z = origin.Z - config.SearchRadius; z <= origin.Z + config.SearchRadius; z++)
                action(new BlockPos(x, y, z, entity.Pos.Dimension));
        }

        void SaveNestPosition()
        {
            TreeAttribute position = new TreeAttribute();
            position.SetInt("x", nestPos.X); position.SetInt("y", nestPos.Y); position.SetInt("z", nestPos.Z);
            position.SetInt("dimension", nestPos.dimension);
            entity.WatchedAttributes[EntityBehaviorFswReproduction.NestPosKey] = position;
            entity.WatchedAttributes.MarkPathDirty(EntityBehaviorFswReproduction.NestPosKey);
        }

        BlockPos LoadNestPosition()
        {
            ITreeAttribute position = entity.WatchedAttributes.GetTreeAttribute(EntityBehaviorFswReproduction.NestPosKey);
            if (position == null || position.GetInt("dimension") != entity.Pos.Dimension) return null;
            return new BlockPos(position.GetInt("x"), position.GetInt("y"), position.GetInt("z"), position.GetInt("dimension"));
        }

        void StartExcursion()
        {
            timer = 0;
            double distance = config.ExcursionMin + entity.World.Rand.NextDouble() * (config.ExcursionMax - config.ExcursionMin);
            double angle = entity.World.Rand.NextDouble() * Math.PI * 2;
            entity.AnimManager?.StartAnimation(config.SearchAnimation);
            Navigate(new Vec3d(nestPos.X + 0.5 + Math.Cos(angle) * distance, nestPos.Y,
                nestPos.Z + 0.5 + Math.Sin(angle) * distance), 0.7f);
        }

        void Navigate(Vec3d target, float tolerance)
        {
            reached = failed = false;
            travelSeconds = 0;
            pathTraverser.NavigateTo_Async(target, config.MoveSpeed, tolerance,
                () => { reached = true; }, () => { failed = true; });
        }

        bool AtNest() => NestingRules.WithinReach(entity.Pos.X - nestPos.X - 0.5,
            entity.Pos.Y - nestPos.Y, entity.Pos.Z - nestPos.Z - 0.5, 1.55);

        void FaceNest() => entity.Pos.Yaw = (float)Math.Atan2(nestPos.X + 0.5 - entity.Pos.X,
            nestPos.Z + 0.5 - entity.Pos.Z);

        bool SpawnGroundEgg(ItemStack stack)
        {
            EntityProperties type = entity.World.GetEntityType(new AssetLocation(config.EggEntity));
            if (type == null) return false;
            Entity egg = entity.World.ClassRegistry.CreateEntity(type);
            if (egg == null) return false;
            int index = config.ClutchSize - reproduction.Remaining;
            double angle = index * Math.PI * 2 / config.ClutchSize;
            egg.Pos.SetFrom(entity.Pos);
            egg.Pos.SetPos(entity.Pos.X + Math.Cos(angle) * 0.45, entity.Pos.Y + 0.05,
                entity.Pos.Z + Math.Sin(angle) * 0.45);
            egg.Pos.Motion.Set(0, 0, 0);

            egg.WatchedAttributes[EntityBehaviorFswEgg.ChickKey] = stack.Attributes["chick"].Clone();
            entity.World.SpawnEntity(egg);
            return true;
        }
    }
}
