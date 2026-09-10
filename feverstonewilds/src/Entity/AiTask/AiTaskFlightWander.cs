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
    private readonly int waypointCount;
    private readonly string animation;
    private readonly float animationSpeed;
    private readonly string idleAnimation;
    private readonly float idleAnimationSpeed;
    private readonly float idleDurationMs;
    private readonly float idleAnimationEaseInSpeed;
    private readonly float idleAnimationEaseOutSpeed;
    private int waypointsReached;
    private bool isHovering;
    private long hoverUntilMs;

    public AiTaskFlightWander(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig) : base(entity, taskConfig, aiConfig)
    {
        flight = entity.GetBehavior<BehaviorFlight>();
        horizontalRange = taskConfig["horizontalRange"].AsFloat(12f);
        verticalRange = taskConfig["verticalRange"].AsFloat(5f);
        waypointCount = Math.Max(1, taskConfig["waypointCount"].AsInt(1));
        animation = taskConfig["animation"].AsString(null);
        animationSpeed = taskConfig["animationSpeed"].AsFloat(1f);
        idleAnimation = taskConfig["idleAnimation"].AsString(null);
        idleAnimationSpeed = taskConfig["idleAnimationSpeed"].AsFloat(1f);
        idleDurationMs = taskConfig["idleDurationMs"].AsFloat(0f);
        idleAnimationEaseInSpeed = taskConfig["idleAnimationEaseInSpeed"].AsFloat(6f);
        idleAnimationEaseOutSpeed = taskConfig["idleAnimationEaseOutSpeed"].AsFloat(6f);
    }

    public override bool ShouldExecute() => PreconditionsSatisfied() && flight != null && flight.IsFlying && !flight.IsLanding && !flight.IsAttacking && entity.Alive;

    public override void StartExecute()
    {
        base.StartExecute();

        waypointsReached = 0;
        isHovering = false;
        SetTravelAnimation();
        SetNextFlightTarget();
    }

    public override bool ContinueExecute(float dt)
    {
        if (!flight.IsFlying || flight.IsLanding || flight.IsAttacking || !entity.Alive) return false;

        if (isHovering)
        {
            if (entity.World.ElapsedMilliseconds < hoverUntilMs) return true;

            isHovering = false;
            if (waypointsReached >= waypointCount) return false;

            SetTravelAnimation();
            SetNextFlightTarget();
            return true;
        }

        if (!flight.HasReachedTarget) return true;

        waypointsReached++;
        if (string.IsNullOrEmpty(idleAnimation) || idleDurationMs <= 0)
        {
            if (waypointsReached >= waypointCount) return false;

            SetNextFlightTarget();
            return true;
        }

        isHovering = true;
        hoverUntilMs = entity.World.ElapsedMilliseconds + (long)idleDurationMs;
        flight.StopFlightMotion();
        flight.SetFlightAnimation("flightwander", idleAnimation, idleAnimationSpeed, 10f, false, idleAnimationEaseInSpeed, idleAnimationEaseOutSpeed);
        return true;
    }

    public override void FinishExecute(bool cancelled)
    {
        flight?.ClearFlightTarget();
        flight?.ClearFlightAnimation("flightwander");
        base.FinishExecute(cancelled);
    }

    private void SetNextFlightTarget()
    {
        double angle = entity.World.Rand.NextDouble() * GameMath.TWOPI;
        double distance = horizontalRange * (0.5 + entity.World.Rand.NextDouble() * 0.5);
        double y = entity.Pos.Y + (entity.World.Rand.NextDouble() * 2 - 1) * verticalRange;
        flight.SetFlightTarget(new Vec3d(entity.Pos.X + Math.Cos(angle) * distance, y, entity.Pos.Z + Math.Sin(angle) * distance));
    }

    private void SetTravelAnimation()
    {
        flight.SetFlightAnimation("flightwander", animation, animationSpeed);
    }
}
