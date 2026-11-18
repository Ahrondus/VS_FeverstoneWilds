using System;
using FeverstoneWilds.Flight.Behavior;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace FeverstoneWilds.Flight.Behavior;

public class BehaviorFlightRideable : EntityBehaviorRideable
{
    private const long FlightSneakReleaseQuietMs = 250;
    private const long FlightSwivelEaseOutDurationMs = 750;
    private const long FlightSwivelLandingClearMs = 150;

    private bool waitingForFlightSneakRelease;
    private long lastFlightSneakPressMs;
    private double flightTurnMotion;
    private bool clientFlightSwivelEaseActive;
    private float clientFlightSwivelStart;
    private long clientFlightSwivelEaseStartMs;
    private long clientFlightSwivelClearUntilMs;
    private bool wasAttackPressed;
    private bool wasGrabPressed;

    public BehaviorFlightRideable(Entity entity) : base(entity) { }

    bool IsGriffon => entity.Code?.Domain == "feverstonewilds" && entity.Code.Path.StartsWith("griffon-");

    bool IsRiderSelection(int index)
    {
        var boxes = entity.GetBehavior<EntityBehaviorSelectionBoxes>()?.selectionBoxes;
        if (index <= 0 || boxes == null || index > boxes.Length) return false;
        string point = boxes[index - 1].AttachPoint.Code;
        return Array.Exists(Seats ?? Array.Empty<IMountableSeat>(), seat => seat.Config != null &&
            seat.Config.Controllable && (seat.Config.SelectionBox == point || seat.Config.APName == point));
    }

