namespace ilyvion.LoadingProgress;

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

        LoadingDataTracker.Previous = LoadingDataTracker.Current;
        LoadingDataTracker.Current = __instance.Name;
    }

    [HarmonyPatch(nameof(ModContentPack.LoadPatches))]
    [HarmonyPrefix]
    private static void LoadPatchesPrefix(ModContentPack __instance)
    {
        if (LoadingProgressWindow.CurrentStage != LoadingStage.ErrorCheckPatches)
        {
            return;
        }

        LoadingDataTracker.Previous = LoadingDataTracker.Current;
        LoadingDataTracker.Current = __instance.Name;
    }
}
