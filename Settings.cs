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
        [Section("CR.SECTION")]

        [Name("CR.RESPAWN_TIME")]
        [Description("CR.DESC_RESPAWN_TIME")]
        [Choice(new string[] { "Fast (5 days)", "Normal (15 days)", "Slow (30 days)", "Realistic (60 days)" })]
        public int respawnPreset = 1;   // 0=5d  1=15d  2=30d  3=60d

        [Name("CR.MIN_COAL")]
        [Description("CR.DESC_MIN_COAL")]
        [Slider(1, 8)]
        public int minCoal = 2;

        [Name("CR.MAX_COAL")]
        [Description("CR.DESC_MAX_COAL")]
        [Slider(1, 8)]
        public int maxCoal = 4;

        [Name("CR.SCAN_RADIUS")]
        [Description("CR.DESC_SCAN_RADIUS")]
        [Slider(1, 12)]
        public int scanRadius = 4;
    }
}
