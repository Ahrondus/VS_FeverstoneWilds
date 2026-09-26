using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace FeverstoneWilds.Flight.Behavior;

// Creative-style aerial movement for FlyingEntityAgent.
// Configurable via JSON
// Keeps Vintage Story's collision physics.
public class BehaviorFlight : EntityBehavior
{
    private enum EnumLandingSearchResult
    {
        Found,
        NoValidGround,
        Water
    }

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
    private float failedLandingDescent;
    private bool playerFlightEnabled;
    private float playerFlightSpeed;
    private float playerFlightSprintSpeed;
    private float playerFlightVerticalSpeed;
    private float playerFlightTakeoffSpeed;
    private float playerFlightSteering;
    private string playerFlightAnimation;
    private string playerFlightSprintAnimation;
    private string playerFlightIdleAnimation;
    private long nextStateChangeMs;
    private Vec3d targetPosition;
    private bool hasTarget;
    private bool isLanding;
    private bool isAttacking;
    private bool isPlayerControlledFlight;
    private bool clientPlayerFlightActive;
    private bool playerFlightAscending;
    private bool playerFlightDescending;
    private bool playerFlightSprinting;
    private long playerFlightInputUntilMs;
    private string mountedFlightAnimation;
    private bool landingAnimationStarted;
    private string clientAnimationSource;
    private string clientAnimation;
    private float clientAnimationSpeed;
    private float clientAnimationWeight;
    private float clientAnimationEaseInSpeed;
    private float clientAnimationEaseOutSpeed;
    private bool clientAnimationSuppressDefault;
    private string clientFlightTurnAnimation;

    public BehaviorFlight(Entity entity) : base(entity) { }

    public bool IsFlying => entity.WatchedAttributes.GetBool(FlyingAttribute);

    public bool IsLanding => isLanding;

    public bool IsAttacking => isAttacking;

    public bool IsPlayerControlledFlight => isPlayerControlledFlight;

    public bool PlayerFlightEnabled => playerFlightEnabled;

    public bool CanPlayerDismount(float maximumHeightAboveGround)
    {
        if (TryFindLandingPosition(out Vec3d landingPosition) != EnumLandingSearchResult.Found) return false;

        // landingPosition includes landingHeight so autonomous landing can hover just
        // above the surface. Dismount safety needs the actual solid surface instead.
        double groundSurfaceY = landingPosition.Y - landingHeight;
        double heightAboveGround = entity.Pos.Y - groundSurfaceY;
        return heightAboveGround >= 0 && heightAboveGround <= maximumHeightAboveGround;
    }

    public void EndPlayerFlightForDismount(float maximumHeightAboveGround)
    {
        if (entity.World.Side != EnumAppSide.Server || !CanPlayerDismount(maximumHeightAboveGround)) return;

        SetFlying(false);

        if (entity.GetInterface<IMountable>()?.Controller is EntityAgent controller)
        {
            controller.TryUnmount();
        }
    }

    public void RequestPlayerDismount()
    {
        EndPlayerFlightForDismount(2f);
    }

    public override string PropertyName() => "flight";

