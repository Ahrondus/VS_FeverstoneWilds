using Vintagestory.API.Common;
using FeverstoneWilds.Config;

namespace FeverstoneWilds
{
	public class FWildsCore : ModSystem
	{
		public override void Start(ICoreAPI api)
		{
			base.Start(api);

			api.RegisterBlockClass("BlockAnimalNestLarge", typeof(BlockAnimalNest));

			api.RegisterBlockEntityClass("AnimalNestLarge", typeof (BlockEntityAnimalNestLarge));

            ModConfig.ReadConfig(api);
		}
	}
}
