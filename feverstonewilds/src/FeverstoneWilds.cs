using Vintagestory.API.Common;
using FeverstoneWilds.Config;
using FeverstoneWilds.Flight;
using FeverstoneWilds.Flight.AiTask;
using FeverstoneWilds.Flight.Behavior;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using FeverstoneWilds.Nesting;

namespace FeverstoneWilds
{
	public class FWildsCore : ModSystem
	{
		public override void Start(ICoreAPI api)
		{
			base.Start(api);

			api.RegisterBlockClass("BlockAnimalNestLarge", typeof(BlockAnimalNest));

			api.RegisterBlockEntityClass("AnimalNestLarge", typeof(BlockEntityAnimalNestLarge));
			api.RegisterBlockClass("FswBreedingNest", typeof(BlockAnimalNest));
			api.RegisterBlockEntityClass("FswBreedingNest", typeof(BlockEntityFswBreedingNest));
			api.RegisterBlockEntityBehaviorClass("GriffonNestClutch", typeof(GriffonNestClutch));
			api.RegisterEntityBehaviorClass("fsweggreproduction", typeof(EntityBehaviorFswReproduction));
			api.RegisterEntityBehaviorClass("fswegg", typeof(EntityBehaviorFswEgg));
			api.RegisterItemClass("FswEgg", typeof(ItemFswEgg));
			api.RegisterEntityBehaviorClass("griffonfeathervariant", typeof(BehaviorGriffonFeathers));
			api.RegisterEntityBehaviorClass("griffonmarkings", typeof(BehaviorGriffonMarkings));
			api.RegisterEntityBehaviorClass("griffongrow", typeof(BehaviorGriffonGrow));
			api.RegisterEntityBehaviorClass("griffongrowthsize", typeof(BehaviorGriffonGrowthSize));
			api.RegisterEntityBehaviorClass("griffonnestshelldrop", typeof(GriffonNestShellDrop));

			api.RegisterEntityBehaviorClass("plantSapling", typeof(BehaviorPlantSapling));
			api.RegisterEntityBehaviorClass("fswentitylight", typeof(BehaviorEntityLight));
			api.RegisterEntityBehaviorClass("flight", typeof(BehaviorFlight));
			api.RegisterEntityBehaviorClass("flightrideable", typeof(BehaviorFlightRideable));
            api.RegisterEntityBehaviorClass("direwolfrideable", typeof(FeverstoneWilds.Commands.DirewolfRideable));
			api.RegisterEntity("FlyingEntityAgent", typeof(FlyingEntityAgent));

			if (api is ICoreServerAPI serverApi)
			{
				serverApi.RegisterAiTask<AiTaskFswNesting>("fswnesting");
				serverApi.RegisterAiTask<AiTaskGriffonSeekNestAndLay>("griffonseeknestandlay");
                serverApi.RegisterAiTask<AiTaskGriffonGuardNest>("griffonguardnest");
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
