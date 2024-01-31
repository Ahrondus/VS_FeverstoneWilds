using Vintagestory.API.Common;
using FeverstoneWilds.Config;

namespace FeverstoneWilds
{
	public class FWildsCore : ModSystem
	{
		public override void Start(ICoreAPI api)
		{
			base.Start(api);

			api.RegisterBlockClass("BlockAnimalNestCockatrice", typeof(BlockAnimalNest));
			api.RegisterBlockClass("BlockAnimalNestOstrich", typeof(BlockAnimalNest));

			api.RegisterBlockEntityClass("AnimalNestOstrich", typeof(BlockEntityAnimalNestOstrich));
            api.RegisterBlockEntityClass("AnimalNestCockatrice", typeof(BlockEntityAnimalNestCockatrice));

            ModConfig.ReadConfig(api);
		}
	}
}
