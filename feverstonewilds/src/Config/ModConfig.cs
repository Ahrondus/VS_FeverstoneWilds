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
          api.World.Logger.Event("Creating New 'Feverstone Wilds' Config");
          GenerateConfig(api);
          config = LoadConfig(api);
        }
        else
        {
			    api.World.Logger.Event("Reading 'Feverstone Wilds' Config");
          GenerateConfig(api, config);
        }
      }
      catch
      {
        api.World.Logger.Event("Creating New 'Feverstone Wilds' Config");
        GenerateConfig(api);
        config = LoadConfig(api);
      }
        // Land Creatures
        api.World.Config.SetBool("FSWBisonEnabled", config.FSWBisonEnabled);
        api.World.Config.SetBool("FSWBisonCalfEnabled", config.FSWBisonCalfEnabled);
        api.World.Config.SetBool("FSWCockatriceEnabled", config.FSWCockatriceEnabled);
        api.World.Config.SetBool("FSWTameCockatriceEnabled", config.FSWCockatriceEnabled);
        api.World.Config.SetBool("FSWWildDirewolfEnabled", config.FSWWildDirewolfEnabled);
        api.World.Config.SetBool("FSWTameDirewolfEnabled", config.FSWTameDirewolfEnabled);
        api.World.Config.SetBool("FSWWildDirewolfPupEnabled", config.FSWWildDirewolfPupEnabled);
        api.World.Config.SetBool("FSWTameDirewolfPupEnabled", config.FSWTameDirewolfPupEnabled);
        api.World.Config.SetBool("FSWFaunlingEnabled", config.FSWFaunlingEnabled);
        api.World.Config.SetBool("FSWFoalEnabled", config.FSWFoalEnabled);
        api.World.Config.SetBool("FSWGeodeCrabEnabled", config.FSWGeodeCrabEnabled);
        api.World.Config.SetBool("FSWGiraffeEnabled", config.FSWGiraffeEnabled);
        api.World.Config.SetBool("FSWGolemEnabled", config.FSWGolemEnabled);
        api.World.Config.SetBool("FSWHellboarEnabled", config.FSWHellboarEnabled);
        api.World.Config.SetBool("FSWHorseEnabled", config.FSWHorseEnabled);
        api.World.Config.SetBool("FSWOstrichEnabled", config.FSWOstrichEnabled);
        api.World.Config.SetBool("FSWSpiderEnabled", config.FSWSpiderEnabled);
        api.World.Config.SetBool("FSWScorpionEnabled", config.FSWScorpionEnabled);
        api.World.Config.SetBool("FSWToadEnabled", config.FSWToadEnabled);
        
        // Water Creatures
        api.World.Config.SetBool("FSWBuromenfishEnabled", config.FSWBuromenfishEnabled);
        api.World.Config.SetBool("FSWDiscusFishEnabled", config.FSWDiscusFishEnabled);
        api.World.Config.SetBool("FSWEelEnabled", config.FSWEelEnabled);
        api.World.Config.SetBool("FSWOrcaEnabled", config.FSWOrcaEnabled);
        api.World.Config.SetBool("FSWSharksEnabled", config.FSWSharksEnabled);
        api.World.Config.SetBool("FSWStingrayEnabled", config.FSWStingrayEnabled);

        // Golem Types
        api.World.Config.SetBool("FSWCopperGolemEnabled", config.FSWCopperGolemEnabled);
        api.World.Config.SetBool("FSWTinGolemEnabled", config.FSWTinGolemEnabled);
        api.World.Config.SetBool("FSWIronGolemEnabled", config.FSWIronGolemEnabled);
    }

    // Load a previous config
    private static FeverstoneWildsConfig LoadConfig(ICoreAPI api) =>
      api.LoadModConfig<FeverstoneWildsConfig>(jsonConfig);

    // Generate a new config file
    private static void GenerateConfig(ICoreAPI api) =>
      api.StoreModConfig(new FeverstoneWildsConfig(), jsonConfig);

    // Generate a new config from an existing config
    private static void GenerateConfig(ICoreAPI api, FeverstoneWildsConfig previousConfig) =>
      api.StoreModConfig(new FeverstoneWildsConfig(previousConfig), jsonConfig);
  }
}