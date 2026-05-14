using System;

namespace FeverstoneWilds.Config
{
    public class FeverstoneWildsConfig
    {
        // Land Creatures
        public string confighint = "To disable any creature in this list, simply change any of these values from true to false.";
        public string configexample = "For example, to remove faunlings, set FSWFaunlingEnabled to 'false'.";
        public string landheader = "----- Land Creatures -----";
        public bool FSWBisonEnabled = true;
        public bool FSWBisonCalfEnabled = true;
        public bool FSWCockatriceEnabled = true;
        public bool FSWTameCockatriceEnabled = true;
        public bool FSWWildDirewolfEnabled = true;
        public bool FSWTameDirewolfEnabled = true;
        public bool FSWWildDirewolfPupEnabled = true;
        public bool FSWTameDirewolfPupEnabled = true;
        public bool FSWFaunlingEnabled = true;
        public bool FSWFoalEnabled = true;
        public bool FSWGeodeCrabEnabled = true;
        public bool FSWGiraffeEnabled = true;
        public bool FSWGolemEnabled = true;
        public bool FSWHorseEnabled = true;
        public bool FSWHellboarEnabled = true;
        public bool FSWOstrichEnabled = true;
        public bool FSWSpiderEnabled = true;
        public bool FSWScorpionEnabled = true;
        public bool FSWToadEnabled = true;

        // Water Creatures
        public string waterheader = "----- Water Creatures -----";
        public bool FSWBuromenfishEnabled = true;
        public bool FSWDiscusFishEnabled = true;
        public bool FSWEelEnabled = true;
        public bool FSWOrcaEnabled = true;
        public bool FSWSharksEnabled = true;
        public bool FSWStingrayEnabled = true;
        public bool FSWGhostfishEnabled = true;

        // Golem Types
        public string eachgolem = "----- Golems by Type -----";
        public bool FSWCopperGolemEnabled = true;
        public bool FSWTinGolemEnabled = true;
        public bool FSWIronGolemEnabled = true;

        // Animal Cages Config
        public string animalcages = "----- Animal Cages Config -----";
        public bool AllowCagedFSWCreatures = true;

        // ConfigLib Config

         public string configlibhints = "----- Config Lib Spawn Rates -----";
        public int BISON_SPAWN_CHANCE_WORLDGEN { get { return _bison_spawn_worldgen; } set {_bison_spawn_worldgen = value >= 0 ? value : 0;} }
        private int _bison_spawn_worldgen = 4;

        public int BISON_SPAWN_CHANCE_RUNTIME { get { return _bison_spawn_runtime; } set { _bison_spawn_runtime = value >= 0 ? value : 0; } }
        private int _bison_spawn_runtime = 3;
        public int COCKATRICE_SPAWN_CHANCE_WORLDGEN { get { return _cockatrice_spawn_worldgen; } set {_cockatrice_spawn_worldgen = value >= 0 ? value : 0;} }
        private int _cockatrice_spawn_worldgen = 4;

        public int COCKATRICE_SPAWN_CHANCE_RUNTIME { get { return _cockatrice_spawn_runtime; } set { _cockatrice_spawn_runtime = value >= 0 ? value : 0; } }
        private int _cockatrice_spawn_runtime = 3;
        public int DIREWOLF_SPAWN_CHANCE_WORLDGEN { get { return _direwolf_spawn_worldgen; } set {_direwolf_spawn_worldgen = value >= 0 ? value : 0;} }
        private int _direwolf_spawn_worldgen = 4;

        public int DIREWOLF_SPAWN_CHANCE_RUNTIME { get { return _direwolf_spawn_runtime; } set { _direwolf_spawn_runtime = value >= 0 ? value : 0; } }
        private int _direwolf_spawn_runtime = 4;
        public int FAUNLING_SPAWN_CHANCE_WORLDGEN { get { return _faunling_spawn_worldgen; } set {_faunling_spawn_worldgen = value >= 0 ? value : 0;} }
        private int _faunling_spawn_worldgen = 4;

