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

        // Golem Types
        public string eachgolem = "----- Golems by Type -----";
        public bool FSWCopperGolemEnabled = true;
        public bool FSWTinGolemEnabled = true;
        public bool FSWIronGolemEnabled = true;

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

            // Golem Types
            FSWCopperGolemEnabled = previousConfig.FSWCopperGolemEnabled;
            FSWTinGolemEnabled = previousConfig.FSWTinGolemEnabled;
            FSWIronGolemEnabled = previousConfig.FSWIronGolemEnabled;
        }
    }
}