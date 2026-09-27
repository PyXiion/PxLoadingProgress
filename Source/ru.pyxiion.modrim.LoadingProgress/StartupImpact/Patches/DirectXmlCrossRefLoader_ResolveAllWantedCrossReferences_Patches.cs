namespace ru.pyxiion.modrim.LoadingProgress.StartupImpact.Patches;

[HarmonyPatch(
    typeof(DirectXmlCrossRefLoader),
    nameof(DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences)
)]
[HarmonyPatchCategory("StartupImpact")]
internal static class DirectXmlCrossRefLoader_ResolveAllWantedCrossReferences_Patches
{
    private static string? ProfilerKey(FailMode failReportMode)
    {
        if (LoadingProgressWindow.CurrentStage == LoadingStage.Finished)
        {
            return null;
        }

        switch (failReportMode)
        {
            case FailMode.Silent:
                return "LoadingProgress.StartupImpact.ResolveAllWantedCrossReferences.NonImplied";
            case FailMode.LogErrors:
                return "LoadingProgress.StartupImpact.ResolveAllWantedCrossReferences.Implied";
            default:
                LoadingProgressMod.Warning(
                    $"Unknown fail report mode used with DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences: {failReportMode}"
                );
                return null;
        }
    }

    internal static void Prefix(FailMode failReportMode)
    {
        if (ProfilerKey(failReportMode) is { } key)
        {
            StartupImpactProfilerUtil.StartBaseGameProfiler(key);
        }
    }

    internal static void Postfix(FailMode failReportMode)
    {
        if (ProfilerKey(failReportMode) is { } key)
        {
            StartupImpactProfilerUtil.StopBaseGameProfiler(key);
        }
    }
}
