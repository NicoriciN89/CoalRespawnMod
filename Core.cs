using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using ModData;
using UnityEngine;

[assembly: MelonInfo(typeof(CoalRespawnMod.Core), "CoalRespawnMod", "1.1.1", "NnicolaeN")]
[assembly: MelonGame("Hinterland", "TheLongDark")]
[assembly: MelonColor(255, 64, 64, 64)]

namespace CoalRespawnMod
{
    // emptyAt   : spawnerKey → game-time (hrs) when the deposit was emptied
    // respawnAt : spawnerKey → game-time (hrs) when the mod last refilled it
    //             (needed to re-place coal after a save reload, before the player picks it up)
    public class CoalSaveData
    {
        public Dictionary<string, Dictionary<string, float>> emptyAt   { get; set; } = new();
        public Dictionary<string, Dictionary<string, float>> respawnAt { get; set; } = new();
    }

    public class Core : MelonMod
    {
        internal static MelonLogger.Instance Log;

        // scene → (spawnerKey → hour emptied)
        internal static Dictionary<string, Dictionary<string, float>> emptyAt   = new();
        // scene → (spawnerKey → hour of last mod-triggered spawn)
        internal static Dictionary<string, Dictionary<string, float>> respawnAt = new();

        internal static ModDataManager dataManager = new ModDataManager("CoalRespawnMod", false);
        internal static object         coroutineHandle;

        private const string SaveTag           = "coalRespawn";
        private const float  CoroutineInterval = 60f; // real seconds between checks
        private const float  InitialDelay      = 6f;  // initial delay after scene load

        // days per preset index (matches respawnPreset)
        private static readonly int[] RespawnPresetDays = { 1, 5, 15, 30, 60 };

        public static float GetRespawnHours()
        {
            int preset = Math.Clamp(Settings.instance.respawnPreset, 0, RespawnPresetDays.Length - 1);
            return RespawnPresetDays[preset] * 24f;
        }

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            Settings.OnLoad();
            Log.Msg("CoalRespawnMod v1.1.1 loaded");
        }

        public static bool IsPlayableScene(string scene) =>
            !string.IsNullOrEmpty(scene) &&
            !scene.Contains("MainMenu") &&
            scene != "Boot" &&
            scene != "Empty";

        private static bool IsAllowedScene(string scene)
        {
            if (string.IsNullOrEmpty(scene)) return false;
            string lower = scene.ToLowerInvariant();
            int loc = Math.Clamp(Settings.instance.locationPreset, 0, 3);
            return loc switch
            {
                0 => lower.Contains("cave"),
                1 => lower.Contains("mine"),
                2 => lower.Contains("cave") || lower.Contains("mine"),
                3 => true,
                _ => lower.Contains("cave") || lower.Contains("mine")
            };
        }

        public static string SpawnerKey(string scene, Vector3 pos) =>
            $"{scene}|{pos.x:F1}|{pos.y:F1}|{pos.z:F1}";

