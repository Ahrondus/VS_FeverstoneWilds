using Vintagestory.API.Common;
using FeverstoneWilds.Config;

namespace FeverstoneWilds
{
	public class FWildsCore : ModSystem
	{
		public override void Start(ICoreAPI api)
		{
			base.Start(api);

			api.RegisterBlockClass("BlockAnimalNest", typeof(BlockAnimalNest));

			api.RegisterBlockEntityClass("AnimalNest", typeof(BlockEntityAnimalNest));

			ModConfig.ReadConfig(api);
		}
	}
}
