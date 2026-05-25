using HarmonyLib;
using Il2Cpp;
using MelonLoader;

namespace CoalRespawnMod
{

    [HarmonyPatch(typeof(SaveGameSystem), nameof(SaveGameSystem.LoadSceneData))]
    internal static class Patch_LoadSceneData
    {
        internal static void Postfix(string name)
        {
            Core.LoadData();
            Core.StartCoroutine();
        }
    }


    [HarmonyPatch(typeof(SaveGameSystem), nameof(SaveGameSystem.SaveSceneData))]
    internal static class Patch_SaveSceneData
    {
        internal static void Postfix(SlotData slot)
        {
            Core.SaveData();
        }
    }


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
