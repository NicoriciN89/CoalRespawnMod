using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace CoalRespawnMod
{
    internal static class LocalizationManager
    {
        private static Dictionary<string, Dictionary<string, string>> _data;

        private static Dictionary<string, Dictionary<string, string>> Data =>
            _data ?? (_data = Load());

        internal static void Reload() => _data = null;

        private const string EmbeddedResource = "CoalRespawnMod.localization.json";

        private static Dictionary<string, Dictionary<string, string>> Load()
        {
            // 1. Пользовательский оверрайд: UserData/CoalRespawnMod/localization.json
            string dllDir   = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            string userPath = Path.Combine(
                Path.GetDirectoryName(dllDir) ?? dllDir,
                "UserData", "CoalRespawnMod", "localization.json");

            if (File.Exists(userPath))
            {
                var fromFile = TryLoadJson(File.ReadAllText(userPath, Encoding.UTF8));
                if (fromFile != null)
                {
                    MelonLogger.Msg($"[CoalRespawnMod] Localization override loaded: {userPath}");
                    return fromFile;
                }
            }

            // 2. Встроенный ресурс внутри DLL
            var asm    = Assembly.GetExecutingAssembly();
            var stream = asm.GetManifestResourceStream(EmbeddedResource);
            if (stream != null)
            {
                using var reader  = new StreamReader(stream, Encoding.UTF8);
                var fromEmbedded  = TryLoadJson(reader.ReadToEnd());
                if (fromEmbedded != null) return fromEmbedded;
            }
            else
            {
                MelonLogger.Warning($"[CoalRespawnMod] Embedded resource '{EmbeddedResource}' not found — using English fallback.");
            }

            return Fallback;
        }

        private static Dictionary<string, Dictionary<string, string>> TryLoadJson(string json)
        {
            try   { return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json); }
            catch (Exception ex) { MelonLogger.Warning($"[CoalRespawnMod] Localization JSON parse error: {ex.Message}"); return null; }
        }


        internal static string Get(string key)
        {
            string lang = Localization.Language ?? "English";
            var data = Data;
            if (data.TryGetValue(lang,      out var dict) && dict.TryGetValue(key, out string val))  return val;
            if (data.TryGetValue("English", out var en)   && en.TryGetValue(key,   out string enVal)) return enVal;
            return key;
        }

        private static readonly Dictionary<string, Dictionary<string, string>> Fallback = new()
        {
            ["English"] = new()
            {
                ["CR.SECTION"]           = "Cave Coal Respawn",
                ["CR.RESPAWN_TIME"]      = "Respawn time",
                ["CR.DESC_RESPAWN_TIME"] = "How long before an emptied coal deposit refills.\n  Fast: 5d  Normal: 15d  Slow: 30d  Realistic: 60d",
                ["CR.MIN_COAL"]          = "Min coal pieces",
                ["CR.DESC_MIN_COAL"]     = "Minimum GEAR_Coal pieces spawned per deposit. Default: 2",
                ["CR.MAX_COAL"]          = "Max coal pieces",
                ["CR.DESC_MAX_COAL"]     = "Maximum GEAR_Coal pieces spawned per deposit. Default: 4",
                ["CR.SCAN_RADIUS"]       = "Scan radius (m)",
                ["CR.DESC_SCAN_RADIUS"]  = "Radius in metres to detect coal around each deposit. Default: 4",
            }
        };
    }


    [HarmonyPatch(typeof(Localization), nameof(Localization.Get))]
    internal static class Patch_LocalizationGet
    {
        static void Postfix(string __0, ref string __result)
        {
            if (__0 == null || !__0.StartsWith("CR.")) return;
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
            if (__result == null || !__result.StartsWith("CR.")) return;
            __result = LocalizationManager.Get(__result);
        }
    }
}
