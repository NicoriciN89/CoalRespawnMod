using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using ModData;
using UnityEngine;

[assembly: MelonInfo(typeof(WildernessRenewableMod.Core), "WildernessRenewableMod", "2.0.0", "NnicolaeN")]
[assembly: MelonGame("Hinterland", "TheLongDark")]
[assembly: MelonColor(255, 34, 139, 34)]

namespace WildernessRenewableMod
{
    // Per-scene tracking data for one resource type
    public class ResourceData
    {
        // Positions where this resource was discovered (position key → true)
        // Coal doesn't use this (uses RadialObjectSpawner anchors instead)
        public List<string>              knownPoints { get; set; } = new();
        // key → game-hours when the spot was emptied
        public Dictionary<string, float> emptyAt     { get; set; } = new();
        // key → game-hours when mod last refilled (to re-place after save reload)
        public Dictionary<string, float> respawnAt   { get; set; } = new();
    }

    public class WildernessRenewableSaveData
    {
        public Dictionary<string, ResourceData> coal        { get; set; } = new();
        public Dictionary<string, ResourceData> sticks      { get; set; } = new();
        public Dictionary<string, ResourceData> smallSticks { get; set; } = new();
        public Dictionary<string, ResourceData> feathers    { get; set; } = new();
    }

    public class Core : MelonMod
    {
        internal static MelonLogger.Instance Log;

        // scene → ResourceData  (one dict per resource type)
        internal static Dictionary<string, ResourceData> coalData       = new();
        internal static Dictionary<string, ResourceData> stickData      = new();
        internal static Dictionary<string, ResourceData> smallStickData = new();
        internal static Dictionary<string, ResourceData> featherData    = new();

        internal static ModDataManager dataManager    = new ModDataManager("WildernessRenewableMod", false);
        internal static object         coroutineHandle;

        private const string SaveTag           = "wildernessRenewable";
        private const float  CoroutineInterval = 60f; // real seconds between checks
        private const float  InitialDelay      = 6f;  // wait after scene load

        // Preset index → in-game days
        private static readonly int[] RespawnPresetDays = { 1, 5, 15, 30, 60 };

        public static float GetRespawnHours(int preset)
        {
            preset = Math.Clamp(preset, 0, RespawnPresetDays.Length - 1);
            return RespawnPresetDays[preset] * 24f;
        }

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            Settings.OnLoad();
            Log.Msg("WildernessRenewableMod v2.0.0 loaded");
        }

        public static bool IsPlayableScene(string scene) =>
            !string.IsNullOrEmpty(scene) &&
            !scene.Contains("MainMenu") &&
            scene != "Boot" &&
            scene != "Empty";

        private static bool IsCoalScene(string scene)
        {
            if (string.IsNullOrEmpty(scene)) return false;
            string lower = scene.ToLowerInvariant();
            return Math.Clamp(Settings.instance.coalLocationPreset, 0, 3) switch
            {
                0 => lower.Contains("cave"),
                1 => lower.Contains("mine"),
                2 => lower.Contains("cave") || lower.Contains("mine"),
                3 => true,
                _ => lower.Contains("cave") || lower.Contains("mine")
            };
        }

        // Position key encodes coordinates with 1-decimal precision
        public static string PosKey(Vector3 pos) =>
            $"{pos.x.ToString("F1", CultureInfo.InvariantCulture)}|" +
            $"{pos.y.ToString("F1", CultureInfo.InvariantCulture)}|" +
            $"{pos.z.ToString("F1", CultureInfo.InvariantCulture)}";

        public static Vector3 KeyToPos(string key)
        {
            try
            {
                var p = key.Split('|');
                if (p.Length < 3) throw new FormatException("Expected 3 components");
                return new Vector3(
                    float.Parse(p[0], CultureInfo.InvariantCulture),
                    float.Parse(p[1], CultureInfo.InvariantCulture),
                    float.Parse(p[2], CultureInfo.InvariantCulture));
            }
            catch (Exception e)
            {
                Log?.Warning($"[WRM] Bad position key '{key}': {e.Message}");
                return Vector3.zero;
            }
        }

