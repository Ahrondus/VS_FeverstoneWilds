using FeverstoneWilds.Flight.Behavior;
using Vintagestory.API.Common;

namespace FeverstoneWilds.Flight;

/// <summary>
/// Opt-in entity base for creatures that use <see cref="BehaviorFlight"/>.
/// It preserves normal physics except that gravity is disabled while the
/// attached flight behavior is active.
/// </summary>
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