    public override void Initialize(EntityProperties properties, JsonObject attributes)
    {
        base.Initialize(properties, attributes);

        flightSpeed = attributes["flightSpeed"].AsFloat(0.08f);
        verticalSpeed = attributes["verticalSpeed"].AsFloat(0.06f);
        steering = attributes["steering"].AsFloat(0.25f);
        arrivalDistance = attributes["arrivalDistance"].AsFloat(1.25f);
        autoFlight = attributes["autoFlight"].AsBool(true);
        minGroundSeconds = attributes["minGroundSeconds"].AsFloat(22f);
        maxGroundSeconds = attributes["maxGroundSeconds"].AsFloat(48f);
        minFlightSeconds = attributes["minFlightSeconds"].AsFloat(30f);
        maxFlightSeconds = attributes["maxFlightSeconds"].AsFloat(52f);
        takeoffSpeed = attributes["takeoffSpeed"].AsFloat(0.06f);
        landingSpeed = attributes["landingSpeed"].AsFloat(0.035f);
        landingHeight = attributes["landingHeight"].AsFloat(0.15f);
        landingArrivalDistance = attributes["landingArrivalDistance"].AsFloat(0.05f);
        landingAnimation = attributes["landingAnimation"].AsString(null);
        landingAnimationSpeed = attributes["landingAnimationSpeed"].AsFloat(0.7f);
        landingAnimationWeight = attributes["landingAnimationWeight"].AsFloat(30f);
        landingAnimationStartHeight = attributes["landingAnimationStartHeight"].AsFloat(6f);
        landingScanDepth = attributes["landingScanDepth"].AsInt(120);
        failedLandingDescent = attributes["failedLandingDescent"].AsFloat(8f);
        playerFlightEnabled = GetPlayerFlightEnabled(attributes);
        playerFlightSpeed = attributes["playerFlightSpeed"].AsFloat(flightSpeed);
        playerFlightSprintSpeed = attributes["playerFlightSprintSpeed"].AsFloat(playerFlightSpeed);
        playerFlightVerticalSpeed = attributes["playerFlightVerticalSpeed"].AsFloat(verticalSpeed);
        playerFlightTakeoffSpeed = attributes["playerFlightTakeoffSpeed"].AsFloat(takeoffSpeed);
        playerFlightSteering = attributes["playerFlightSteering"].AsFloat(steering);
        playerFlightAnimation = attributes["playerFlightAnimation"].AsString("fly");
        playerFlightSprintAnimation = attributes["playerFlightSprintAnimation"].AsString("speedfly");
        playerFlightIdleAnimation = attributes["playerFlightIdleAnimation"].AsString("flyidle");

        entity.AfterPhysicsTick += ClearOnGroundWhileFlying;
    }

    public override void OnEntityDespawn(EntityDespawnData despawn)
    {
        entity.AfterPhysicsTick -= ClearOnGroundWhileFlying;
        base.OnEntityDespawn(despawn);
    }

    private void ClearOnGroundWhileFlying()
    {
        if (!IsFlying || !entity.OnGround) return;

        entity.OnGround = false;
        entity.MarkTagsDirty();
    }

