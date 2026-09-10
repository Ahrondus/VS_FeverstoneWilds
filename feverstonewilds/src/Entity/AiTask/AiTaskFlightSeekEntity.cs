using FeverstoneWilds.Flight.Behavior;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using System;
using Vintagestory.GameContent;

namespace FeverstoneWilds.Flight.AiTask;

// Supplies three-dimensional movement toward the task's target while flight is active.
public class AiTaskFlightSeekEntity : AiTaskBaseTargetable
{
    private readonly BehaviorFlight flight;
    private readonly float preferredAltitude;
    private readonly float seekingRange;
    private readonly string animation;
    private readonly float animationSpeed;

    public AiTaskFlightSeekEntity(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig) : base(entity, taskConfig, aiConfig)
    {
        flight = entity.GetBehavior<BehaviorFlight>();
        preferredAltitude = taskConfig["preferredAltitude"].AsFloat(3f);
        seekingRange = taskConfig["seekingRange"].AsFloat(20f);
        animation = taskConfig["animation"].AsString(null);
        animationSpeed = taskConfig["animationSpeed"].AsFloat(1f);
    }

    public override bool ShouldExecute()
    {
        if (!PreconditionsSatisfied()) return false;
        if (flight == null || !flight.IsFlying || flight.IsLanding || flight.IsAttacking || !entity.Alive) return false;

        targetEntity = entity.World.GetNearestEntity(entity.Pos.XYZ, seekingRange, seekingRange, candidate =>
            IsTargetableEntity(candidate, seekingRange)
        );

        return targetEntity != null;
    }

    public override void StartExecute()
    {
        base.StartExecute();
        flight?.SetFlightAnimation("flightseekentity", animation, animationSpeed);
        UpdateFlightTarget();
    }

    public override bool ContinueExecute(float dt)
    {
        if (!flight.IsFlying || flight.IsLanding || flight.IsAttacking || targetEntity == null || !targetEntity.Alive || !entity.Alive) return false;

        UpdateFlightTarget();
        return true;
    }

    public override void FinishExecute(bool cancelled)
    {
        flight?.ClearFlightTarget();
        flight?.ClearFlightAnimation("flightseekentity");
        base.FinishExecute(cancelled);
    }

    private void UpdateFlightTarget()
    {
        if (targetEntity == null) return;

        flight.SetFlightTarget(targetEntity.Pos.XYZ.AddCopy(0, preferredAltitude, 0));
    }
}
