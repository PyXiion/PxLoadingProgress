namespace ru.pyxiion.modrim.LoadingProgress;

[HarmonyPatch(typeof(ModContentPack))]
internal static class ModContentPack_LoadingDataTracker_Patches
{
    [HarmonyPatch(nameof(ModContentPack.ReloadContentInt))]
    [HarmonyPrefix]
    private static void ReloadContentIntPrefix(ModContentPack __instance, bool hotReload)
    {
        if (hotReload)
        {
            return;
        }

        LoadingDataTracker.SwitchTo(__instance.Name);
    }

    [HarmonyPatch(nameof(ModContentPack.LoadPatches))]
    [HarmonyPrefix]
    private static void LoadPatchesPrefix(ModContentPack __instance)
    {
        if (LoadingProgressWindow.CurrentStage != LoadingStage.ErrorCheckPatches)
        {
            return;
        }

        LoadingDataTracker.SwitchTo(__instance.Name);
    }
}
