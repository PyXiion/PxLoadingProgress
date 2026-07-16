namespace ru.pyxiion.modrim.LoadingProgress;

[HarmonyPatch(typeof(DirectXmlToObjectNew))]
internal static class DirectXmlToObjectNew_LoadingDataTracker_Patches
{
    [HarmonyPatch(nameof(DirectXmlToObjectNew.DefFromNodeNew))]
    [HarmonyPostfix]
    private static void DefFromNodeNewPostfix(Def __result)
    {
        if (LoadingProgressWindow.CurrentStage != LoadingStage.LoadingDefs)
        {
            return;
        }

        LoadingDataTracker.LastDef = __result;
    }
}
