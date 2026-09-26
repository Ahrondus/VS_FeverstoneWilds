using FeverstoneWilds.Flight.Behavior;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace FeverstoneWilds.Flight.Behavior;

public class BehaviorFlightRideable : EntityBehaviorRideable
{
    private const long DismountSneakHoldMs = 750;
    private const float DismountMaximumHeightAboveGround = 2f;

    private long sneakStartedMs;
    private bool dismountRequested;

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
        if (action == EnumEntityAction.Sneak && !on)
        {
            sneakStartedMs = 0;
            dismountRequested = false;
        }

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

            if (action == EnumEntityAction.Sneak && flight.IsFlying)
            {
                if (!on)
                {
                    sneakStartedMs = 0;
                    dismountRequested = false;
                    SendFlightInput(EnumFlightInputAction.Descend, false);
                    handling = EnumHandling.PreventDefault;
                    return;
                }

                if (sneakStartedMs == 0)
                {
                    sneakStartedMs = entity.World.ElapsedMilliseconds;
                }

                if (!dismountRequested && entity.World.ElapsedMilliseconds - sneakStartedMs >= DismountSneakHoldMs && flight.CanPlayerDismount(DismountMaximumHeightAboveGround))
                {
                    dismountRequested = true;
                    SendFlightInput(EnumFlightInputAction.RequestDismount, true);
                    sneakStartedMs = 0;
                    dismountRequested = false;
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
