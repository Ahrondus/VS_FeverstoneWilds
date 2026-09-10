using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace FeverstoneWilds.Flight.Behavior;

/// <summary>
/// Supplies opt-in, creative-style aerial movement for FlyingEntityAgent.
/// Creature JSON controls speed and steering, so drag can be introduced later
/// without replacing the behavior or Vintage Story's collision physics.
/// </summary>
public class BehaviorFlight : EntityBehavior
{
    public const string FlyingAttribute = "feverstonewilds:isFlying";
    public const string FlightAnimationSourceAttribute = "feverstonewilds:flightAnimationSource";
    public const string FlightAnimationAttribute = "feverstonewilds:flightAnimation";
    public const string FlightAnimationSpeedAttribute = "feverstonewilds:flightAnimationSpeed";
    public const string FlightAnimationWeightAttribute = "feverstonewilds:flightAnimationWeight";
    public const string FlightAnimationEaseInSpeedAttribute = "feverstonewilds:flightAnimationEaseInSpeed";
    public const string FlightAnimationEaseOutSpeedAttribute = "feverstonewilds:flightAnimationEaseOutSpeed";
    public const string FlightAnimationSuppressDefaultAttribute = "feverstonewilds:flightAnimationSuppressDefault";

    private float flightSpeed;
    private float verticalSpeed;
    private float steering;
    private float arrivalDistance;
    private bool autoFlight;
    private float minGroundSeconds;
    private float maxGroundSeconds;
    private float minFlightSeconds;
    private float maxFlightSeconds;
    private float takeoffSpeed;
    private float landingSpeed;
    private float landingHeight;
    private float landingArrivalDistance;
    private string landingAnimation;
    private float landingAnimationSpeed;
    private float landingAnimationWeight;
    private float landingAnimationStartHeight;
    private int landingScanDepth;
    private long nextStateChangeMs;
    private Vec3d targetPosition;
    private bool hasTarget;
    private bool isLanding;
    private bool isAttacking;
    private bool landingAnimationStarted;
    private string clientAnimationSource;
    private string clientAnimation;
    private float clientAnimationSpeed;
    private float clientAnimationWeight;
    private float clientAnimationEaseInSpeed;
    private float clientAnimationEaseOutSpeed;
    private bool clientAnimationSuppressDefault;

    public BehaviorFlight(Entity entity) : base(entity) { }

    public bool IsFlying => entity.WatchedAttributes.GetBool(FlyingAttribute);

    public bool IsLanding => isLanding;

    public bool IsAttacking => isAttacking;

    public override string PropertyName() => "flight";

    public override void Initialize(EntityProperties properties, JsonObject attributes)
    {
        base.Initialize(properties, attributes);

        flightSpeed = attributes["flightSpeed"].AsFloat(0.08f);
        verticalSpeed = attributes["verticalSpeed"].AsFloat(0.06f);
        steering = attributes["steering"].AsFloat(0.2f);
        arrivalDistance = attributes["arrivalDistance"].AsFloat(1.25f);
        autoFlight = attributes["autoFlight"].AsBool(false);
        minGroundSeconds = attributes["minGroundSeconds"].AsFloat(12f);
        maxGroundSeconds = attributes["maxGroundSeconds"].AsFloat(28f);
        minFlightSeconds = attributes["minFlightSeconds"].AsFloat(10f);
        maxFlightSeconds = attributes["maxFlightSeconds"].AsFloat(20f);
        takeoffSpeed = attributes["takeoffSpeed"].AsFloat(0.06f);
        landingSpeed = attributes["landingSpeed"].AsFloat(0.035f);
        landingHeight = attributes["landingHeight"].AsFloat(0.15f);
        landingArrivalDistance = attributes["landingArrivalDistance"].AsFloat(0.05f);
        landingAnimation = attributes["landingAnimation"].AsString(null);
        landingAnimationSpeed = attributes["landingAnimationSpeed"].AsFloat(0.7f);
        landingAnimationWeight = attributes["landingAnimationWeight"].AsFloat(30f);
        landingAnimationStartHeight = attributes["landingAnimationStartHeight"].AsFloat(6f);
        landingScanDepth = attributes["landingScanDepth"].AsInt(80);
    }

