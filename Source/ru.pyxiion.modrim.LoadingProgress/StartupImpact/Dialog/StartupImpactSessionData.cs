namespace ru.pyxiion.modrim.LoadingProgress.StartupImpact.Dialog;

internal sealed record StartupImpactSessionData(
    DateTimeOffset Timestamp,
    float LoadingTime,
    IReadOnlyDictionary<string, float> Metrics,
    float TotalImpact,
    IReadOnlyDictionary<string, float> OffThreadMetrics,
    float OffThreadTotalImpact,
    IReadOnlyList<StartupImpactSessionModData> Mods
)
{
    internal static StartupImpactSessionData FromCurrentSession() => new(
        DateTimeOffset.UtcNow,
        LoadingProgressMod.instance.StartupImpact.TotalLoadingTime,
        LoadingProgressMod.instance.StartupImpact.BaseGameProfiler.Metrics.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value
        ),
        0f,
        LoadingProgressMod.instance.StartupImpact.BaseGameProfiler.OffThreadMetrics.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value
        ),
        0f,
        [.. LoadingProgressMod.instance.StartupImpact.Modlist.ModsInImpactOrder.Select(StartupImpactSessionModData.FromModInfo)]
    );
}
