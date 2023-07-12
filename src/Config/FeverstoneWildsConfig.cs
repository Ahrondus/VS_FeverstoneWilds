namespace FeverstoneWilds.Config
{
    public class FeverstoneWildsConfig
    {
        // Land Creatures
        public bool FSWBisonEnabled = true;
        public bool FSWBisonCalfEnabled = true;
        public bool FSWCockatriceEnabled = true;
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
        public bool FSWToadEnabled = true;

        // Water Creatures
        public bool FSWBuromenfishEnabled = true;
        public bool FSWDiscusFishEnabled = true;
        public bool FSWEelEnabled = true;
        public bool FSWOrcaEnabled = true;
        public bool FSWSharksEnabled = true;
        public bool FSWStingrayEnabled = true;

        public FeverstoneWildsConfig() { }

        public FeverstoneWildsConfig(FeverstoneWildsConfig previousConfig)
        {
            // Land Creatures
            FSWCockatriceEnabled = previousConfig.FSWCockatriceEnabled;
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
            FSWToadEnabled = previousConfig.FSWToadEnabled;

            // Water Creatures
            FSWBuromenfishEnabled = previousConfig.FSWBuromenfishEnabled;
            FSWDiscusFishEnabled = previousConfig.FSWDiscusFishEnabled;
            FSWEelEnabled = previousConfig.FSWEelEnabled;
            FSWOrcaEnabled = previousConfig.FSWOrcaEnabled;
            FSWSharksEnabled = previousConfig.FSWSharksEnabled;
            FSWStingrayEnabled = previousConfig.FSWStingrayEnabled;
        }
    }
}