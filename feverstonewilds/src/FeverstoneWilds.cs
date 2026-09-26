using Vintagestory.API.Common;
using FeverstoneWilds.Config;
using FeverstoneWilds.Flight;
using FeverstoneWilds.Flight.AiTask;
using FeverstoneWilds.Flight.Behavior;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace FeverstoneWilds
{
		public class FWildsCore : ModSystem
	{
		public override void Start(ICoreAPI api)
		{
			base.Start(api);

			api.RegisterBlockClass("BlockAnimalNestLarge", typeof(BlockAnimalNest));
			api.RegisterBlockEntityClass("AnimalNestLarge", typeof(BlockEntityAnimalNestLarge));

			api.RegisterEntityBehaviorClass("plantSapling", typeof(BehaviorPlantSapling));
			api.RegisterEntityBehaviorClass("flight", typeof(BehaviorFlight));
			api.RegisterEntityBehaviorClass("flightrideable", typeof(BehaviorFlightRideable));
			api.RegisterEntity("FlyingEntityAgent", typeof(FlyingEntityAgent));

			if (api is ICoreServerAPI serverApi)
			{
				serverApi.Network.RegisterChannel(FlightInputNetwork.ChannelName)
					.RegisterMessageType<FlightInputPacket>()
					.SetMessageHandler<FlightInputPacket>(OnFlightInput);
				serverApi.RegisterAiTask<AiTaskFlightWander>("flightwander");
				serverApi.RegisterAiTask<AiTaskFlightSeekEntity>("flightseekentity");
				serverApi.RegisterAiTask<AiTaskFlightMeleeAttack>("flightmeleeattack");
			}

			ModConfig.ReadConfig(api);
		}

		public override void StartClientSide(Vintagestory.API.Client.ICoreClientAPI capi)
		{
			base.StartClientSide(capi);
			FlightInputNetwork.RegisterClient(capi);
		}

		private void OnFlightInput(IServerPlayer player, FlightInputPacket packet)
		{
			if (packet == null) return;

			var entity = player.Entity.World.GetEntityById(packet.EntityId);
			var flight = entity?.GetBehavior<BehaviorFlight>();
			IMountable mountable = entity?.GetInterface<IMountable>();
			IMountableSeat seat = mountable == null ? null : MountableUtil.GetSeatOfMountedEntity(mountable, player.Entity);

			if (flight == null || !flight.PlayerFlightEnabled || seat?.CanControl != true) return;

			flight.SetPlayerFlightInput(packet.Action, packet.Active);
		}

	}
}