    public void SetFlying(bool value)
    {
        if (entity.World.Side != EnumAppSide.Server) return;

        entity.WatchedAttributes.SetBool(FlyingAttribute, value);
        if (!value)
        {
            hasTarget = false;
            isLanding = false;
            landingAnimationStarted = false;
            ClearFlightAnimation();
        }
    }

    public void SetFlightAnimation(string source, string animation, float animationSpeed, float animationWeight = 10f, bool suppressDefaultAnimation = false, float easeInSpeed = 6f, float easeOutSpeed = 6f)
    {
        if (entity.World.Side != EnumAppSide.Server || string.IsNullOrEmpty(source) || string.IsNullOrEmpty(animation)) return;

        entity.WatchedAttributes.SetString(FlightAnimationSourceAttribute, source);
        entity.WatchedAttributes.SetString(FlightAnimationAttribute, animation);
        entity.WatchedAttributes.SetFloat(FlightAnimationSpeedAttribute, animationSpeed);
        entity.WatchedAttributes.SetFloat(FlightAnimationWeightAttribute, animationWeight);
        entity.WatchedAttributes.SetFloat(FlightAnimationEaseInSpeedAttribute, easeInSpeed);
        entity.WatchedAttributes.SetFloat(FlightAnimationEaseOutSpeedAttribute, easeOutSpeed);
        entity.WatchedAttributes.SetBool(FlightAnimationSuppressDefaultAttribute, suppressDefaultAnimation);
    }

    public void ClearFlightAnimation(string source = null)
    {
        if (entity.World.Side != EnumAppSide.Server) return;
        if (!string.IsNullOrEmpty(source) && entity.WatchedAttributes.GetString(FlightAnimationSourceAttribute) != source) return;

        entity.WatchedAttributes.SetString(FlightAnimationSourceAttribute, null);
        entity.WatchedAttributes.SetString(FlightAnimationAttribute, null);
        entity.WatchedAttributes.SetFloat(FlightAnimationSpeedAttribute, 0);
        entity.WatchedAttributes.SetFloat(FlightAnimationWeightAttribute, 0);
        entity.WatchedAttributes.SetFloat(FlightAnimationEaseInSpeedAttribute, 0);
        entity.WatchedAttributes.SetFloat(FlightAnimationEaseOutSpeedAttribute, 0);
        entity.WatchedAttributes.SetBool(FlightAnimationSuppressDefaultAttribute, false);
    }

    public void SetFlightTarget(Vec3d position)
    {
        targetPosition = position.Clone();
        hasTarget = true;
    }

    public void ClearFlightTarget() => hasTarget = false;

    public void StopFlightMotion()
    {
        if (entity.World.Side != EnumAppSide.Server) return;

        hasTarget = false;
        entity.Pos.Motion.X = 0;
        entity.Pos.Motion.Y = 0;
        entity.Pos.Motion.Z = 0;
    }

    public void BeginFlightAttack()
    {
        if (entity.World.Side != EnumAppSide.Server) return;

        isAttacking = true;
        StopFlightMotion();
    }

    public void EndFlightAttack() => isAttacking = false;

    public bool HasReachedTarget => !hasTarget || entity.Pos.XYZ.SquareDistanceTo(targetPosition) <= arrivalDistance * arrivalDistance;

    public override void OnGameTick(float deltaTime)
    {
        if (entity.World.Side == EnumAppSide.Client)
        {
            UpdateClientFlightAnimation();
            return;
        }

        UpdateAutomaticFlightState();

        if (isLanding)
        {
            UpdateLanding();
            return;
        }

        if (!IsFlying || !hasTarget) return;

        ApplyFlightMovement(verticalSpeed, arrivalDistance);
    }

