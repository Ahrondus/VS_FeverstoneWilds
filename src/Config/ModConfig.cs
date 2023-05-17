using Vintagestory.API.Common;

namespace FeverstoneWilds.Config
{
  public static class ModConfig
  {
    private const string jsonConfig = "FeverstoneWildsConfig.json";
    private static FeverstoneWildsConfig config;

    public static void ReadConfig(ICoreAPI api)
    {
      try
      {
        config = LoadConfig(api);

        if (config == null)
        {
          GenerateConfig(api);
          config = LoadConfig(api);
        }
        else
        {
          GenerateConfig(api, config);
        }
      }
      catch
      {
        GenerateConfig(api);
        config = LoadConfig(api);
      }

        api.World.Config.SetBool("FaunlingEnabled", config.FaunlingEnabled);
        api.World.Config.SetBool("WildDirewolfEnabled", config.WildDirewolfEnabled);
        api.World.Config.SetBool("TameDirewolfEnabled", config.TameDirewolfEnabled);
        api.World.Config.SetBool("WildDirewolfPupEnabled", config.WildDirewolfPupEnabled);
        api.World.Config.SetBool("TameDirewolfPupEnabled", config.TameDirewolfPupEnabled);
        api.World.Config.SetBool("HorseEnabled", config.HorseEnabled);
        api.World.Config.SetBool("FoalEnabled", config.FoalEnabled);
        api.World.Config.SetBool("BisonEnabled", config.BisonEnabled);
        api.World.Config.SetBool("BisonCalfEnabled", config.BisonCalfEnabled);
        api.World.Config.SetBool("GolemEnabled", config.GolemEnabled);
        api.World.Config.SetBool("GeodeCrabEnabled", config.GeodeCrabEnabled);
        api.World.Config.SetBool("SpiderEnabled", config.SpiderEnabled);
        api.World.Config.SetBool("ToadEnabled", config.ToadEnabled);
        api.World.Config.SetBool("BuromenfishEnabled", config.BuromenfishEnabled);
        api.World.Config.SetBool("DiscusFishEnabled", config.DiscusFishEnabled);
        api.World.Config.SetBool("SharkEnabled", config.SharkEnabled);
        api.World.Config.SetBool("OrcaEnabled", config.OrcaEnabled);
        api.World.Config.SetBool("StingrayEnabled", config.StingrayEnabled);
    }

    private static FeverstoneWildsConfig LoadConfig(ICoreAPI api) =>
      api.LoadModConfig<FeverstoneWildsConfig>(jsonConfig);

    private static void GenerateConfig(ICoreAPI api) =>
      api.StoreModConfig(new FeverstoneWildsConfig(), jsonConfig);

    private static void GenerateConfig(ICoreAPI api, FeverstoneWildsConfig previousConfig) =>
      api.StoreModConfig(new FeverstoneWildsConfig(previousConfig), jsonConfig);
  }
}