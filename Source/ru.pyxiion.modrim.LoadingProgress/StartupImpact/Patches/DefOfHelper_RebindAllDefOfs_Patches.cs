namespace ru.pyxiion.modrim.LoadingProgress.StartupImpact.Patches;

[HarmonyPatch(typeof(DefOfHelper), nameof(DefOfHelper.RebindAllDefOfs))]
[HarmonyPatchCategory("StartupImpact")]
internal static class DefOfHelper_RebindAllDefOfs_Patches
{
    private static string ProfilerKey(bool earlyTryMode) =>
        earlyTryMode
            ? "LoadingProgress.StartupImpact.DefOfHelperRebindAllDefOfs.Early"
            : "LoadingProgress.StartupImpact.DefOfHelperRebindAllDefOfs.Final";

    internal static void Prefix(bool earlyTryMode) =>
        StartupImpactProfilerUtil.StartBaseGameProfiler(ProfilerKey(earlyTryMode));

    internal static void Postfix(bool earlyTryMode) =>
        StartupImpactProfilerUtil.StopBaseGameProfiler(ProfilerKey(earlyTryMode));
}
