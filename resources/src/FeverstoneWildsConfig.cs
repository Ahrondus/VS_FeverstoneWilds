namespace FeverstoneWilds
{
    public class FeverstoneWildsConfig
    {
        public static FeverstoneWildsConfig Loaded { get; set; } = new FeverstoneWildsConfig();

        public bool FaunlingEnabled { get; set; } = true;
        public bool WildDirewolfEnabled { get; set; } = true;
        public bool TameDirewolfEnabled { get; set; } = true;
        public bool WildDirewolfPupEnabled { get; set; } = true;
        public bool TameDirewolfPupEnabled { get; set; } = true;
        public bool HorseEnabled { get; set; } = true;
        public bool FoalEnabled { get; set; } = true;
        public bool GolemEnabled { get; set; } = true;
        public bool GeodeCrabEnabled { get; set; } = true;
        public bool SpiderEnabled { get; set; } = true;
        public bool ToadEnabled { get; set; } = true;
        public bool BuromenfishEnabled { get; set; } = true;
        public bool DiscusFishEnabled { get; set; } = true;
        public bool BullsharkEnabled { get; set; } = true;
        public bool StingrayEnabled { get; set; } = true;
    }
}