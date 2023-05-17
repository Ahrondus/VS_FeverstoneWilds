using Vintagestory.API.Common;
using FeverstoneWilds.Config;

namespace FeverstoneWilds
{
	public class FWildsCore : ModSystem
	{
		public override void Start(ICoreAPI api)
		{
			base.Start(api);

			ModConfig.ReadConfig(api);
			api.World.Logger.Event("Reading 'Feverstone Wilds' Config");
		}
	}
}
