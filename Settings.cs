using ModSettings;

namespace WildernessRenewableMod
{
    internal static class Settings
    {
        internal static WildernessRenewableSettings instance;

        internal static void OnLoad()
        {
            instance = new WildernessRenewableSettings();
            instance.AddToModSettings("Wilderness Renewable");
        }
    }

    internal class WildernessRenewableSettings : JsonModSettings
    {
        // ── COAL ─────────────────────────────────────────────────────────────────

        [Section("WR.SECTION_COAL", Localize = true)]

        [Name("WR.ENABLE_COAL", Localize = true)]
        [Description("WR.DESC_ENABLE_COAL", Localize = true)]
        public bool enableCoal = true;

        [Name("WR.COAL_RESPAWN_TIME", Localize = true)]
        [Description("WR.DESC_RESPAWN_TIME", Localize = true)]
        [Choice(new string[] { "WR.CHOICE_DAILY", "WR.CHOICE_FAST", "WR.CHOICE_NORMAL", "WR.CHOICE_SLOW", "WR.CHOICE_REALISTIC" }, Localize = true)]
        public int coalRespawnPreset = 2;   // 0=1d  1=5d  2=15d  3=30d  4=60d

        [Name("WR.MIN_COAL", Localize = true)]
        [Description("WR.DESC_MIN_COAL", Localize = true)]
        [Slider(1, 20)]
        public int minCoal = 2;

        [Name("WR.MAX_COAL", Localize = true)]
        [Description("WR.DESC_MAX_COAL", Localize = true)]
        [Slider(1, 20)]
        public int maxCoal = 4;

        [Name("WR.COAL_SCAN_RADIUS", Localize = true)]
        [Description("WR.DESC_COAL_SCAN_RADIUS", Localize = true)]
        [Slider(1, 12)]
        public int coalScanRadius = 4;

        [Name("WR.COAL_LOCATION", Localize = true)]
        [Description("WR.DESC_COAL_LOCATION", Localize = true)]
        [Choice(new string[] { "WR.LOC_CAVES", "WR.LOC_MINES", "WR.LOC_BOTH", "WR.LOC_EVERYWHERE" }, Localize = true)]
        public int coalLocationPreset = 2;  // 0=caves  1=mines  2=both  3=everywhere

        // ── LARGE STICKS ─────────────────────────────────────────────────────────

        [Section("WR.SECTION_STICKS", Localize = true)]

        [Name("WR.ENABLE_STICKS", Localize = true)]
        [Description("WR.DESC_ENABLE_STICKS", Localize = true)]
        public bool enableSticks = true;

        [Name("WR.STICKS_RESPAWN_TIME", Localize = true)]
        [Description("WR.DESC_RESPAWN_TIME", Localize = true)]
        [Choice(new string[] { "WR.CHOICE_DAILY", "WR.CHOICE_FAST", "WR.CHOICE_NORMAL", "WR.CHOICE_SLOW", "WR.CHOICE_REALISTIC" }, Localize = true)]
        public int sticksRespawnPreset = 1; // default: 5 days

        [Name("WR.MIN_STICKS", Localize = true)]
        [Description("WR.DESC_MIN_STICKS", Localize = true)]
        [Slider(1, 10)]
        public int minSticks = 2;

        [Name("WR.MAX_STICKS", Localize = true)]
        [Description("WR.DESC_MAX_STICKS", Localize = true)]
        [Slider(1, 10)]
        public int maxSticks = 5;

        // ── SMALL STICKS ─────────────────────────────────────────────────────────

        [Section("WR.SECTION_SMALL_STICKS", Localize = true)]

        [Name("WR.ENABLE_SMALL_STICKS", Localize = true)]
        [Description("WR.DESC_ENABLE_SMALL_STICKS", Localize = true)]
        public bool enableSmallSticks = true;

        [Name("WR.SMALL_STICKS_RESPAWN_TIME", Localize = true)]
        [Description("WR.DESC_RESPAWN_TIME", Localize = true)]
        [Choice(new string[] { "WR.CHOICE_DAILY", "WR.CHOICE_FAST", "WR.CHOICE_NORMAL", "WR.CHOICE_SLOW", "WR.CHOICE_REALISTIC" }, Localize = true)]
        public int smallSticksRespawnPreset = 1; // default: 5 days

        [Name("WR.MIN_SMALL_STICKS", Localize = true)]
        [Description("WR.DESC_MIN_SMALL_STICKS", Localize = true)]
        [Slider(1, 10)]
        public int minSmallSticks = 3;

        [Name("WR.MAX_SMALL_STICKS", Localize = true)]
        [Description("WR.DESC_MAX_SMALL_STICKS", Localize = true)]
        [Slider(1, 10)]
        public int maxSmallSticks = 6;

        // ── RAVEN FEATHERS ────────────────────────────────────────────────────────

        [Section("WR.SECTION_FEATHERS", Localize = true)]

        [Name("WR.ENABLE_FEATHERS", Localize = true)]
        [Description("WR.DESC_ENABLE_FEATHERS", Localize = true)]
        public bool enableFeathers = true;

        [Name("WR.FEATHERS_RESPAWN_TIME", Localize = true)]
        [Description("WR.DESC_RESPAWN_TIME", Localize = true)]
        [Choice(new string[] { "WR.CHOICE_DAILY", "WR.CHOICE_FAST", "WR.CHOICE_NORMAL", "WR.CHOICE_SLOW", "WR.CHOICE_REALISTIC" }, Localize = true)]
        public int feathersRespawnPreset = 1; // default: 5 days

        [Name("WR.MIN_FEATHERS", Localize = true)]
        [Description("WR.DESC_MIN_FEATHERS", Localize = true)]
        [Slider(1, 6)]
        public int minFeathers = 1;

        [Name("WR.MAX_FEATHERS", Localize = true)]
        [Description("WR.DESC_MAX_FEATHERS", Localize = true)]
        [Slider(1, 6)]
        public int maxFeathers = 3;
    }
}