        // ── Save / Load ───────────────────────────────────────────────────────────

        public static void LoadData()
        {
            string json = dataManager.Load(SaveTag);
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var data = JsonSerializer.Deserialize<WildernessRenewableSaveData>(json);
                if (data != null)
                {
                    coalData       = data.coal        ?? new();
                    stickData      = data.sticks      ?? new();
                    smallStickData = data.smallSticks ?? new();
                    featherData    = data.feathers    ?? new();
                }
            }
            catch (Exception e)
            {
                Log?.Warning($"[WRM] Failed to load save data: {e.Message}");
                coalData = new(); stickData = new(); smallStickData = new(); featherData = new();
            }
        }

        public static void SaveData()
        {
            var data = new WildernessRenewableSaveData
            {
                coal        = coalData,
                sticks      = stickData,
                smallSticks = smallStickData,
                feathers    = featherData
            };
            dataManager.Save(JsonSerializer.Serialize(data), SaveTag);
        }

        // ── Coroutine ─────────────────────────────────────────────────────────────

        public static void StartCoroutine()
        {
            if (coroutineHandle != null) MelonCoroutines.Stop(coroutineHandle);
            coroutineHandle = MelonCoroutines.Start(RenewableLoop());
        }

        public static IEnumerator RenewableLoop()
        {
            yield return new WaitForSeconds(InitialDelay);
            if (!IsPlayableScene(GameManager.m_ActiveScene)) yield break;

            while (IsPlayableScene(GameManager.m_ActiveScene))
            {
                string scene    = GameManager.m_ActiveScene;
                float  nowHours = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();
                var    s        = Settings.instance;

                if (s.enableCoal && IsCoalScene(scene))
                    ProcessCoal(scene, nowHours, s);

                if (s.enableSticks)
                    ProcessPositionTracked(scene, nowHours, GetRespawnHours(s.sticksRespawnPreset),
                        "GEAR_Stick", stickData, s.minSticks, s.maxSticks, 2f);

                if (s.enableSmallSticks)
                    ProcessPositionTracked(scene, nowHours, GetRespawnHours(s.smallSticksRespawnPreset),
                        "GEAR_SmallStick", smallStickData, s.minSmallSticks, s.maxSmallSticks, 1.5f);

                if (s.enableFeathers)
                    ProcessPositionTracked(scene, nowHours, GetRespawnHours(s.feathersRespawnPreset),
                        "GEAR_RavenFeather", featherData, s.minFeathers, s.maxFeathers, 1.5f);

                yield return new WaitForSeconds(CoroutineInterval);
            }
        }

        // ── Coal — uses RadialObjectSpawner as fixed anchors ──────────────────────

        private static void ProcessCoal(string scene, float nowHours, WildernessRenewableSettings s)
        {
            if (!coalData.ContainsKey(scene)) coalData[scene] = new();
            var data       = coalData[scene];
            float respawnH = GetRespawnHours(s.coalRespawnPreset);
            float radius   = s.coalScanRadius;

            var spawners = UnityEngine.Object.FindObjectsOfType<RadialObjectSpawner>();
            foreach (var spawner in spawners)
            {
                if (spawner == null) continue;
                if (spawner.name.IndexOf("coal", StringComparison.OrdinalIgnoreCase) < 0) continue;

                string key = PosKey(spawner.transform.position);
                int    cnt = CountGearNear(spawner.transform.position, radius, "GEAR_Coal");
                ProcessEntry(data, key, spawner.transform.position, cnt, nowHours, respawnH,
                    radius, "GEAR_Coal", s.minCoal, s.maxCoal, $"coal/{spawner.name}");
            }
        }

        // ── Sticks / Feathers — discover positions from live items ────────────────

        private static void ProcessPositionTracked(
            string scene, float nowHours, float respawnH,
            string gearPrefix, Dictionary<string, ResourceData> allData,
            int min, int max, float scanRadius)
        {
            if (!allData.ContainsKey(scene)) allData[scene] = new();
            var data = allData[scene];

            // Add newly discovered positions from items currently in the scene.
            // Guard: skip positions that are already within scanRadius of a known point —
            // this prevents items spawned by the mod from creating duplicate origin points,
            // which would cause exponential growth of spawn spots over multiple cycles.
            var allItems = UnityEngine.Object.FindObjectsOfType<GearItem>();
            foreach (var gi in allItems)
            {
                if (gi == null) continue;
                if (gi.name.IndexOf(gearPrefix, StringComparison.OrdinalIgnoreCase) < 0) continue;
                string key    = PosKey(gi.transform.position);
                Vector3 giPos = gi.transform.position;
                if (!data.knownPoints.Contains(key) && !HasNearbyKnownPoint(data, giPos, scanRadius))
                    data.knownPoints.Add(key);
            }

            // Check each known spawn position
            // Snapshot to avoid modifying during iteration
            var snapshot = new List<string>(data.knownPoints);
            foreach (string key in snapshot)
            {
                Vector3 pos = KeyToPos(key);
                int     cnt = CountGearNear(pos, scanRadius, gearPrefix);
                ProcessEntry(data, key, pos, cnt, nowHours, respawnH,
                    scanRadius, gearPrefix, min, max, $"{gearPrefix}");
            }
        }

        private static bool HasNearbyKnownPoint(ResourceData data, Vector3 pos, float radius)
        {
            foreach (string existingKey in data.knownPoints)
            {
                Vector3 existingPos = KeyToPos(existingKey);
                if (existingPos == Vector3.zero) continue; // skip invalid keys
                if (Vector3.Distance(existingPos, pos) <= radius)
                    return true;
            }
            return false;
        }

        // ── Shared entry logic (coal + position-tracked) ──────────────────────────

        private static void ProcessEntry(
            ResourceData data, string key, Vector3 pos, int cnt,
            float nowHours, float respawnH,
            float radius, string gearPrefix, int min, int max, string logLabel)
        {
            if (cnt > 0)
            {
                // Items present — clear stale markers
                data.emptyAt.Remove(key);
                data.respawnAt.Remove(key);
                return;
            }

            // Mod previously refilled this spot; re-place after a save reload
            if (data.respawnAt.ContainsKey(key))
            {
                DoSpawn(pos, radius, gearPrefix, min, max);
                data.respawnAt.Remove(key);
                Log?.Msg($"[WRM] Re-spawn after reload: {logLabel}");
                return;
            }

            // Just became empty — record the time
            if (!data.emptyAt.ContainsKey(key))
            {
                data.emptyAt[key] = nowHours;
                Log?.Msg($"[WRM] Emptied: {logLabel} @ {pos}");
                return;
            }

            // Timer expired → respawn
            if (nowHours >= data.emptyAt[key] + respawnH)
            {
                DoSpawn(pos, radius, gearPrefix, min, max);
                data.emptyAt.Remove(key);
                data.respawnAt[key] = nowHours;
                Log?.Msg($"[WRM] Respawned: {logLabel} @ {pos}");
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static int CountGearNear(Vector3 pos, float radius, string prefix)
        {
            var hits = Physics.OverlapSphere(pos, radius);
            if (hits == null) return 0;
            int count = 0;
            foreach (var col in hits)
            {
                if (col == null) continue;
                var gi = col.GetComponent<GearItem>();
                if (gi != null && gi.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    count++;
            }
            return count;
        }

        private static void DoSpawn(Vector3 center, float radius, string gearName, int min, int max)
        {
            int count = UnityEngine.Random.Range(min, Math.Max(max, min) + 1);

            GearItem prefab = null;
            try { prefab = GearItem.LoadGearItemPrefab(gearName); }
            catch (Exception e) { Log?.Warning($"[WRM] Failed to load {gearName}: {e.Message}"); return; }

            if (prefab == null) { Log?.Warning($"[WRM] Prefab null: {gearName}"); return; }

            float spawnR = radius * 0.75f;
            for (int i = 0; i < count; i++)
            {
                Vector2 r2   = UnityEngine.Random.insideUnitCircle * spawnR;
                Vector3 sPos = center + new Vector3(r2.x, 0.15f, r2.y);
                GameObject.Instantiate(prefab.gameObject, sPos, UnityEngine.Random.rotation);
            }
        }
    }
}
