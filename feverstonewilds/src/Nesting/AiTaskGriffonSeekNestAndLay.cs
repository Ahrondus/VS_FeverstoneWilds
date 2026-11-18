using System;
using System.Reflection;
using FeverstoneWilds.Flight.Behavior;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace FeverstoneWilds.Nesting
{
    // Vanilla henbox laying, brooding, and ground-laying fallback.
    public class AiTaskGriffonSeekNestAndLay : AiTaskSeekBlockAndLay
    {
        static readonly Action<AiTaskSeekBlockAndLay> GroundFallback = typeof(AiTaskSeekBlockAndLay)
            .GetMethod("LayEggOnGround", BindingFlags.Instance | BindingFlags.NonPublic)
            .CreateDelegate<Action<AiTaskSeekBlockAndLay>>();
        public AiTaskGriffonSeekNestAndLay(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
            : base(entity, taskConfig, aiConfig)
        {
            if (sitAnimMeta != null)
            {
                sitAnimMeta.Weight = 10;
                sitAnimMeta.SupressDefaultAnimation = true;
            }
        }

        public override bool ShouldExecute()
        {
            if (entity.Swimming || entity.GetInterface<IMountable>()?.IsBeingControlled() == true) return false;
            var flight = entity.GetBehavior<BehaviorFlight>();
            // Request landing only when she has eaten enough to lay.
            if ((entity.WatchedAttributes.GetTreeAttribute("hunger")?.GetFloat("saturation") ?? 0) < 3) return false;
            if (!entity.OnGround || flight?.IsFlying == true)
            {
                flight?.HoldGroundForNesting();
                return false;
            }
            if (!base.ShouldExecute()) return false;
            // Vanilla does not know about persistent clutch ownership. Search past
            // another mother's nest so a free second nest can still be selected.
            if (!AvailableNest(targetPoi))
                targetPoi = entity.Api.ModLoader.GetModSystem<POIRegistry>().GetWeightedNearestPoi(entity.Pos.XYZ, 42,
                    poi => poi is IAnimalNest nest && AvailableNest(nest)) as IAnimalNest;
            if (targetPoi == null)
            {
                // Vanilla's initial search cannot see our clutch-owner filter.
                // Reuse its exact fallback when all otherwise suitable nests are locked.
                GroundFallback(this);
                return false;
            }
            WrapTarget();
            flight?.HoldGroundForNesting();
            return true;
        }

        public override bool ContinueExecute(float dt)
        {
            entity.GetBehavior<BehaviorFlight>()?.HoldGroundForNesting();
            if (entity.Swimming || entity.GetInterface<IMountable>()?.IsBeingControlled() == true) return false;
            WrapTarget();
            var clutch = GriffonNestClutch.For(targetPoi);
            if (clutch != null)
            {
                if (!clutch.CanUse(entity)) return false;
                // Once full or partially hatched/collected, this clutch cannot be
                // topped up, even by its own mother. She may continue brooding it.
                if (!clutch.CanLay(entity)) laid = true;
            }
            if (targetPoi is GriffonClutchNest)
            {
                var surface = targetPoi.Position;
                bool nearCenter = surface.HorizontalSquareDistanceTo(entity.Pos.X, entity.Pos.Z) <= MinDistanceToTarget();
                // Vanilla only checks X/Z. Do not brood from beside/under a nest or
                // keep its occupier while being pushed away from the clutch.
                if (sitAnimStarted && (!entity.OnGround || !GriffonBrooding.OnNest(entity.Pos.XYZ, surface))) return false;
                if (nearCenter && !sitAnimStarted && !GriffonBrooding.TrySettle(entity, surface)) return false;
                if (nearCenter)
                {
                    entity.ServerControls.StopAllMovement();
                    entity.Controls.StopAllMovement();
                    entity.Pos.Motion.X = entity.Pos.Motion.Z = 0;
                }
            }
            return base.ContinueExecute(dt);
        }

        bool AvailableNest(IAnimalNest nest) => nest != null && !nest.Occupied(entity) &&
            nest.IsSuitableFor(entity, nestTypes) && (GriffonNestClutch.For(nest)?.CanUse(entity) ?? true);

        void WrapTarget()
        {
            if (targetPoi is GriffonClutchNest) return;
            var clutch = GriffonNestClutch.For(targetPoi);
            if (clutch != null) targetPoi = new GriffonClutchNest(targetPoi, clutch, entity);
        }

        public override void FinishExecute(bool cancelled)
        {
            // Vanilla always clears targetPoi's occupier on finish. A female whose
            // approach loses to another female must not release that other's nest.
            IAnimalNest nest = targetPoi;
            if (nest?.Occupied(entity) == true) targetPoi = null;
            try
            {
                base.FinishExecute(cancelled);
            }
            finally
            {
                targetPoi = nest;
            }
        }

    }
}