        public int FAUNLING_SPAWN_CHANCE_RUNTIME { get { return _faunling_spawn_runtime; } set { _faunling_spawn_runtime = value >= 0 ? value : 0; } }
        private int _faunling_spawn_runtime = 4;
        public int GEODECRAB_SPAWN_CHANCE_WORLDGEN { get { return _geodecrab_spawn_worldgen; } set {_geodecrab_spawn_worldgen = value >= 0 ? value : 0;} }
        private int _geodecrab_spawn_worldgen = 3;

        public int GEODECRAB_SPAWN_CHANCE_RUNTIME { get { return _geodecrab_spawn_runtime; } set { _geodecrab_spawn_runtime = value >= 0 ? value : 0; } }
        private int _geodecrab_spawn_runtime = 2;
        public int GOLEM_SPAWN_CHANCE_WORLDGEN { get { return _golem_spawn_worldgen; } set {_golem_spawn_worldgen = value >= 0 ? value : 0;} }
        private int _golem_spawn_worldgen = 3;

        public int GOLEM_SPAWN_CHANCE_RUNTIME { get { return _golem_spawn_runtime; } set { _golem_spawn_runtime = value >= 0 ? value : 0; } }
        private int _golem_spawn_runtime = 2;
        public int HELLBOAR_SPAWN_CHANCE_WORLDGEN { get { return _hellboar_spawn_worldgen; } set {_hellboar_spawn_worldgen = value >= 0 ? value : 0;} }
        private int _hellboar_spawn_worldgen = 4;

        public int HELLBOAR_SPAWN_CHANCE_RUNTIME { get { return _hellboar_spawn_runtime; } set { _hellboar_spawn_runtime = value >= 0 ? value : 0; } }
        private int _hellboar_spawn_runtime = 4;
        public int HORSE_SPAWN_CHANCE_WORLDGEN { get { return _horse_spawn_worldgen; } set {_horse_spawn_worldgen = value >= 0 ? value : 0;} }
        private int _horse_spawn_worldgen = 4;

        public int HORSE_SPAWN_CHANCE_RUNTIME { get { return _horse_spawn_runtime; } set { _horse_spawn_runtime = value >= 0 ? value : 0; } }
        private int _horse_spawn_runtime = 3;
        public int OSTRICH_SPAWN_CHANCE_WORLDGEN { get { return _ostrich_spawn_worldgen; } set {_ostrich_spawn_worldgen = value >= 0 ? value : 0;} }
        private int _ostrich_spawn_worldgen = 4;

        public int OSTRICH_SPAWN_CHANCE_RUNTIME { get { return _ostrich_spawn_runtime; } set { _ostrich_spawn_runtime = value >= 0 ? value : 0; } }
        private int _ostrich_spawn_runtime = 2;

        public FeverstoneWildsConfig() { }

