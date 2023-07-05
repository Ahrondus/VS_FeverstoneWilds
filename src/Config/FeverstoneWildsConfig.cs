namespace FeverstoneWilds.Config
{
    public class FeverstoneWildsConfig
    {
        // Land Creatures
        public bool BisonEnabled = true;
        public bool BisonCalfEnabled = true;
        public bool CockatriceEnabled = true;
        public bool WildDirewolfEnabled = true;
        public bool TameDirewolfEnabled = true;
        public bool WildDirewolfPupEnabled = true;
        public bool TameDirewolfPupEnabled = true;
        public bool FaunlingEnabled = true;
        public bool FoalEnabled = true;
        public bool GeodeCrabEnabled = true;
        public bool GiraffeEnabled = true;
        public bool GolemEnabled = true;
        public bool HorseEnabled = true;
        public bool OstrichEnabled = true;
        public bool SpiderEnabled = true;
        public bool ToadEnabled = true;

        // Water Creatures
        public bool BuromenfishEnabled = true;
        public bool DiscusFishEnabled = true;
        public bool EelEnabled = true;
        public bool OrcaEnabled = true;
        public bool SharksEnabled = true;
        public bool StingrayEnabled = true;

        public FeverstoneWildsConfig() { }

        public FeverstoneWildsConfig(FeverstoneWildsConfig previousConfig)
        {
            // Land Creatures
            CockatriceEnabled = previousConfig.CockatriceEnabled;
            FaunlingEnabled = previousConfig.FaunlingEnabled;
            WildDirewolfEnabled = previousConfig.WildDirewolfEnabled;
            TameDirewolfEnabled = previousConfig.TameDirewolfEnabled;
            WildDirewolfPupEnabled = previousConfig.WildDirewolfPupEnabled;
            TameDirewolfPupEnabled = previousConfig.TameDirewolfPupEnabled;
            HorseEnabled = previousConfig.HorseEnabled;
            FoalEnabled = previousConfig.FoalEnabled;
            BisonEnabled = previousConfig.BisonEnabled;
            BisonCalfEnabled = previousConfig.BisonCalfEnabled;
            GiraffeEnabled = previousConfig.GiraffeEnabled;
            GolemEnabled = previousConfig.GolemEnabled;
            GeodeCrabEnabled = previousConfig.GeodeCrabEnabled;
            OstrichEnabled = previousConfig.OstrichEnabled;
            SpiderEnabled = previousConfig.SpiderEnabled;
            ToadEnabled = previousConfig.ToadEnabled;

            // Water Creatures
            BuromenfishEnabled = previousConfig.BuromenfishEnabled;
            DiscusFishEnabled = previousConfig.DiscusFishEnabled;
            EelEnabled = previousConfig.EelEnabled;
            OrcaEnabled = previousConfig.OrcaEnabled;
            SharksEnabled = previousConfig.SharksEnabled;
            StingrayEnabled = previousConfig.StingrayEnabled;
        }
    }
}