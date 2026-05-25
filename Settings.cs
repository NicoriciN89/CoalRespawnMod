using ModSettings;

namespace CoalRespawnMod
{
    internal static class Settings
    {
        internal static CoalRespawnSettings instance;

        internal static void OnLoad()
        {
            instance = new CoalRespawnSettings();
            instance.AddToModSettings("Coal Respawn");
        }
    }

    internal class CoalRespawnSettings : JsonModSettings
    {
        [Section("CR.SECTION", Localize = true)]

        [Name("CR.RESPAWN_TIME", Localize = true)]
        [Description("CR.DESC_RESPAWN_TIME", Localize = true)]
        [Choice(new string[] { "CR.CHOICE_DAILY", "CR.CHOICE_FAST", "CR.CHOICE_NORMAL", "CR.CHOICE_SLOW", "CR.CHOICE_REALISTIC" }, Localize = true)]
        public int respawnPreset = 2;   // 0=1d  1=5d  2=15d  3=30d  4=60d

        [Name("CR.MIN_COAL", Localize = true)]
        [Description("CR.DESC_MIN_COAL", Localize = true)]
        [Slider(1, 8)]
        public int minCoal = 2;

        [Name("CR.MAX_COAL", Localize = true)]
        [Description("CR.DESC_MAX_COAL", Localize = true)]
        [Slider(1, 8)]
        public int maxCoal = 4;

        [Name("CR.SCAN_RADIUS", Localize = true)]
        [Description("CR.DESC_SCAN_RADIUS", Localize = true)]
        [Slider(1, 12)]
        public int scanRadius = 4;

        [Name("CR.LOCATION", Localize = true)]
        [Description("CR.DESC_LOCATION", Localize = true)]
        [Choice(new string[] { "CR.LOC_CAVES", "CR.LOC_MINES", "CR.LOC_BOTH", "CR.LOC_EVERYWHERE" }, Localize = true)]
        public int locationPreset = 2;   // 0=caves  1=mines  2=caves+mines  3=everywhere
    }
}
