namespace ru.pyxiion.modrim.LoadingProgress.StartupImpact.Dialog;

internal sealed record StartupImpactSessionModData(
    string ModName,
    string ModPackageId,
    IReadOnlyDictionary<string, float> Metrics,
    float TotalImpact,
    IReadOnlyDictionary<string, float> OffThreadMetrics,
    float OffThreadTotalImpact
)
{
    internal static StartupImpactSessionModData FromModInfo(ModInfo info) => new(
        info.Mod.Name ?? string.Empty,
        info.Mod.PackageIdPlayerFacing ?? string.Empty,
        new Dictionary<string, float>(info.Profiler.Metrics),
        info.Profiler.TotalImpact,
        new Dictionary<string, float>(info.Profiler.OffThreadMetrics),
        info.Profiler.OffThreadTotalImpact
    );
}
