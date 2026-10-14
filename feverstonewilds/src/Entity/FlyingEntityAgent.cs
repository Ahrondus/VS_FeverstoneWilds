using FeverstoneWilds.Flight.Behavior;
using Vintagestory.API.Common;

namespace FeverstoneWilds.Flight;

public class FlyingEntityAgent : EntityAgent
{
    public override bool ApplyGravity
    {
        get
        {
            BehaviorFlight flight = GetBehavior<BehaviorFlight>();
            return flight == null || !flight.IsFlying;
        }
    }
}