    public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition,
        EnumInteractMode mode, ref EnumHandling handled)
    {
        // FaceAP is reserved for ability-driven pickup, including when the rider seat is occupied.
        if (IsGriffon && !Commands.FSWildsBehavior.CanRideCompanion(entity, byEntity))
        {
            handled = EnumHandling.PreventSubsequent;
            if (entity.Api is ICoreClientAPI client)
                client.TriggerIngameError(this, "griffon-private", "This mount is private. Only its owner may use it.");
            return;
        }
        if (IsGriffon && !IsRiderSelection((byEntity as EntityPlayer)?.EntitySelection?.SelectionBoxIndex ?? -1)) return;
        base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
    }

    public override WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection selection,
        IClientPlayer player, ref EnumHandling handled)
    {
        if (IsGriffon && !Commands.FSWildsBehavior.CanRideCompanion(entity, player.Entity)) return Array.Empty<WorldInteraction>();
        if (IsGriffon && !IsRiderSelection(selection.SelectionBoxIndex)) return Array.Empty<WorldInteraction>();
        return base.GetInteractionHelp(world, selection, player, ref handled);
    }

    protected override IMountableSeat CreateSeat(string seatId, SeatConfig config)
    {
        EntityRideableSeat seat = new FlightRideableSeat(this, seatId, config);
        OnEntityAction vanillaOnAction = seat.Controls.OnAction;

        seat.Controls.OnAction = (EnumEntityAction action, bool on, ref EnumHandling handling) =>
        {
            OnFlightControls(seat, vanillaOnAction, action, on, ref handling);
        };

        return seat;
    }

    private void OnFlightControls(EntityRideableSeat seat, OnEntityAction vanillaOnAction, EnumEntityAction action, bool on, ref EnumHandling handling)
    {
        BehaviorFlight flight = entity.GetBehavior<BehaviorFlight>();

        if (flight?.PlayerFlightEnabled == true)
        {
            if (action == EnumEntityAction.Sprint)
            {
                SendFlightInput(EnumFlightInputAction.Sprint, on);
            }

            if (action == EnumEntityAction.Jump)
            {
                SendFlightInput(EnumFlightInputAction.Ascend, on);
                handling = EnumHandling.PreventDefault;
                return;
            }

            if (entity.World.Side == EnumAppSide.Client && action == EnumEntityAction.Sneak && waitingForFlightSneakRelease)
            {
                if (on)
                {
                    lastFlightSneakPressMs = entity.World.ElapsedMilliseconds;
                    if (flight.IsFlying) SendFlightInput(EnumFlightInputAction.Descend, true);
                }

                if (!on) SendFlightInput(EnumFlightInputAction.Descend, false);
                handling = EnumHandling.PreventDefault;
                return;
            }

            if (action == EnumEntityAction.Sneak && flight.IsFlying)
            {
                if (entity.World.Side == EnumAppSide.Client && on)
                {
                    waitingForFlightSneakRelease = true;
                    lastFlightSneakPressMs = entity.World.ElapsedMilliseconds;
                }
                if (!on)
                {
                    SendFlightInput(EnumFlightInputAction.Descend, false);
                    handling = EnumHandling.PreventDefault;
                    return;
                }

                SendFlightInput(EnumFlightInputAction.Descend, on);
                handling = EnumHandling.PreventDefault;
                return;
            }
        }

        vanillaOnAction?.Invoke(action, on, ref handling);
    }

    public override double SeatsToMotion(float deltaTime)
    {
        double turnMotion = base.SeatsToMotion(deltaTime);
        if (entity.GetBehavior<BehaviorFlight>()?.IsFlying != true) return turnMotion;

        flightTurnMotion = turnMotion;
        return 0;
    }

    protected override void UpdateAngleAndMotion(float deltaTime)
    {
        EntityBehaviorGait gait = entity.GetBehavior<EntityBehaviorGait>();
        base.UpdateAngleAndMotion(deltaTime);

        BehaviorFlight flight = entity.GetBehavior<BehaviorFlight>();
        if (flight?.IsFlying != true)
        {
            if (entity.World.Side == EnumAppSide.Client)
            {
                if (clientFlightSwivelEaseActive)
                {
                    clientFlightSwivelEaseActive = false;
                    clientFlightSwivelClearUntilMs = entity.World.ElapsedMilliseconds + FlightSwivelLandingClearMs;
                }

                if (entity.World.ElapsedMilliseconds < clientFlightSwivelClearUntilMs && entity.Properties.Client?.Renderer is EntityShapeRenderer landingRenderer)
                {
                    landingRenderer.nowSwivelRad = 0;
                }
            }
            return;
        }

        if (gait != null)
        {
            gait.AngularVelocity = 0;
        }

        if (entity is EntityAgent entityAgent)
        {
            entityAgent.sidewaysSwivelAngle = 0;
        }

        if (entity.World.Side == EnumAppSide.Client && entity.Properties.Client?.Renderer is EntityShapeRenderer renderer)
        {
            if (!clientFlightSwivelEaseActive)
            {
                clientFlightSwivelEaseActive = true;
                clientFlightSwivelStart = renderer.nowSwivelRad;
                clientFlightSwivelEaseStartMs = entity.World.ElapsedMilliseconds;
            }

            float progress = Math.Min(1f, (entity.World.ElapsedMilliseconds - clientFlightSwivelEaseStartMs) / (float)FlightSwivelEaseOutDurationMs);
            renderer.nowSwivelRad = clientFlightSwivelStart * (1f - progress);
        }

        float clampedDeltaTime = Math.Min(0.5f, deltaTime);
        entity.Pos.Yaw = (entity.Pos.Yaw + (float)(flightTurnMotion * flight.PlayerFlightTurnSpeed * clampedDeltaTime * 30f)) % GameMath.TWOPI;
    }

    public override void OnGameTick(float deltaTime)
    {
        base.OnGameTick(deltaTime);

        if (entity.Api is ICoreClientAPI capi)
        {
            int attackKey = capi.Input.GetHotKeyByCode("fsw-griffon-attack")?.CurrentMapping?.KeyCode ?? (int)GlKeys.F;
            int grabKey = capi.Input.GetHotKeyByCode("fsw-griffon-grab")?.CurrentMapping?.KeyCode ?? (int)GlKeys.R;
            bool attackPressed = attackKey >= 0 && attackKey < capi.Input.KeyboardKeyStateRaw.Length && capi.Input.KeyboardKeyStateRaw[attackKey];
            bool grabPressed = grabKey >= 0 && grabKey < capi.Input.KeyboardKeyStateRaw.Length && capi.Input.KeyboardKeyStateRaw[grabKey];
            var rider = capi.World.Player?.Entity;
            if (rider?.MountedOn?.Entity?.EntityId == entity.EntityId)
            {
                var abilities = capi.ModLoader.GetModSystem<Commands.GriffonMountedAbilities>();
                if (attackPressed && !wasAttackPressed) abilities?.RequestAbility(false);
                if (grabPressed && !wasGrabPressed) abilities?.RequestAbility(true);
            }
            wasAttackPressed = attackPressed;
            wasGrabPressed = grabPressed;
        }

        if (entity.World.Side != EnumAppSide.Client || !waitingForFlightSneakRelease) return;

        if (entity.World.ElapsedMilliseconds - lastFlightSneakPressMs >= FlightSneakReleaseQuietMs)
        {
            waitingForFlightSneakRelease = false;
        }
    }

    private void SendFlightInput(EnumFlightInputAction action, bool active)
    {
        entity.GetBehavior<BehaviorFlight>()?.SetPlayerFlightInput(action, active);

        if (entity.World.Side == EnumAppSide.Server)
        {
            return;
        }

        FlightInputNetwork.Send(entity, action, active);
    }
}