    private bool GetPlayerFlightEnabled(JsonObject attributes)
    {
        bool enabled = attributes["playerFlightEnabled"].AsBool(false);
        Dictionary<string, bool> enabledByType = attributes["playerFlightEnabledByType"].AsObject<Dictionary<string, bool>>(null);
        if (enabledByType == null) return enabled;

        int bestMatchSpecificity = -1;
        foreach (KeyValuePair<string, bool> entry in enabledByType)
        {
            if (!WildcardUtil.Match(entry.Key, entity.Code.Path)) continue;

            int specificity = entry.Key.Replace("*", string.Empty).Length;
            if (specificity <= bestMatchSpecificity) continue;

            bestMatchSpecificity = specificity;
            enabled = entry.Value;
        }

        return enabled;
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
            playerFlightAscending = false;
            playerFlightDescending = false;
            playerFlightSprinting = false;
            ClearFlightAnimation();
        }
    }

    public void SetPlayerFlightInput(EnumFlightInputAction action, bool active)
    {
        if (action == EnumFlightInputAction.Ascend)
        {
            playerFlightAscending = active;
            if (active) playerFlightDescending = false;
        }
        else if (action == EnumFlightInputAction.Descend)
        {
            playerFlightDescending = active;
            if (active) playerFlightAscending = false;
        }
        else if (action == EnumFlightInputAction.Sprint)
        {
            playerFlightSprinting = active;
            return;
        }
        else if (action == EnumFlightInputAction.RequestDismount)
        {
            if (active) RequestPlayerDismount();
            return;
        }

        if (active)
        {
            playerFlightInputUntilMs = entity.World.ElapsedMilliseconds + 150;
        }

        if (entity.World.Side == EnumAppSide.Client && action == EnumFlightInputAction.Ascend && active)
        {
            clientPlayerFlightActive = true;
        }

    }

    private void UpdatePlayerFlightInputTimeout()
    {
        if (playerFlightInputUntilMs == 0 || entity.World.ElapsedMilliseconds < playerFlightInputUntilMs) return;

        playerFlightInputUntilMs = 0;
        playerFlightAscending = false;
        playerFlightDescending = false;
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
            UpdatePlayerFlightInputTimeout();
            UpdateClientPlayerControlledFlight();
            SuppressClientGaitAnimation();
            UpdateClientFlightAnimation();
            return;
        }

        UpdatePlayerFlightInputTimeout();

        if (TryUpdatePlayerControlledFlight()) return;

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

    private bool TryUpdatePlayerControlledFlight()
    {
        if (!playerFlightEnabled || entity.GetInterface<IMountable>() is not IMountable mountable || !mountable.IsBeingControlled())
        {
            if (isPlayerControlledFlight)
            {
                isPlayerControlledFlight = false;
                mountedFlightAnimation = null;
                ClearFlightAnimation("mountedflight");
                playerFlightAscending = false;
                playerFlightDescending = false;
                playerFlightSprinting = false;

                if (IsFlying && entity.Alive)
                {
                    BeginLanding();
                }
            }

            return false;
        }

        isPlayerControlledFlight = true;

        UpdatePlayerControlledFlight(mountable.ControllingControls);
        return true;
    }

    private void UpdatePlayerControlledFlight(EntityControls controls)
    {
        if (controls == null) return;

        bool jumping = playerFlightAscending;
        bool sneaking = playerFlightDescending;
        bool sprinting = playerFlightSprinting || controls.Sprint;

        if (!IsFlying)
        {
            mountedFlightAnimation = null;
            ClearFlightAnimation("mountedflight");
            if (!jumping) return;

            isLanding = false;
            hasTarget = false;
            landingAnimationStarted = false;
            nextStateChangeMs = 0;
            SetFlying(true);
            entity.Pos.Motion.Y = Math.Max(entity.Pos.Motion.Y, playerFlightTakeoffSpeed);
        }

        isLanding = false;
        hasTarget = false;

        float forwardMovement = controls.Forward ? 1 : controls.Backward ? -1 : 0;
        float horizontalSpeed = sprinting ? playerFlightSprintSpeed : playerFlightSpeed;
        float desiredX = (float)Math.Sin(entity.Pos.Yaw) * horizontalSpeed * forwardMovement;
        float desiredZ = (float)Math.Cos(entity.Pos.Yaw) * horizontalSpeed * forwardMovement;
        float desiredY = jumping ? playerFlightVerticalSpeed : sneaking ? -playerFlightVerticalSpeed : 0;

        entity.Pos.Motion.X += (desiredX - entity.Pos.Motion.X) * playerFlightSteering;
        entity.Pos.Motion.Z += (desiredZ - entity.Pos.Motion.Z) * playerFlightSteering;
        entity.Pos.Motion.Y = desiredY;

        string animation = forwardMovement == 0 ? playerFlightIdleAnimation : sprinting ? playerFlightSprintAnimation : playerFlightAnimation;
        if (animation == mountedFlightAnimation) return;

        mountedFlightAnimation = animation;
        SetFlightAnimation("mountedflight", animation, 1f, 10f, true);
    }

    private void UpdateClientPlayerControlledFlight()
    {
        if (!playerFlightEnabled || entity.GetInterface<IMountable>() is not IMountable mountable || !mountable.IsBeingControlled())
        {
            clientPlayerFlightActive = false;
            StopClientFlightTurnAnimation();
            return;
        }

        if (!clientPlayerFlightActive)
        {
            clientPlayerFlightActive = IsFlying;
            StopClientFlightTurnAnimation();
        }

        if (!IsFlying && !playerFlightAscending)
        {
            clientPlayerFlightActive = false;
            StopClientFlightTurnAnimation();
            return;
        }

        if (!clientPlayerFlightActive)
        {
            StopClientFlightTurnAnimation();
            return;
        }

        EntityControls controls = mountable.ControllingControls;
        UpdateClientFlightTurnAnimation(controls);

        float desiredY = playerFlightAscending ? playerFlightVerticalSpeed : playerFlightDescending ? -playerFlightVerticalSpeed : 0;
        entity.Pos.Motion.Y = desiredY;
    }

    private void UpdateClientFlightTurnAnimation(EntityControls controls)
    {
        if (controls == null || entity is not EntityAgent agent)
        {
            StopClientFlightTurnAnimation();
            return;
        }

        bool turningLeft = controls.Left && !controls.Right;
        bool turningRight = controls.Right && !controls.Left;
        string animation = null;
        if (turningLeft || turningRight)
        {
            bool moving = controls.Forward || controls.Backward;
            animation = moving ? (turningLeft ? "turn-left" : "turn-right") : (turningLeft ? "idle-turn-left" : "idle-turn-right");
        }

        if (animation == clientFlightTurnAnimation) return;

        if (!string.IsNullOrEmpty(clientFlightTurnAnimation))
        {
            agent.StopAnimation(clientFlightTurnAnimation);
        }

        if (!string.IsNullOrEmpty(animation)) agent.StartAnimation(animation);
        clientFlightTurnAnimation = animation;
    }

    private void StopClientFlightTurnAnimation()
    {
        if (string.IsNullOrEmpty(clientFlightTurnAnimation) || entity is not EntityAgent agent) return;

        agent.StopAnimation(clientFlightTurnAnimation);
        clientFlightTurnAnimation = null;
    }

    private void SuppressClientGaitAnimation()
    {
        if (!clientPlayerFlightActive || entity is not EntityAgent agent) return;

        agent.AnimManager?.StopAnimation("gait");
        agent.AnimManager?.StopAnimation("idle");
        agent.AnimManager?.StopAnimation("walk");
        agent.AnimManager?.StopAnimation("walkback");
        agent.AnimManager?.StopAnimation("sprint");
        agent.AnimManager?.StopAnimation("run");
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

        // A tamed mount remains grounded when nobody is controlling it.
        if (playerFlightEnabled && entity.GetInterface<IMountable>() != null) return;

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
        StartLandingAnimation();

        EnumLandingSearchResult landingSearchResult = TryFindLandingPosition(out Vec3d landingPosition);
        if (landingSearchResult != EnumLandingSearchResult.Found)
        {
            ClearFlightAnimation("flightlanding");
            isLanding = false;
            landingAnimationStarted = false;

            if (landingSearchResult == EnumLandingSearchResult.Water)
            {
                // Keep altitude over water rather than descending toward the lakebed.
                double angle = entity.World.Rand.NextDouble() * GameMath.TWOPI;
                SetFlightTarget(new Vec3d(entity.Pos.X + Math.Cos(angle) * failedLandingDescent, entity.Pos.Y, entity.Pos.Z + Math.Sin(angle) * failedLandingDescent));
            }
            else
            {
                // Descend a short distance so the next landing search can check lower terrain.
                SetFlightTarget(new Vec3d(entity.Pos.X, Math.Max(0.5, entity.Pos.Y - failedLandingDescent), entity.Pos.Z));
            }

            SetFlightAnimation("flightfallback", "fly", 1f);
            ScheduleNextStateChange(entity.World.ElapsedMilliseconds, minFlightSeconds, maxFlightSeconds);
            return;
        }

        SetFlightTarget(landingPosition);
        if (IsAtFlightTarget(landingArrivalDistance))
        {
            SetFlying(false);
            nextStateChangeMs = 0;
            return;
        }

        ApplyFlightMovement(landingSpeed, landingArrivalDistance);
    }

    private EnumLandingSearchResult TryFindLandingPosition(out Vec3d landingPosition)
    {
        BlockPos groundPos = entity.Pos.AsBlockPos.Copy();
        groundPos.Y--;
        int minimumY = Math.Max(0, groundPos.Y - Math.Max(1, landingScanDepth));

        for (; groundPos.Y >= minimumY; groundPos.Y--)
        {
            Block block = entity.World.BlockAccessor.GetBlock(groundPos);
            if (block.Id == 0 || block.IsLiquid() || !block.SideIsSolid(groundPos, BlockFacing.UP.Index)) continue;

            BlockPos landingBlockPos = groundPos.Copy();
            landingBlockPos.Y++;
            Block landingBlock = entity.World.BlockAccessor.GetBlock(landingBlockPos);
            if (landingBlock.IsLiquid())
            {
                landingPosition = null;
                return EnumLandingSearchResult.Water;
            }

            landingPosition = new Vec3d(entity.Pos.X, groundPos.Y + 1 + landingHeight, entity.Pos.Z);
            return EnumLandingSearchResult.Found;
        }

        landingPosition = null;
        return EnumLandingSearchResult.NoValidGround;
    }

    private bool IsAtFlightTarget(float currentArrivalDistance)
    {
        return hasTarget && entity.Pos.XYZ.SquareDistanceTo(targetPosition) <= currentArrivalDistance * currentArrivalDistance;
    }

    private void StartLandingAnimation()
    {
        if (landingAnimationStarted || string.IsNullOrEmpty(landingAnimation)) return;

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