    private void ApplyFlightMovement(float currentVerticalSpeed, float currentArrivalDistance)
    {

        Vec3d delta = targetPosition.SubCopy(entity.Pos.XYZ);
        double distanceSquared = delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z;
        if (distanceSquared <= currentArrivalDistance * currentArrivalDistance)
        {
            hasTarget = false;
            entity.Pos.Motion.X = 0;
            entity.Pos.Motion.Y = 0;
            entity.Pos.Motion.Z = 0;
            return;
        }

        delta.Normalize();
        UpdateFlightFacing(delta);
        entity.Pos.Motion.X += (delta.X * flightSpeed - entity.Pos.Motion.X) * steering;
        entity.Pos.Motion.Z += (delta.Z * flightSpeed - entity.Pos.Motion.Z) * steering;
        entity.Pos.Motion.Y += (delta.Y * currentVerticalSpeed - entity.Pos.Motion.Y) * steering;
    }

    private void UpdateFlightFacing(Vec3d direction)
    {
        entity.Pos.Pitch = 0;
        if (direction.X * direction.X + direction.Z * direction.Z < 0.0001) return;

        float targetYaw = (float)Math.Atan2(direction.X, direction.Z);
        float yawDelta = GameMath.AngleRadDistance(entity.Pos.Yaw, targetYaw);
        entity.Pos.Yaw = GameMath.NormaliseAngleRad(entity.Pos.Yaw + yawDelta * steering);
    }

    private void UpdateAutomaticFlightState()
    {
        if (!autoFlight) return;

        if (!entity.Alive)
        {
            if (IsFlying) SetFlying(false);
            return;
        }

        if (isLanding) return;

        long now = entity.World.ElapsedMilliseconds;

        if (IsFlying)
        {
            if (nextStateChangeMs == 0)
            {
                ScheduleNextStateChange(now, minFlightSeconds, maxFlightSeconds);
            }
            else if (now >= nextStateChangeMs)
            {
                BeginLanding();
            }

            return;
        }

        if (!entity.OnGround) return;

        if (nextStateChangeMs == 0)
        {
            ScheduleNextStateChange(now, minGroundSeconds, maxGroundSeconds);
        }
        else if (now >= nextStateChangeMs)
        {
            SetFlying(true);
            entity.Pos.Motion.Y = Math.Max(entity.Pos.Motion.Y, takeoffSpeed);
            ScheduleNextStateChange(now, minFlightSeconds, maxFlightSeconds);
        }
    }

    private void ScheduleNextStateChange(long now, float minimumSeconds, float maximumSeconds)
    {
        float minimum = Math.Max(0, minimumSeconds);
        float maximum = Math.Max(minimum, maximumSeconds);
        float durationSeconds = minimum + (maximum - minimum) * (float)entity.World.Rand.NextDouble();
        nextStateChangeMs = now + (long)(durationSeconds * 1000);
    }

    private void BeginLanding()
    {
        isLanding = true;
        hasTarget = false;
        landingAnimationStarted = false;
    }

    private void UpdateLanding()
    {
        if (!TryFindLandingPosition(out Vec3d landingPosition))
        {
            // Stay airborne rather than falling if the terrain below is unloaded
            // or no valid non-liquid ground exists inside the scan range.
            hasTarget = false;
            entity.Pos.Motion.X *= 1 - steering;
            entity.Pos.Motion.Y *= 1 - steering;
            entity.Pos.Motion.Z *= 1 - steering;
            return;
        }

        SetFlightTarget(landingPosition);
        StartLandingAnimationIfNeeded(landingPosition);
        if (IsAtFlightTarget(landingArrivalDistance))
        {
            SetFlying(false);
            nextStateChangeMs = 0;
            return;
        }

        ApplyFlightMovement(landingSpeed, landingArrivalDistance);
    }