        public static void LoadData()
        {
            string json = dataManager.Load(SaveTag);
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var data = JsonSerializer.Deserialize<CoalSaveData>(json);
                if (data != null)
                {
                    emptyAt   = data.emptyAt   ?? new();
                    respawnAt = data.respawnAt  ?? new();
                }
            }
            catch (Exception e)
            {
                Log?.Warning($"[CoalRespawnMod] Failed to load save data: {e.Message}");
                emptyAt   = new();
                respawnAt = new();
            }
        }

        public static void SaveData()
        {
            var data = new CoalSaveData { emptyAt = emptyAt, respawnAt = respawnAt };
            dataManager.Save(JsonSerializer.Serialize(data), SaveTag);
        }

        public static void StartCoroutine()
        {
            if (coroutineHandle != null) MelonCoroutines.Stop(coroutineHandle);
            coroutineHandle = MelonCoroutines.Start(CoalRespawnLoop());
        }

        public static IEnumerator CoalRespawnLoop()
        {
            // Brief delay to let the scene fully load and place all objects
            for (float t = 0f; t < InitialDelay; t += Time.deltaTime)
            {
                if (!IsPlayableScene(GameManager.m_ActiveScene)) yield break;
                yield return new WaitForEndOfFrame();
            }

            while (IsPlayableScene(GameManager.m_ActiveScene))
            {
                ProcessCoalSpawners();

                // Wait for the next interval
                for (float t = 0f; t < CoroutineInterval; t += Time.deltaTime)
                {
                    if (!IsPlayableScene(GameManager.m_ActiveScene)) yield break;
                    yield return new WaitForEndOfFrame();
                }
            }
        }

        private static void ProcessCoalSpawners()
        {
            string scene = GameManager.m_ActiveScene;
            if (!IsPlayableScene(scene)) return;
            if (!IsAllowedScene(scene)) return;

            float nowHours     = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();
            float respawnHours = GetRespawnHours();
            float radius       = Settings.instance.scanRadius;

            // Ensure per-scene dictionaries exist
            if (!emptyAt.ContainsKey(scene))   emptyAt[scene]   = new();
            if (!respawnAt.ContainsKey(scene))  respawnAt[scene] = new();

            // Find all coal spawners in the scene
            var spawners = UnityEngine.Object.FindObjectsOfType<RadialObjectSpawner>();
            foreach (var spawner in spawners)
            {
                if (spawner == null) continue;
                if (!spawner.name.Contains("coal") && !spawner.name.Contains("Coal")) continue;

                Vector3 pos = spawner.transform.position;
                string  key = SpawnerKey(scene, pos);
                int     cnt = CountCoalNear(pos, radius);

                if (cnt > 0)
                {
                    // Coal present — clear any stale markers
                    emptyAt[scene].Remove(key);
                    respawnAt[scene].Remove(key);
                    continue;
                }

                // Was this spawner recently refilled by the mod?
                // (coal may have de-spawned on save reload — re-place it)
                if (respawnAt[scene].ContainsKey(key))
                {
                    DoSpawnCoal(pos, radius);
                    respawnAt[scene].Remove(key); // prevent infinite re-spawn if player picks it up after reload
                    Log?.Msg($"[CoalRespawnMod] Re-spawn after reload: {spawner.name}");
                    continue;
                }

                // Spawner just became empty?
                if (!emptyAt[scene].ContainsKey(key))
                {
                    emptyAt[scene][key] = nowHours;
                    Log?.Msg($"[CoalRespawnMod] Deposit emptied: {spawner.name} @ {pos}");
                    continue;
                }

                // Timer expired?
                float emptiedAt = emptyAt[scene][key];
                if (nowHours >= emptiedAt + respawnHours)
                {
                    DoSpawnCoal(pos, radius);
                    emptyAt[scene].Remove(key);
                    respawnAt[scene][key] = nowHours;
                    Log?.Msg($"[CoalRespawnMod] Coal respawned: {spawner.name} @ {pos}");
                }
            }
        }

        private static int CountCoalNear(Vector3 pos, float radius)
        {
            int count = 0;
            var hits = Physics.OverlapSphere(pos, radius);
            if (hits == null) return 0;
            foreach (var col in hits)
            {
                if (col == null) continue;
                var gi = col.GetComponent<GearItem>();
                if (gi != null && gi.name.StartsWith("GEAR_Coal"))
                    count++;
            }
            return count;
        }

        private static void DoSpawnCoal(Vector3 center, float radius)
        {
            int min   = Settings.instance.minCoal;
            int max   = Math.Max(Settings.instance.maxCoal, min);
            int count = UnityEngine.Random.Range(min, max + 1);

            GearItem prefab = null;
            try { prefab = GearItem.LoadGearItemPrefab("GEAR_Coal"); }
            catch (Exception e) { Log?.Warning($"[CoalRespawnMod] Failed to load GEAR_Coal: {e.Message}"); }

            if (prefab == null) { Log?.Warning("[CoalRespawnMod] GEAR_Coal prefab == null"); return; }

            float spawnRadius = radius * 0.75f;
            for (int i = 0; i < count; i++)
            {
                Vector2 r2   = UnityEngine.Random.insideUnitCircle * spawnRadius;
                Vector3 sPos = center + new Vector3(r2.x, 0.15f, r2.y);
                Quaternion rot = UnityEngine.Random.rotation;
                GameObject.Instantiate(prefab.gameObject, sPos, rot);
            }
        }
    }
}
