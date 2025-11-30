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

        /* public string configlibhints = "----- Config Lib Spawn Rates -----";
        public float BISON_SPAWN_CHANCE_WORLDGEN { get { return _bison_spawn_worldgen; } set {_bison_spawn_worldgen = value >= 0 ? (value*0.0005f) : 0;} }
        private float _bison_spawn_worldgen = 0.002f;

        public float BISON_SPAWN_CHANCE_RUNTIME { get { return _bison_spawn_runtime; } set { _bison_spawn_runtime = value >= 0 ? (value*0.00001f) : 0; } }
        private float _bison_spawn_runtime = 0.00003f;
        public float COCKATRICE_SPAWN_CHANCE_WORLDGEN { get { return _cockatrice_spawn_worldgen; } set {_cockatrice_spawn_worldgen = value >= 0 ? (value*0.001f) : 0;} }
        private float _cockatrice_spawn_worldgen = 0.004f;

        public float COCKATRICE_SPAWN_CHANCE_RUNTIME { get { return _cockatrice_spawn_runtime; } set { _cockatrice_spawn_runtime = value >= 0 ? (value*0.00001f) : 0; } }
        private float _cockatrice_spawn_runtime = 0.00003f;
        public float DIREWOLF_SPAWN_CHANCE_WORLDGEN { get { return _direwolf_spawn_worldgen; } set {_direwolf_spawn_worldgen = value >= 0 ? (value*0.0025f) : 0;} }
        private float _direwolf_spawn_worldgen = 0.010f;

        public float DIREWOLF_SPAWN_CHANCE_RUNTIME { get { return _direwolf_spawn_runtime; } set { _direwolf_spawn_runtime = value >= 0 ? (value*0.00001f) : 0; } }
        private float _direwolf_spawn_runtime = 0.00004f;
        public float FAUNLING_SPAWN_CHANCE_WORLDGEN { get { return _faunling_spawn_worldgen; } set {_faunling_spawn_worldgen = value >= 0 ? (value*0.002375f) : 0;} }
        private float _faunling_spawn_worldgen = 0.0095f;

        public float FAUNLING_SPAWN_CHANCE_RUNTIME { get { return _faunling_spawn_runtime; } set { _faunling_spawn_runtime = value >= 0 ? (value*0.000005f) : 0; } }
        private float _faunling_spawn_runtime = 0.00002f;
        public float GEODECRAB_SPAWN_CHANCE_WORLDGEN { get { return _geodecrab_spawn_worldgen; } set {_geodecrab_spawn_worldgen = value >= 0 ? (value*0.005f) : 0;} }
        private float _geodecrab_spawn_worldgen = 0.02f;

        public float GEODECRAB_SPAWN_CHANCE_RUNTIME { get { return _geodecrab_spawn_runtime; } set { _geodecrab_spawn_runtime = value >= 0 ? (value*0.000005f) : 0; } }
        private float _geodecrab_spawn_runtime = 0.0006f;
        public float GOLEM_SPAWN_CHANCE_WORLDGEN { get { return _golem_spawn_worldgen; } set {_golem_spawn_worldgen = value >= 0 ? (value*0.002f) : 0;} }
        private float _golem_spawn_worldgen = 0.006f;

        public float GOLEM_SPAWN_CHANCE_RUNTIME { get { return _golem_spawn_runtime; } set { _golem_spawn_runtime = value >= 0 ? (value*0.000005f) : 0; } }
        private float _golem_spawn_runtime = 0.00001f;
        public float HELLBOAR_SPAWN_CHANCE_WORLDGEN { get { return _hellboar_spawn_worldgen; } set {_hellboar_spawn_worldgen = value >= 0 ? (value*0.0025f) : 0;} }
        private float _hellboar_spawn_worldgen = 0.010f;

        public float HELLBOAR_SPAWN_CHANCE_RUNTIME { get { return _hellboar_spawn_runtime; } set { _hellboar_spawn_runtime = value >= 0 ? (value*0.00001f) : 0; } }
        private float _hellboar_spawn_runtime = 0.00004f;
        public float HORSE_SPAWN_CHANCE_WORLDGEN { get { return _horse_spawn_worldgen; } set {_horse_spawn_worldgen = value >= 0 ? (value*0.0025f) : 0;} }
        private float _horse_spawn_worldgen = 0.010f;

        public float HORSE_SPAWN_CHANCE_RUNTIME { get { return _horse_spawn_runtime; } set { _horse_spawn_runtime = value >= 0 ? (value*0.00001f) : 0; } }
        private float _horse_spawn_runtime = 0.00003f;
        public float OSTRICH_SPAWN_CHANCE_WORLDGEN { get { return _ostrich_spawn_worldgen; } set {_ostrich_spawn_worldgen = value >= 0 ? (value*0.00125f) : 0;} }
        private float _ostrich_spawn_worldgen = 0.005f;

        public float OSTRICH_SPAWN_CHANCE_RUNTIME { get { return _ostrich_spawn_runtime; } set { _ostrich_spawn_runtime = value >= 0 ? (value*0.00001f) : 0; } }
        private float _ostrich_spawn_runtime = 0.00002f; */

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
            /* BISON_SPAWN_CHANCE_WORLDGEN = previousConfig.BISON_SPAWN_CHANCE_WORLDGEN;
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
            OSTRICH_SPAWN_CHANCE_RUNTIME = previousConfig.OSTRICH_SPAWN_CHANCE_RUNTIME; */
        }
    }
}