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
			api.RegisterEntity("FlyingEntityAgent", typeof(FlyingEntityAgent));

			if (api is ICoreServerAPI serverApi)
			{
				serverApi.RegisterAiTask<AiTaskFlightWander>("flightwander");
				serverApi.RegisterAiTask<AiTaskFlightSeekEntity>("flightseekentity");
			}

			ModConfig.ReadConfig(api);
		}
	}
}
