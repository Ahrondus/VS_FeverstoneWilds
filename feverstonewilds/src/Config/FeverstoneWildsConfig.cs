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

        // Complex Creatures
        public string complexheader = "----- Complex Creatures -----";
        public bool FSWGriffonEnabled = true;

        // Water Creatures
        public string waterheader = "----- Water Creatures -----";
        public bool FSWBuromenfishEnabled = true;
        public bool FSWDiscusFishEnabled = true;
        public bool FSWEelEnabled = true;
        public bool FSWGhostfishEnabled = true;
        public bool FSWOrcaEnabled = true;
        public bool FSWSharksEnabled = true;
        public bool FSWStingrayEnabled = true;

        // Golem Types
        public string eachgolem = "----- Golems by Type -----";
        public bool FSWCopperGolemEnabled = true;
        public bool FSWTinGolemEnabled = true;
        public bool FSWIronGolemEnabled = true;

        // Faunling Sapling Behavior
        public string faunlingsapling = "----- Faunling Sapling Behavior -----";
        public bool FSWFaunlingSaplingEnabled = true;

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

        public int GRIFFON_SPAWN_CHANCE_WORLDGEN { get { return _griffon_spawn_worldgen; } set {_griffon_spawn_worldgen = value >= 0 ? value : 0;} }
        private int _griffon_spawn_worldgen = 2;
        public int GRIFFON_SPAWN_CHANCE_RUNTIME { get { return _griffon_spawn_runtime; } set { _griffon_spawn_runtime = value >= 0 ? value : 0; } }
        private int _griffon_spawn_runtime = 2;

        public int BUROMENFISH_SPAWN_CHANCE_WORLDGEN { get { return _buromenfish_spawn_worldgen; } set {_buromenfish_spawn_worldgen = value >= 0 ? value : 0;} }
        private int _buromenfish_spawn_worldgen = 2;
        public int BUROMENFISH_SPAWN_CHANCE_RUNTIME { get { return _buromenfish_spawn_runtime; } set { _buromenfish_spawn_runtime = value >= 0 ? value : 0; } }
        private int _buromenfish_spawn_runtime = 4;
        public int DISCUS_SPAWN_CHANCE_WORLDGEN { get { return _discus_spawn_worldgen; } set {_discus_spawn_worldgen = value >= 0 ? value : 0;} }
        private int _discus_spawn_worldgen = 4;

        public int DISCUS_SPAWN_CHANCE_RUNTIME { get { return _discus_spawn_runtime; } set { _discus_spawn_runtime = value >= 0 ? value : 0; } }
        private int _discus_spawn_runtime = 4;
        public int EEL_SPAWN_CHANCE_WORLDGEN { get { return _eel_spawn_worldgen; } set {_eel_spawn_worldgen = value >= 0 ? value : 0;} }
        private int _eel_spawn_worldgen = 4;

        public int EEL_SPAWN_CHANCE_RUNTIME { get { return _eel_spawn_runtime; } set { _eel_spawn_runtime = value >= 0 ? value : 0; } }
        private int _eel_spawn_runtime = 4;
        public int GHOSTFISH_SPAWN_CHANCE_WORLDGEN { get { return _ghostfish_spawn_worldgen; } set {_ghostfish_spawn_worldgen = value >= 0 ? value : 0;} }
        private int _ghostfish_spawn_worldgen = 4;

        public int GHOSTFISH_SPAWN_CHANCE_RUNTIME { get { return _ghostfish_spawn_runtime; } set { _ghostfish_spawn_runtime = value >= 0 ? value : 0; } }
        private int _ghostfish_spawn_runtime = 4;
        public int ORCA_SPAWN_CHANCE_WORLDGEN { get { return _orca_spawn_worldgen; } set {_orca_spawn_worldgen = value >= 0 ? value : 0;} }
        private int _orca_spawn_worldgen = 4;

        public int ORCA_SPAWN_CHANCE_RUNTIME { get { return _orca_spawn_runtime; } set { _orca_spawn_runtime = value >= 0 ? value : 0; } }
        private int _orca_spawn_runtime = 4;
        public int SHARKS_SPAWN_CHANCE_WORLDGEN { get { return _sharks_spawn_worldgen; } set {_sharks_spawn_worldgen = value >= 0 ? value : 0;} }
        private int _sharks_spawn_worldgen = 4;

        public int SHARKS_SPAWN_CHANCE_RUNTIME { get { return _sharks_spawn_runtime; } set { _sharks_spawn_runtime = value >= 0 ? value : 0; } }
        private int _sharks_spawn_runtime = 4;
        public int STINGRAY_SPAWN_CHANCE_WORLDGEN { get { return _stingray_spawn_worldgen; } set {_stingray_spawn_worldgen = value >= 0 ? value : 0;} }
        private int _stingray_spawn_worldgen = 4;

        public int STINGRAY_SPAWN_CHANCE_RUNTIME { get { return _stingray_spawn_runtime; } set { _stingray_spawn_runtime = value >= 0 ? value : 0; } }
        private int _stingray_spawn_runtime = 4;

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

            // Complex Creatures
            FSWGriffonEnabled = previousConfig.FSWGriffonEnabled;

            // Water Creatures
            FSWBuromenfishEnabled = previousConfig.FSWBuromenfishEnabled;
            FSWDiscusFishEnabled = previousConfig.FSWDiscusFishEnabled;
            FSWEelEnabled = previousConfig.FSWEelEnabled;
            FSWGhostfishEnabled = previousConfig.FSWGhostfishEnabled;
            FSWOrcaEnabled = previousConfig.FSWOrcaEnabled;
            FSWSharksEnabled = previousConfig.FSWSharksEnabled;
            FSWStingrayEnabled = previousConfig.FSWStingrayEnabled;

            // Golem Types
            FSWCopperGolemEnabled = previousConfig.FSWCopperGolemEnabled;
            FSWTinGolemEnabled = previousConfig.FSWTinGolemEnabled;
            FSWIronGolemEnabled = previousConfig.FSWIronGolemEnabled;

            // Faunling Sapling Behavior
            FSWFaunlingSaplingEnabled = previousConfig.FSWFaunlingSaplingEnabled;

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

            GRIFFON_SPAWN_CHANCE_WORLDGEN = previousConfig.GRIFFON_SPAWN_CHANCE_WORLDGEN;
            GRIFFON_SPAWN_CHANCE_RUNTIME = previousConfig.GRIFFON_SPAWN_CHANCE_RUNTIME;
            
            BUROMENFISH_SPAWN_CHANCE_WORLDGEN = previousConfig.BUROMENFISH_SPAWN_CHANCE_WORLDGEN;
            BUROMENFISH_SPAWN_CHANCE_RUNTIME = previousConfig.BUROMENFISH_SPAWN_CHANCE_RUNTIME;
            DISCUS_SPAWN_CHANCE_WORLDGEN = previousConfig.DISCUS_SPAWN_CHANCE_WORLDGEN;
            DISCUS_SPAWN_CHANCE_RUNTIME = previousConfig.DISCUS_SPAWN_CHANCE_RUNTIME;
            EEL_SPAWN_CHANCE_WORLDGEN = previousConfig.EEL_SPAWN_CHANCE_WORLDGEN;
            EEL_SPAWN_CHANCE_RUNTIME = previousConfig.EEL_SPAWN_CHANCE_RUNTIME;
            GHOSTFISH_SPAWN_CHANCE_WORLDGEN = previousConfig.GHOSTFISH_SPAWN_CHANCE_WORLDGEN;
            GHOSTFISH_SPAWN_CHANCE_RUNTIME = previousConfig.GHOSTFISH_SPAWN_CHANCE_RUNTIME;
            ORCA_SPAWN_CHANCE_WORLDGEN = previousConfig.ORCA_SPAWN_CHANCE_WORLDGEN;
            ORCA_SPAWN_CHANCE_RUNTIME = previousConfig.ORCA_SPAWN_CHANCE_RUNTIME;
            SHARKS_SPAWN_CHANCE_WORLDGEN = previousConfig.SHARKS_SPAWN_CHANCE_WORLDGEN;
            SHARKS_SPAWN_CHANCE_RUNTIME = previousConfig.SHARKS_SPAWN_CHANCE_RUNTIME;
            STINGRAY_SPAWN_CHANCE_WORLDGEN = previousConfig.STINGRAY_SPAWN_CHANCE_WORLDGEN;
            STINGRAY_SPAWN_CHANCE_RUNTIME = previousConfig.STINGRAY_SPAWN_CHANCE_RUNTIME;
        }
    }
}