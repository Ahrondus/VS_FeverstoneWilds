namespace FeverstoneWilds.Config
{
    public class FeverstoneWildsConfig
    {
        public bool FaunlingEnabled = true;
        public bool WildDirewolfEnabled = true;
        public bool TameDirewolfEnabled = true;
        public bool WildDirewolfPupEnabled = true;
        public bool TameDirewolfPupEnabled = true;
        public bool HorseEnabled = true;
        public bool FoalEnabled = true;
        public bool GolemEnabled = true;
        public bool GeodeCrabEnabled = true;
        public bool SpiderEnabled = true;
        public bool ToadEnabled = true;
        public bool BuromenfishEnabled = true;
        public bool DiscusFishEnabled = true;
        public bool BullsharkEnabled = true;
        public bool StingrayEnabled = true;

        public FeverstoneWildsConfig() { }

        public FeverstoneWildsConfig(FeverstoneWildsConfig previousConfig)
        {
            FaunlingEnabled = previousConfig.FaunlingEnabled;
            WildDirewolfEnabled = previousConfig.WildDirewolfEnabled;
            TameDirewolfEnabled = previousConfig.TameDirewolfEnabled;
            WildDirewolfPupEnabled = previousConfig.WildDirewolfPupEnabled;
            TameDirewolfPupEnabled = previousConfig.TameDirewolfPupEnabled;
            HorseEnabled = previousConfig.HorseEnabled;
            FoalEnabled = previousConfig.FoalEnabled;
            GolemEnabled = previousConfig.GolemEnabled;
            GeodeCrabEnabled = previousConfig.GeodeCrabEnabled;
            SpiderEnabled = previousConfig.SpiderEnabled;
            ToadEnabled = previousConfig.ToadEnabled;
            BuromenfishEnabled = previousConfig.BuromenfishEnabled;
            DiscusFishEnabled = previousConfig.DiscusFishEnabled;
            BullsharkEnabled = previousConfig.BullsharkEnabled;
            StingrayEnabled = previousConfig.StingrayEnabled;
        }
    }
}