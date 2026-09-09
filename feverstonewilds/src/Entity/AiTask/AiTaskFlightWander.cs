using System;
using FeverstoneWilds.Flight.Behavior;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace FeverstoneWilds.Flight.AiTask;

// Chooses short aerial destinations while flight is active.
public class AiTaskFlightWander : AiTaskBase
{
    private readonly BehaviorFlight flight;
    private readonly float horizontalRange;
    private readonly float verticalRange;

    public AiTaskFlightWander(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig) : base(entity, taskConfig, aiConfig)
    {
        flight = entity.GetBehavior<BehaviorFlight>();
        horizontalRange = taskConfig["horizontalRange"].AsFloat(12f);
        verticalRange = taskConfig["verticalRange"].AsFloat(5f);
    }

    public override bool ShouldExecute() => flight != null && flight.IsFlying && !flight.IsLanding;

    public override void StartExecute()
    {
        base.StartExecute();

        double angle = entity.World.Rand.NextDouble() * GameMath.TWOPI;
        double distance = horizontalRange * (0.5 + entity.World.Rand.NextDouble() * 0.5);
        double y = entity.Pos.Y + (entity.World.Rand.NextDouble() * 2 - 1) * verticalRange;
        flight.SetFlightTarget(new Vec3d(entity.Pos.X + Math.Cos(angle) * distance, y, entity.Pos.Z + Math.Sin(angle) * distance));
    }

    public override bool ContinueExecute(float dt) => flight.IsFlying && !flight.IsLanding && !flight.HasReachedTarget;

    public override void FinishExecute(bool cancelled)
    {
        flight?.ClearFlightTarget();
        base.FinishExecute(cancelled);
    }
}
