using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Util;
using Vintagestory.API.Server;
using System.Reflection;
using Vintagestory.ServerMods;
using FeverstoneWilds.Configuration;

namespace FeverstoneWilds
{
	public class FWildsCore : ModSystem
	{
		public override void StartPre(ICoreAPI api)
		{
			base.StartPre(api);

			try {
				FeverstoneWildsConfig FromDisk;
				if((FromDisk = api.LoadModConfig<FeverstoneWildsConfig>(FeverstoneWildsConfig.json)) == null)
				{
					api.StoreModConfig<FeverstoneWildsConfig>(FeverstoneWildsConfig.Loaded, "FeverstoneWildsConfig.json");
				}
				else FeverstoneWildsConfig.Loaded = FromDisk;
			}
			catch {
				api.StoreModConfig<FeverstoneWildsConfig>(FeverstoneWildsConfig.Loaded, "FeverstoneWildsConfig.json");
			}

			api.World.Config.SetBool("FaunlingEnabled", FeverstoneWildsConfig.Loaded.FaunlingEnabled);
			api.World.Config.SetBool("WildDirewolfEnabled", FeverstoneWildsConfig.Loaded.WildDirewolfEnabled);
			api.World.Config.SetBool("TameDirewolfEnabled", FeverstoneWildsConfig.Loaded.TameDirewolfEnabled);
			api.World.Config.SetBool("WildDirewolfPupEnabled", FeverstoneWildsConfig.Loaded.WildDirewolfPupEnabled);
			api.World.Config.SetBool("TameDirewolfPupEnabled", FeverstoneWildsConfig.Loaded.TameDirewolfPupEnabled);
			api.World.Config.SetBool("HorseEnabled", FeverstoneWildsConfig.Loaded.HorseEnabled);
			api.World.Config.SetBool("FoalEnabled", FeverstoneWildsConfig.Loaded.FoalEnabled);
			api.World.Config.SetBool("GolemEnabled", FeverstoneWildsConfig.Loaded.GolemEnabled);
			api.World.Config.SetBool("GeodeCrabEnabled", FeverstoneWildsConfig.Loaded.GeodeCrabEnabled);
			api.World.Config.SetBool("SpiderEnabled", FeverstoneWildsConfig.Loaded.SpiderEnabled);
			api.World.Config.SetBool("ToadEnabled", FeverstoneWildsConfig.Loaded.ToadEnabled);
			api.World.Config.SetBool("BuromenfishEnabled", FeverstoneWildsConfig.Loaded.BuromenfishEnabled);
			api.World.Config.SetBool("DiscusFishEnabled", FeverstoneWildsConfig.Loaded.DiscusFishEnabled);
			api.World.Config.SetBool("BullsharkEnabled", FeverstoneWildsConfig.Loaded.BullsharkEnabled);
			api.World.Config.SetBool("StingrayEnabled", FeverstoneWildsConfig.Loaded.StingrayEnabled);

			ModConfig.ReadConfig(api);
			api.World.Logger.Event("Reading 'FeverstoneWilds' Config");
		}
		
		public override void StartServerSide(ICoreAPI api)
		{
			base.StartServerSide(api);
		}
		
		public override void StartClientSide(ICoreAPI api)
		{
			base.StartClientSide(api);
		}
	}
}
