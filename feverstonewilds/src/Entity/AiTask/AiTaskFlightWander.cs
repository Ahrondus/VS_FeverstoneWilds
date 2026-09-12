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
    private readonly int waypointSearchAttempts;
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
    private bool hasWaypoint;
    private long hoverUntilMs;

    public AiTaskFlightWander(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig) : base(entity, taskConfig, aiConfig)
    {
        flight = entity.GetBehavior<BehaviorFlight>();
        horizontalRange = taskConfig["horizontalRange"].AsFloat(12f);
        verticalRange = taskConfig["verticalRange"].AsFloat(5f);
        waypointSearchAttempts = Math.Max(1, taskConfig["waypointSearchAttempts"].AsInt(6));
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
        hasWaypoint = TrySetNextFlightTarget();
    }

    public override bool ContinueExecute(float dt)
    {
        if (!flight.IsFlying || flight.IsLanding || flight.IsAttacking || !entity.Alive) return false;
        if (!hasWaypoint) return false;

        if (isHovering)
        {
            if (entity.World.ElapsedMilliseconds < hoverUntilMs) return true;

            isHovering = false;
            if (waypointsReached >= waypointCount) return false;

            SetTravelAnimation();
            hasWaypoint = TrySetNextFlightTarget();
            return hasWaypoint;
        }

        if (!flight.HasReachedTarget) return true;

        waypointsReached++;
        if (string.IsNullOrEmpty(idleAnimation) || idleDurationMs <= 0)
        {
            if (waypointsReached >= waypointCount) return false;

            hasWaypoint = TrySetNextFlightTarget();
            return hasWaypoint;
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

    private bool TrySetNextFlightTarget()
    {
        for (int attempt = 0; attempt < waypointSearchAttempts; attempt++)
        {
            double angle = entity.World.Rand.NextDouble() * GameMath.TWOPI;
            double distance = horizontalRange * (0.5 + entity.World.Rand.NextDouble() * 0.5);
            double y = entity.Pos.Y + (entity.World.Rand.NextDouble() * 2 - 1) * verticalRange;
            Vec3d waypoint = new(entity.Pos.X + Math.Cos(angle) * distance, y, entity.Pos.Z + Math.Sin(angle) * distance);

            if (!IsFlightPathClear(waypoint)) continue;

            flight.SetFlightTarget(waypoint);
            return true;
        }

        return false;
    }

    private bool IsFlightPathClear(Vec3d waypoint)
    {
        Vec3d delta = waypoint.SubCopy(entity.Pos.XYZ);
        int steps = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z)));

        for (int step = 1; step <= steps; step++)
        {
            double progress = step / (double)steps;
            Vec3d position = new(entity.Pos.X + delta.X * progress, entity.Pos.Y + delta.Y * progress, entity.Pos.Z + delta.Z * progress);
            if (!IsFlightSpaceClear(position)) return false;
        }

        return true;
    }

    private bool IsFlightSpaceClear(Vec3d position)
    {
        BlockPos blockPos = entity.Pos.AsBlockPos.Copy();
        int clearanceBlocks = Math.Max(1, (int)Math.Ceiling(entity.CollisionBox.YSize));
        int x = (int)Math.Floor(position.X);
        int y = (int)Math.Floor(position.Y);
        int z = (int)Math.Floor(position.Z);

        for (int offsetY = 0; offsetY < clearanceBlocks; offsetY++)
        {
            blockPos.Set(x, y + offsetY, z);
            Block block = entity.World.BlockAccessor.GetBlock(blockPos);
            if (block.Id != 0 || block.IsLiquid()) return false;
        }

        return true;
    }

    private void SetTravelAnimation()
    {
        flight.SetFlightAnimation("flightwander", animation, animationSpeed);
    }
}
