using ProtoBuf;

namespace FeverstoneWilds.Flight;

[ProtoContract]
public class FlightInputPacket
{
    [ProtoMember(1)]
    public long EntityId { get; set; }

    [ProtoMember(2)]
    public EnumFlightInputAction Action { get; set; }

    [ProtoMember(3)]
    public bool Active { get; set; }
}

public enum EnumFlightInputAction
{
    Ascend,
    Descend,
    Sprint
}
