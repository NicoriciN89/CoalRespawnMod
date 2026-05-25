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

[assembly: MelonInfo(typeof(CoalRespawnMod.Core), "CoalRespawnMod", "1.0.0", "NnicolaeN")]
[assembly: MelonGame("Hinterland", "TheLongDark")]
[assembly: MelonColor(255, 64, 64, 64)]

namespace CoalRespawnMod
{
    // emptyAt   : ключ спаунера → игровое время (ч) когда он опустел
    // respawnAt : ключ спаунера → игровое время (ч) когда мы его пересоздали
    //             (нужно чтобы переспаунить уголь заново после загрузки сейва,
    //              пока игрок его не подберёт)
    public class CoalSaveData
    {
        public Dictionary<string, Dictionary<string, float>> emptyAt   { get; set; } = new();
        public Dictionary<string, Dictionary<string, float>> respawnAt { get; set; } = new();
    }

    public class Core : MelonMod
    {
        internal static MelonLogger.Instance Log;

        // scene → (spawnerKey → час опустения)
        internal static Dictionary<string, Dictionary<string, float>> emptyAt   = new();
        // scene → (spawnerKey → час последнего спауна нашим модом)
        internal static Dictionary<string, Dictionary<string, float>> respawnAt = new();

        internal static ModDataManager dataManager = new ModDataManager("CoalRespawnMod", false);
        internal static object         coroutineHandle;

        private const string SaveTag           = "coalRespawn";
        private const float  CoroutineInterval = 60f; // реальных секунд между проверками
        private const float  InitialDelay      = 6f;  // задержка после загрузки сцены

        // Дни для каждого пресета (индекс = respawnPreset)
        private static readonly int[] RespawnPresetDays = { 5, 15, 30, 60 };

        public static float GetRespawnHours()
        {
            int preset = Math.Clamp(Settings.instance.respawnPreset, 0, RespawnPresetDays.Length - 1);
            return RespawnPresetDays[preset] * 24f;
        }

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            Settings.OnLoad();
            Log.Msg("CoalRespawnMod v1.0.0 loaded");
        }

        public static bool IsPlayableScene(string scene) =>
            !string.IsNullOrEmpty(scene) &&
            !scene.Contains("MainMenu") &&
            scene != "Boot" &&
            scene != "Empty";

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
                Log?.Warning($"[CoalRespawnMod] Ошибка загрузки данных: {e.Message}");
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
            // Небольшая задержка: дать игре время полностью загрузить сцену и расставить объекты
            for (float t = 0f; t < InitialDelay; t += Time.deltaTime)
            {
                if (!IsPlayableScene(GameManager.m_ActiveScene)) yield break;
                yield return new WaitForEndOfFrame();
            }

            while (IsPlayableScene(GameManager.m_ActiveScene))
            {
                ProcessCoalSpawners();

                // Ждём следующий интервал
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

            float nowHours     = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();
            float respawnHours = GetRespawnHours();
            float radius       = Settings.instance.scanRadius;

            // Убедимся что словари для сцены существуют
            if (!emptyAt.ContainsKey(scene))   emptyAt[scene]   = new();
            if (!respawnAt.ContainsKey(scene))  respawnAt[scene] = new();

            // Найти все угольные спаунеры в сцене
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
                    // Уголь есть — снимаем метки если они были
                    emptyAt[scene].Remove(key);
                    respawnAt[scene].Remove(key);
                    continue;
                }

                // Был ли спаунер недавно пересоздан нашим модом?
                // (Уголь мог исчезнуть после загрузки сейва — перезаспауним)
                if (respawnAt[scene].ContainsKey(key))
                {
                    DoSpawnCoal(pos, radius);
                    Log?.Msg($"[CoalRespawnMod] Переспаун после загрузки: {spawner.name}");
                    continue;
                }

                // Спаунер только что опустел?
                if (!emptyAt[scene].ContainsKey(key))
                {
                    emptyAt[scene][key] = nowHours;
                    Log?.Msg($"[CoalRespawnMod] Спаунер опустел: {spawner.name} @ {pos}");
                    continue;
                }

                // Таймер истёк?
                float emptiedAt = emptyAt[scene][key];
                if (nowHours >= emptiedAt + respawnHours)
                {
                    DoSpawnCoal(pos, radius);
                    emptyAt[scene].Remove(key);
                    respawnAt[scene][key] = nowHours;
                    Log?.Msg($"[CoalRespawnMod] Уголь возродился: {spawner.name} @ {pos}");
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
            int count = UnityEngine.Random.Range(
                Settings.instance.minCoal,
                Settings.instance.maxCoal + 1);

            GearItem prefab = null;
            try { prefab = GearItem.LoadGearItemPrefab("GEAR_Coal"); }
            catch (Exception e) { Log?.Warning($"[CoalRespawnMod] Не удалось загрузить GEAR_Coal: {e.Message}"); }

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
