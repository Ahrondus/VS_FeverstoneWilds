using FeverstoneWilds.Flight.Behavior;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using System;

namespace FeverstoneWilds.Flight.AiTask;

// Supplies three-dimensional movement toward the task's target while flight is active.
public class AiTaskFlightSeekEntity : AiTaskBase
{
    private readonly BehaviorFlight flight;
    private readonly float preferredAltitude;
    private readonly float seekingRange;
    private readonly string[] entityCodes;
    private Entity targetEntity;

    public AiTaskFlightSeekEntity(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig) : base(entity, taskConfig, aiConfig)
    {
        flight = entity.GetBehavior<BehaviorFlight>();
        preferredAltitude = taskConfig["preferredAltitude"].AsFloat(3f);
        seekingRange = taskConfig["seekingRange"].AsFloat(20f);
        entityCodes = taskConfig["entityCodes"].AsArray<string>(Array.Empty<string>());
    }

    public override bool ShouldExecute()
    {
        if (flight == null || !flight.IsFlying || flight.IsLanding || entityCodes.Length == 0) return false;

        targetEntity = entity.World.GetNearestEntity(entity.Pos.XYZ, seekingRange, seekingRange, candidate =>
            candidate != entity && candidate.Alive && WildcardUtil.Match(entityCodes, candidate.Code.Path)
        );

        return targetEntity != null;
    }

    public override void StartExecute()
    {
        base.StartExecute();
        UpdateFlightTarget();
    }

    public override bool ContinueExecute(float dt)
    {
        if (!flight.IsFlying || flight.IsLanding || targetEntity == null || !targetEntity.Alive) return false;

        UpdateFlightTarget();
        return true;
    }

    public override void FinishExecute(bool cancelled)
    {
        flight?.ClearFlightTarget();
        base.FinishExecute(cancelled);
    }

    private void UpdateFlightTarget()
    {
        if (targetEntity == null) return;

        flight.SetFlightTarget(targetEntity.Pos.XYZ.AddCopy(0, preferredAltitude, 0));
    }
}