        public FeverstoneWildsConfig(FeverstoneWildsConfig previousConfig)
        {
            // Land Creatures
            FSWCockatriceEnabled = previousConfig.FSWCockatriceEnabled;
            FSWTameCockatriceEnabled = previousConfig.FSWCockatriceEnabled;
            FSWFaunlingEnabled = previousConfig.FSWFaunlingEnabled;
            FSWWildDirewolfEnabled = previousConfig.FSWWildDirewolfEnabled;
            FSWTameDirewolfEnabled = previousConfig.FSWTameDirewolfEnabled;
            FSWWildDirewolfPupEnabled = previousConfig.FSWWildDirewolfPupEnabled;
            FSWTameDirewolfPupEnabled = previousConfig.FSWTameDirewolfPupEnabled;
            FSWHellboarEnabled = previousConfig.FSWHellboarEnabled;
            FSWHorseEnabled = previousConfig.FSWHorseEnabled;
            FSWFoalEnabled = previousConfig.FSWFoalEnabled;
            FSWBisonEnabled = previousConfig.FSWBisonEnabled;
            FSWBisonCalfEnabled = previousConfig.FSWBisonCalfEnabled;
            FSWGiraffeEnabled = previousConfig.FSWGiraffeEnabled;
            FSWGolemEnabled = previousConfig.FSWGolemEnabled;
            FSWGeodeCrabEnabled = previousConfig.FSWGeodeCrabEnabled;
            FSWOstrichEnabled = previousConfig.FSWOstrichEnabled;
            FSWSpiderEnabled = previousConfig.FSWSpiderEnabled;
            FSWScorpionEnabled = previousConfig.FSWScorpionEnabled;
            FSWToadEnabled = previousConfig.FSWToadEnabled;

            // Water Creatures
            FSWBuromenfishEnabled = previousConfig.FSWBuromenfishEnabled;
            FSWDiscusFishEnabled = previousConfig.FSWDiscusFishEnabled;
            FSWEelEnabled = previousConfig.FSWEelEnabled;
            FSWOrcaEnabled = previousConfig.FSWOrcaEnabled;
            FSWSharksEnabled = previousConfig.FSWSharksEnabled;
            FSWStingrayEnabled = previousConfig.FSWStingrayEnabled;
            FSWGhostfishEnabled = previousConfig.FSWGhostfishEnabled;

            // Golem Types
            FSWCopperGolemEnabled = previousConfig.FSWCopperGolemEnabled;
            FSWTinGolemEnabled = previousConfig.FSWTinGolemEnabled;
            FSWIronGolemEnabled = previousConfig.FSWIronGolemEnabled;

            // AnimalCages Config
            AllowCagedFSWCreatures = previousConfig.AllowCagedFSWCreatures;

            // ConfigLib Configs
             BISON_SPAWN_CHANCE_WORLDGEN = previousConfig.BISON_SPAWN_CHANCE_WORLDGEN;
            BISON_SPAWN_CHANCE_RUNTIME = previousConfig.BISON_SPAWN_CHANCE_RUNTIME;
            COCKATRICE_SPAWN_CHANCE_WORLDGEN = previousConfig.COCKATRICE_SPAWN_CHANCE_WORLDGEN;
            COCKATRICE_SPAWN_CHANCE_RUNTIME = previousConfig.COCKATRICE_SPAWN_CHANCE_RUNTIME;
            DIREWOLF_SPAWN_CHANCE_WORLDGEN = previousConfig.DIREWOLF_SPAWN_CHANCE_WORLDGEN;
            DIREWOLF_SPAWN_CHANCE_RUNTIME = previousConfig.DIREWOLF_SPAWN_CHANCE_RUNTIME;
            FAUNLING_SPAWN_CHANCE_WORLDGEN = previousConfig.FAUNLING_SPAWN_CHANCE_WORLDGEN;
            FAUNLING_SPAWN_CHANCE_RUNTIME = previousConfig.FAUNLING_SPAWN_CHANCE_RUNTIME;
            GEODECRAB_SPAWN_CHANCE_WORLDGEN = previousConfig.GEODECRAB_SPAWN_CHANCE_WORLDGEN;
            GEODECRAB_SPAWN_CHANCE_RUNTIME = previousConfig.GEODECRAB_SPAWN_CHANCE_RUNTIME;
            GOLEM_SPAWN_CHANCE_WORLDGEN = previousConfig.GOLEM_SPAWN_CHANCE_WORLDGEN;
            GOLEM_SPAWN_CHANCE_RUNTIME = previousConfig.GOLEM_SPAWN_CHANCE_RUNTIME;
            HELLBOAR_SPAWN_CHANCE_WORLDGEN = previousConfig.HELLBOAR_SPAWN_CHANCE_WORLDGEN;
            HELLBOAR_SPAWN_CHANCE_RUNTIME = previousConfig.HELLBOAR_SPAWN_CHANCE_RUNTIME;
            HORSE_SPAWN_CHANCE_WORLDGEN = previousConfig.HORSE_SPAWN_CHANCE_WORLDGEN;
            HORSE_SPAWN_CHANCE_RUNTIME = previousConfig.HORSE_SPAWN_CHANCE_RUNTIME;
            OSTRICH_SPAWN_CHANCE_WORLDGEN = previousConfig.OSTRICH_SPAWN_CHANCE_WORLDGEN;
            OSTRICH_SPAWN_CHANCE_RUNTIME = previousConfig.OSTRICH_SPAWN_CHANCE_RUNTIME;
        }
    }
}