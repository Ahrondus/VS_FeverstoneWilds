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

      // Complex Creatures
      api.World.Config.SetBool("FSWGriffonEnabled", config.FSWGriffonEnabled);
      
      // Water Creatures
      api.World.Config.SetBool("FSWBuromenfishEnabled", config.FSWBuromenfishEnabled);
      api.World.Config.SetBool("FSWDiscusFishEnabled", config.FSWDiscusFishEnabled);
      api.World.Config.SetBool("FSWEelEnabled", config.FSWEelEnabled);
      api.World.Config.SetBool("FSWGhostfishEnabled", config.FSWGhostfishEnabled);
      api.World.Config.SetBool("FSWOrcaEnabled", config.FSWOrcaEnabled);
      api.World.Config.SetBool("FSWSharksEnabled", config.FSWSharksEnabled);
      api.World.Config.SetBool("FSWStingrayEnabled", config.FSWStingrayEnabled);
      api.World.Config.SetBool("FSWGhostfishEnabled", config.FSWGhostfishEnabled);

      // Golem Types
      api.World.Config.SetBool("FSWCopperGolemEnabled", config.FSWCopperGolemEnabled);
      api.World.Config.SetBool("FSWTinGolemEnabled", config.FSWTinGolemEnabled);
      api.World.Config.SetBool("FSWIronGolemEnabled", config.FSWIronGolemEnabled);
      
      // Faunling Sapling Behavior
      api.World.Config.SetBool("FSWFaunlingSaplingEnabled", config.FSWFaunlingSaplingEnabled);

      // Griffon Flight Behaviors
      api.World.Config.SetFloat("FSWGriffonFlightSpeed", config.FSWGriffonFlightSpeed);
      api.World.Config.SetFloat("FSWGriffonVerticalSpeed", config.FSWGriffonVerticalSpeed);
      api.World.Config.SetFloat("FSWGriffonSteering", config.FSWGriffonSteering);
      api.World.Config.SetFloat("FSWGriffonArrivalDistance", config.FSWGriffonArrivalDistance);
      api.World.Config.SetBool("FSWGriffonAutoFlight", config.FSWGriffonAutoFlight);
      api.World.Config.SetFloat("FSWGriffonMinGroundSeconds", config.FSWGriffonMinGroundSeconds);
      api.World.Config.SetFloat("FSWGriffonMaxGroundSeconds", config.FSWGriffonMaxGroundSeconds);
      api.World.Config.SetFloat("FSWGriffonMinFlightSeconds", config.FSWGriffonMinFlightSeconds);
      api.World.Config.SetFloat("FSWGriffonMaxFlightSeconds", config.FSWGriffonMaxFlightSeconds);
      api.World.Config.SetFloat("FSWGriffonTakeoffSpeed", config.FSWGriffonTakeoffSpeed);
      api.World.Config.SetFloat("FSWGriffonLandingSpeed", config.FSWGriffonLandingSpeed);
      api.World.Config.SetFloat("FSWGriffonLandingHeight", config.FSWGriffonLandingHeight);
      api.World.Config.SetFloat("FSWGriffonLandingArrivalDistance", config.FSWGriffonLandingArrivalDistance);
      api.World.Config.SetString("FSWGriffonLandingAnimation", config.FSWGriffonLandingAnimation);
      api.World.Config.SetFloat("FSWGriffonLandingAnimationSpeed", config.FSWGriffonLandingAnimationSpeed);
      api.World.Config.SetFloat("FSWGriffonLandingAnimationWeight", config.FSWGriffonLandingAnimationWeight);
      api.World.Config.SetFloat("FSWGriffonLandingAnimationStartHeight", config.FSWGriffonLandingAnimationStartHeight);
      api.World.Config.SetInt("FSWGriffonLandingScanDepth", config.FSWGriffonLandingScanDepth);
      api.World.Config.SetFloat("FSWGriffonFailedLandingDescent", config.FSWGriffonFailedLandingDescent);
      api.World.Config.SetFloat("FSWGriffonPlayerFlightSpeed", config.FSWGriffonPlayerFlightSpeed);
      api.World.Config.SetFloat("FSWGriffonPlayerFlightSprintSpeed", config.FSWGriffonPlayerFlightSprintSpeed);
      api.World.Config.SetFloat("FSWGriffonPlayerFlightVerticalSpeed", config.FSWGriffonPlayerFlightVerticalSpeed);
      api.World.Config.SetFloat("FSWGriffonPlayerFlightTakeoffSpeed", config.FSWGriffonPlayerFlightTakeoffSpeed);
      api.World.Config.SetFloat("FSWGriffonPlayerFlightSteering", config.FSWGriffonPlayerFlightSteering);
      api.World.Config.SetFloat("FSWGriffonPlayerFlightTurnSpeed", config.FSWGriffonPlayerFlightTurnSpeed);
      api.World.Config.SetFloat("FSWGriffonPlayerFlightLandingHeight", config.FSWGriffonPlayerFlightLandingHeight);
      api.World.Config.SetString("FSWGriffonPlayerFlightAnimation", config.FSWGriffonPlayerFlightAnimation);
      api.World.Config.SetString("FSWGriffonPlayerFlightSprintAnimation", config.FSWGriffonPlayerFlightSprintAnimation);
      api.World.Config.SetString("FSWGriffonPlayerFlightIdleAnimation", config.FSWGriffonPlayerFlightIdleAnimation);
      api.World.Config.SetString("FSWGriffonPlayerFlightAscendAnimation", config.FSWGriffonPlayerFlightAscendAnimation);
      api.World.Config.SetString("FSWGriffonPlayerFlightDescendAnimation", config.FSWGriffonPlayerFlightDescendAnimation);

      // AnimalCages Config
      api.World.Config.SetBool("AllowCagedFSWCreatures", config.AllowCagedFSWCreatures);

      // ConfigLib
      api.World.Config.SetFloat("BISON_SPAWN_CHANCE_WORLDGEN", config.BISON_SPAWN_CHANCE_WORLDGEN);
      api.World.Config.SetFloat("BISON_SPAWN_CHANCE_RUNTIME", config.BISON_SPAWN_CHANCE_RUNTIME);
      api.World.Config.SetFloat("COCKATRICE_SPAWN_CHANCE_WORLDGEN", config.COCKATRICE_SPAWN_CHANCE_WORLDGEN);
      api.World.Config.SetFloat("COCKATRICE_SPAWN_CHANCE_RUNTIME", config.COCKATRICE_SPAWN_CHANCE_RUNTIME);
      api.World.Config.SetFloat("DIREWOLF_SPAWN_CHANCE_WORLDGEN", config.DIREWOLF_SPAWN_CHANCE_WORLDGEN);
      api.World.Config.SetFloat("DIREWOLF_SPAWN_CHANCE_RUNTIME", config.DIREWOLF_SPAWN_CHANCE_RUNTIME);
      api.World.Config.SetFloat("FAUNLING_SPAWN_CHANCE_WORLDGEN", config.FAUNLING_SPAWN_CHANCE_WORLDGEN);
      api.World.Config.SetFloat("FAUNLING_SPAWN_CHANCE_RUNTIME", config.FAUNLING_SPAWN_CHANCE_RUNTIME);
      api.World.Config.SetFloat("GEODECRAB_SPAWN_CHANCE_WORLDGEN", config.GEODECRAB_SPAWN_CHANCE_WORLDGEN);
      api.World.Config.SetFloat("GEODECRAB_SPAWN_CHANCE_RUNTIME", config.GEODECRAB_SPAWN_CHANCE_RUNTIME);
      api.World.Config.SetFloat("GOLEM_SPAWN_CHANCE_WORLDGEN", config.GOLEM_SPAWN_CHANCE_WORLDGEN);
      api.World.Config.SetFloat("GOLEM_SPAWN_CHANCE_RUNTIME", config.GOLEM_SPAWN_CHANCE_RUNTIME);
      api.World.Config.SetFloat("HELLBOAR_SPAWN_CHANCE_WORLDGEN", config.HELLBOAR_SPAWN_CHANCE_WORLDGEN);
      api.World.Config.SetFloat("HELLBOAR_SPAWN_CHANCE_RUNTIME", config.HELLBOAR_SPAWN_CHANCE_RUNTIME);
      api.World.Config.SetFloat("HORSE_SPAWN_CHANCE_WORLDGEN", config.HORSE_SPAWN_CHANCE_WORLDGEN);
      api.World.Config.SetFloat("HORSE_SPAWN_CHANCE_RUNTIME", config.HORSE_SPAWN_CHANCE_RUNTIME);
      api.World.Config.SetFloat("OSTRICH_SPAWN_CHANCE_WORLDGEN", config.OSTRICH_SPAWN_CHANCE_WORLDGEN);
      api.World.Config.SetFloat("OSTRICH_SPAWN_CHANCE_RUNTIME", config.OSTRICH_SPAWN_CHANCE_RUNTIME);

      api.World.Config.SetFloat("GRIFFON_SPAWN_CHANCE_WORLDGEN", config.GRIFFON_SPAWN_CHANCE_WORLDGEN);
      api.World.Config.SetFloat("GRIFFON_SPAWN_CHANCE_RUNTIME", config.GRIFFON_SPAWN_CHANCE_RUNTIME);
      
      api.World.Config.SetFloat("BUROMENFISH_SPAWN_CHANCE_WORLDGEN", config.BUROMENFISH_SPAWN_CHANCE_WORLDGEN);
      api.World.Config.SetFloat("BUROMENFISH_SPAWN_CHANCE_RUNTIME", config.BUROMENFISH_SPAWN_CHANCE_RUNTIME);
      api.World.Config.SetFloat("DISCUS_SPAWN_CHANCE_WORLDGEN", config.DISCUS_SPAWN_CHANCE_WORLDGEN);
      api.World.Config.SetFloat("DISCUS_SPAWN_CHANCE_RUNTIME", config.DISCUS_SPAWN_CHANCE_RUNTIME);
      api.World.Config.SetFloat("EEL_SPAWN_CHANCE_WORLDGEN", config.EEL_SPAWN_CHANCE_WORLDGEN);
      api.World.Config.SetFloat("EEL_SPAWN_CHANCE_RUNTIME", config.EEL_SPAWN_CHANCE_RUNTIME);
      api.World.Config.SetFloat("GHOSTFISH_SPAWN_CHANCE_WORLDGEN", config.GHOSTFISH_SPAWN_CHANCE_WORLDGEN);
      api.World.Config.SetFloat("GHOSTFISH_SPAWN_CHANCE_RUNTIME", config.GHOSTFISH_SPAWN_CHANCE_RUNTIME);
      api.World.Config.SetFloat("ORCA_SPAWN_CHANCE_WORLDGEN", config.ORCA_SPAWN_CHANCE_WORLDGEN);
      api.World.Config.SetFloat("ORCA_SPAWN_CHANCE_RUNTIME", config.ORCA_SPAWN_CHANCE_RUNTIME);
      api.World.Config.SetFloat("SHARKS_SPAWN_CHANCE_WORLDGEN", config.SHARKS_SPAWN_CHANCE_WORLDGEN);
      api.World.Config.SetFloat("SHARKS_SPAWN_CHANCE_RUNTIME", config.SHARKS_SPAWN_CHANCE_RUNTIME);
      api.World.Config.SetFloat("STINGRAY_SPAWN_CHANCE_WORLDGEN", config.STINGRAY_SPAWN_CHANCE_WORLDGEN);
      api.World.Config.SetFloat("STINGRAY_SPAWN_CHANCE_RUNTIME", config.STINGRAY_SPAWN_CHANCE_RUNTIME);
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
