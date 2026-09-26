using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;

namespace FeverstoneWilds.Flight;

public static class FlightInputNetwork
{
    public const string ChannelName = "feverstonewilds-mountedflight";

    private static IClientNetworkChannel clientChannel;

    public static void RegisterClient(ICoreClientAPI capi)
    {
        clientChannel = capi.Network.RegisterChannel(ChannelName).RegisterMessageType<FlightInputPacket>();
    }

    public static void Send(Entity entity, EnumFlightInputAction action, bool active)
    {
        clientChannel?.SendPacket(new FlightInputPacket
        {
            EntityId = entity.EntityId,
            Action = action,
            Active = active
        });
    }
}
