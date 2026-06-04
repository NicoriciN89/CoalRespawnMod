using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace WildernessRenewableMod
{
    internal static class LocalizationManager
    {
        private static Dictionary<string, Dictionary<string, string>> _data;

        private static Dictionary<string, Dictionary<string, string>> Data =>
            _data ?? (_data = Load());

        internal static void Reload() => _data = null;

        private const string EmbeddedResource = "WildernessRenewableMod.localization.json";

        private static Dictionary<string, Dictionary<string, string>> Load()
        {
            // 1. User override: UserData/WildernessRenewableMod/localization.json
            string dllDir   = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            string userPath = Path.Combine(
                Path.GetDirectoryName(dllDir) ?? dllDir,
                "UserData", "WildernessRenewableMod", "localization.json");

            if (File.Exists(userPath))
            {
                var fromFile = TryLoadJson(File.ReadAllText(userPath, Encoding.UTF8));
                if (fromFile != null)
                {
                    MelonLogger.Msg($"[WRM] Localization override loaded: {userPath}");
                    return fromFile;
                }
            }

            // 2. Embedded resource inside the DLL
            var asm    = Assembly.GetExecutingAssembly();
            var stream = asm.GetManifestResourceStream(EmbeddedResource);
            if (stream != null)
            {
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var fromEmbedded = TryLoadJson(reader.ReadToEnd());
                if (fromEmbedded != null) return fromEmbedded;
            }
            else
            {
                MelonLogger.Warning($"[WRM] Embedded resource '{EmbeddedResource}' not found — using English fallback.");
            }

            return Fallback;
        }

        private static Dictionary<string, Dictionary<string, string>> TryLoadJson(string json)
        {
            try   { return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json); }
            catch (Exception ex) { MelonLogger.Warning($"[WRM] Localization JSON parse error: {ex.Message}"); return null; }
        }

        // Maps TLD's internal language strings to our JSON keys.
        // Raw values confirmed from MelonLoader logs (Localization.Language property).
        private static readonly Dictionary<string, string> LanguageAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            // Chinese — TLD sends "Simplified Chinese" / "Traditional Chinese" (with space)
            ["Simplified Chinese"]      = "ChineseSimplified",
            ["Traditional Chinese"]     = "ChineseTraditional",
            ["schinese"]                = "ChineseSimplified",
            ["tchinese"]                = "ChineseTraditional",
            ["ChineseSimplified"]       = "ChineseSimplified",
            ["ChineseTraditional"]      = "ChineseTraditional",

            // French — TLD sends "French (France)"
            ["French (France)"]         = "French",

            // Spanish — TLD sends "Spanish (Spain)"
            ["Spanish (Spain)"]         = "Spanish",
            ["Spanish (Latin America)"] = "Spanish",

            // Portuguese — TLD sends "Portuguese (Brazil)"
            ["Portuguese (Brazil)"]     = "Brazilian",
            ["Portuguese (Portugal)"]   = "Brazilian",  // closest we have
            ["pt-BR"]                   = "Brazilian",
            ["pt"]                      = "Brazilian",
            ["PortugueseBrazil"]        = "Brazilian",

            // Ukrainian — matches directly but keep alias for safety
            ["uk"]                      = "Ukrainian",
            ["Ukranian"]                = "Ukrainian",  // common typo in some engines
        };

        private static string _lastLoggedLang = null;

        internal static string Get(string key)
        {
            string raw  = Localization.Language ?? "English";
            string lang = LanguageAliases.TryGetValue(raw, out string mapped) ? mapped : raw;

            // Log the language once so we can verify the key TLD sends
            if (lang != _lastLoggedLang)
            {
                MelonLogger.Msg($"[WRM] Language: '{raw}'" + (mapped != null ? $" → '{mapped}'" : ""));
                _lastLoggedLang = lang;
            }

            var data = Data;
            if (data.TryGetValue(lang,      out var dict) && dict.TryGetValue(key, out string val))   return val;
            if (data.TryGetValue("English", out var en)   && en.TryGetValue(key,   out string enVal)) return enVal;
            return key;
        }

        private static readonly Dictionary<string, Dictionary<string, string>> Fallback = new()
        {
            ["English"] = new()
            {
                ["WR.SECTION_COAL"]           = "Coal",
                ["WR.ENABLE_COAL"]            = "Enable coal respawn",
                ["WR.COAL_RESPAWN_TIME"]      = "Respawn time",
                ["WR.MIN_COAL"]               = "Min coal pieces",
                ["WR.MAX_COAL"]               = "Max coal pieces",
                ["WR.COAL_SCAN_RADIUS"]       = "Scan radius (m)",
                ["WR.COAL_LOCATION"]          = "Respawn location",
                ["WR.SECTION_STICKS"]         = "Large Sticks",
                ["WR.ENABLE_STICKS"]          = "Enable stick respawn",
                ["WR.STICKS_RESPAWN_TIME"]    = "Respawn time",
                ["WR.MIN_STICKS"]             = "Min sticks",
                ["WR.MAX_STICKS"]             = "Max sticks",
                ["WR.SECTION_SMALL_STICKS"]   = "Small Sticks",
                ["WR.ENABLE_SMALL_STICKS"]    = "Enable small stick respawn",
                ["WR.SMALL_STICKS_RESPAWN_TIME"] = "Respawn time",
                ["WR.MIN_SMALL_STICKS"]       = "Min small sticks",
                ["WR.MAX_SMALL_STICKS"]       = "Max small sticks",
                ["WR.SECTION_FEATHERS"]       = "Raven Feathers",
                ["WR.ENABLE_FEATHERS"]        = "Enable feather respawn",
                ["WR.FEATHERS_RESPAWN_TIME"]  = "Respawn time",
                ["WR.MIN_FEATHERS"]           = "Min feathers",
                ["WR.MAX_FEATHERS"]           = "Max feathers",
                ["WR.DESC_RESPAWN_TIME"]      = "How long before emptied resource spots refill.\n  Daily: 1d  Fast: 5d  Normal: 15d  Slow: 30d  Realistic: 60d",
                ["WR.CHOICE_DAILY"]           = "Daily (1 day)",
                ["WR.CHOICE_FAST"]            = "Fast (5 days)",
                ["WR.CHOICE_NORMAL"]          = "Normal (15 days)",
                ["WR.CHOICE_SLOW"]            = "Slow (30 days)",
                ["WR.CHOICE_REALISTIC"]       = "Realistic (60 days)",
                ["WR.LOC_CAVES"]              = "Caves only",
                ["WR.LOC_MINES"]              = "Mines only",
                ["WR.LOC_BOTH"]               = "Caves & Mines",
                ["WR.LOC_EVERYWHERE"]         = "Everywhere",
            }
        };
    }


    [HarmonyPatch(typeof(Localization), nameof(Localization.Get))]
    internal static class Patch_LocalizationGet
    {
        static void Postfix(string __0, ref string __result)
        {
            if (__0 == null || !__0.StartsWith("WR.")) return;
            __result = LocalizationManager.Get(__0);
        }
    }

    [HarmonyPatch]
    internal static class Patch_DescriptionText
    {
        static System.Reflection.MethodBase TargetMethod() =>
            AccessTools.PropertyGetter(
                AccessTools.TypeByName("ModSettings.DescriptionHolder"), "Text");

        static void Postfix(ref string __result)
        {
            if (__result == null || !__result.StartsWith("WR.")) return;
            __result = LocalizationManager.Get(__result);
        }
    }
}
