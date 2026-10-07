using System;
using FeverstoneWilds.Flight.Behavior;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace FeverstoneWilds.Flight.Behavior;

public class BehaviorFlightRideable : EntityBehaviorRideable
{
    private const long FlightSneakReleaseQuietMs = 250;

    private bool waitingForFlightSneakRelease;
    private long lastFlightSneakPressMs;
    private double flightTurnMotion;
    private bool? lastFlightTurnTraceState;
    private long nextFlightRenderTraceAtMs;

    public BehaviorFlightRideable(Entity entity) : base(entity) { }

    protected override IMountableSeat CreateSeat(string seatId, SeatConfig config)
    {
        EntityRideableSeat seat = (EntityRideableSeat)base.CreateSeat(seatId, config);
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
        base.UpdateAngleAndMotion(deltaTime);

        BehaviorFlight flight = entity.GetBehavior<BehaviorFlight>();
        if (flight?.IsFlying != true) return;

        float clampedDeltaTime = Math.Min(0.5f, deltaTime);
        entity.Pos.Yaw = (entity.Pos.Yaw + (float)(flightTurnMotion * clampedDeltaTime * 30f)) % GameMath.TWOPI;

        if (entity.World.Side == EnumAppSide.Client && lastFlightTurnTraceState != flight.IsFlying)
        {
            lastFlightTurnTraceState = flight.IsFlying;
            EntityBehaviorGait gait = entity.GetBehavior<EntityBehaviorGait>();
            entity.Api.Logger.Debug("[FeverstoneWilds] Flight steering gate entity=" + entity.EntityId
                + ": flying=" + flight.IsFlying
                + ", turnMotion=" + flightTurnMotion
                + ", gaitAngularVelocity=" + (gait?.AngularVelocity ?? 0)
                + ", yaw=" + entity.Pos.Yaw);
        }

        if (entity.World.Side == EnumAppSide.Client && entity.World.ElapsedMilliseconds >= nextFlightRenderTraceAtMs)
        {
            nextFlightRenderTraceAtMs = entity.World.ElapsedMilliseconds + 1000;
            EntityBehaviorGait gait = entity.GetBehavior<EntityBehaviorGait>();
            Vec3f mountAngle = MountAngle;
            entity.Api.Logger.Debug("[FeverstoneWilds] Flight render state entity=" + entity.EntityId
                + ": turnMotion=" + flightTurnMotion
                + ", gaitAngularVelocity=" + (gait?.AngularVelocity ?? 0)
                + ", mountAngle=" + mountAngle.X + "," + mountAngle.Y + "," + mountAngle.Z
                + ", onGround=" + entity.OnGround
                + ", seatTransform=" + GetControllingSeatTransform());
        }
    }

    private string GetControllingSeatTransform()
    {
        if (entity.GetInterface<IMountable>() is not IMountable mountable) return "none";

        foreach (IMountableSeat seat in mountable.Seats)
        {
            if (!seat.CanControl || seat.Passenger == null) continue;

            float[] values = seat.RenderTransform?.Values;
            if (values == null || values.Length < 11) return "unavailable";

            return values[0] + "," + values[1] + "," + values[2]
                + ";" + values[4] + "," + values[5] + "," + values[6]
                + ";" + values[8] + "," + values[9] + "," + values[10];
        }

        return "no-controller";
    }

    public override void OnGameTick(float deltaTime)
    {
        base.OnGameTick(deltaTime);

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