    private bool TryFindLandingPosition(out Vec3d landingPosition)
    {
        BlockPos groundPos = entity.Pos.AsBlockPos.Copy();
        groundPos.Y--;
        int minimumY = Math.Max(0, groundPos.Y - Math.Max(1, landingScanDepth));

        for (; groundPos.Y >= minimumY; groundPos.Y--)
        {
            Block block = entity.World.BlockAccessor.GetBlock(groundPos);
            if (block.Id == 0 || block.IsLiquid() || !block.SideIsSolid(groundPos, BlockFacing.UP.Index)) continue;

            landingPosition = new Vec3d(entity.Pos.X, groundPos.Y + 1 + landingHeight, entity.Pos.Z);
            return true;
        }

        landingPosition = null;
        return false;
    }

    private bool IsAtFlightTarget(float currentArrivalDistance)
    {
        return hasTarget && entity.Pos.XYZ.SquareDistanceTo(targetPosition) <= currentArrivalDistance * currentArrivalDistance;
    }

    private void StartLandingAnimationIfNeeded(Vec3d landingPosition)
    {
        if (landingAnimationStarted || string.IsNullOrEmpty(landingAnimation)) return;
        if (entity.Pos.Y - landingPosition.Y > landingAnimationStartHeight) return;

        SetFlightAnimation("flightlanding", landingAnimation, landingAnimationSpeed, landingAnimationWeight, true);
        landingAnimationStarted = true;
    }

    private void UpdateClientFlightAnimation()
    {
        string source = entity.WatchedAttributes.GetString(FlightAnimationSourceAttribute);
        string animation = entity.WatchedAttributes.GetString(FlightAnimationAttribute);
        float animationSpeed = entity.WatchedAttributes.GetFloat(FlightAnimationSpeedAttribute, 1f);
        float animationWeight = entity.WatchedAttributes.GetFloat(FlightAnimationWeightAttribute, 10f);
        float animationEaseInSpeed = entity.WatchedAttributes.GetFloat(FlightAnimationEaseInSpeedAttribute, 6f);
        float animationEaseOutSpeed = entity.WatchedAttributes.GetFloat(FlightAnimationEaseOutSpeedAttribute, 6f);
        bool suppressDefaultAnimation = entity.WatchedAttributes.GetBool(FlightAnimationSuppressDefaultAttribute);

        if (source == clientAnimationSource && animation == clientAnimation && animationSpeed == clientAnimationSpeed && animationWeight == clientAnimationWeight && animationEaseInSpeed == clientAnimationEaseInSpeed && animationEaseOutSpeed == clientAnimationEaseOutSpeed && suppressDefaultAnimation == clientAnimationSuppressDefault) return;

        if (entity is EntityAgent agent)
        {
            if (!string.IsNullOrEmpty(clientAnimationSource))
            {
                agent.AnimManager?.StopAnimation(clientAnimationSource);
            }

            if (!string.IsNullOrEmpty(source) && !string.IsNullOrEmpty(animation))
            {
                agent.AnimManager?.StartAnimation(new AnimationMetaData
                {
                    Animation = animation,
                    Code = source,
                    AnimationSpeed = animationSpeed,
                    Weight = animationWeight,
                    BlendMode = EnumAnimationBlendMode.Average,
                    EaseInSpeed = animationEaseInSpeed,
                    EaseOutSpeed = animationEaseOutSpeed,
                    SupressDefaultAnimation = suppressDefaultAnimation
                });
            }
        }

        clientAnimationSource = source;
        clientAnimation = animation;
        clientAnimationSpeed = animationSpeed;
        clientAnimationWeight = animationWeight;
        clientAnimationEaseInSpeed = animationEaseInSpeed;
        clientAnimationEaseOutSpeed = animationEaseOutSpeed;
        clientAnimationSuppressDefault = suppressDefaultAnimation;
    }
}
