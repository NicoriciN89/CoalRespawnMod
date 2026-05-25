using HarmonyLib;
using Il2Cpp;
using MelonLoader;

namespace CoalRespawnMod
{
    // ── Загрузка сцены: восстановить данные и запустить корутину ─────────────
    [HarmonyPatch(typeof(SaveGameSystem), nameof(SaveGameSystem.LoadSceneData))]
    internal static class Patch_LoadSceneData
    {
        internal static void Postfix(string name)
        {
            Core.LoadData();
            Core.StartCoroutine();
        }
    }

    // ── Сохранение сцены: записать текущее состояние ─────────────────────────
    [HarmonyPatch(typeof(SaveGameSystem), nameof(SaveGameSystem.SaveSceneData))]
    internal static class Patch_SaveSceneData
    {
        internal static void Postfix(SlotData slot)
        {
            Core.SaveData();
        }
    }

    // ── Выход в главное меню: остановить корутину и сбросить состояние ────────
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.DoExitToMainMenu))]
    internal static class Patch_ExitToMenu
    {
        internal static void Postfix()
        {
            if (Core.coroutineHandle != null)
            {
                MelonCoroutines.Stop(Core.coroutineHandle);
                Core.coroutineHandle = null;
            }
        }
    }
}
