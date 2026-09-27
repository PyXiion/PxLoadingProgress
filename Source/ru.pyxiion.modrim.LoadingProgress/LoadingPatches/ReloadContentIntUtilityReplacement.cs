using System.Reflection.Emit;

namespace ru.pyxiion.modrim.LoadingProgress;

internal sealed class ReloadContentIntReplacement
{
    private sealed record Step(
        string Label,
        string DeepProfilerLabel,
        string ProfilerCategory,
        Action<ModContentPack> Reload
    );

    // Mirrors ModContentPack.ReloadContentInt, split up so progress can be shown between steps.
    private static readonly Step[] Steps =
    [
        new(
            "audio clips",
            "Reload audio clips",
            "LoadingProgress.StartupImpact.ModContentPackReloadContentInt.AudioClips",
            mod => mod.audioClips.ReloadAll(false)
        ),
        new(
            "textures",
            "Reload textures",
            "LoadingProgress.StartupImpact.ModContentPackReloadContentInt.Textures",
            mod => mod.textures.ReloadAll(false)
        ),
        new(
            "strings",
            "Reload strings",
            "LoadingProgress.StartupImpact.ModContentPackReloadContentInt.Strings",
            mod => mod.strings.ReloadAll(false)
        ),
        new(
            "asset bundles",
            "Reload asset bundles",
            "LoadingProgress.StartupImpact.ModContentPackReloadContentInt.AssetBundles",
            mod =>
            {
                mod.assetBundles.ReloadAll(false);
                mod.allAssetNamesInBundleCached = null;
                mod.allAssetNamesInBundleCachedTrie = null;
            }
        ),
    ];

    public static int StepCount => Steps.Length;

    /// <summary>
    /// Yields each step's label right before running that step.
    /// </summary>
    public static IEnumerable<string> ReloadContentInt(ModContentPack modContentPack)
    {
        var info = StartupImpact.Profiler.Enabled
            ? LoadingProgressMod.instance.StartupImpact.Modlist.GetModInfoFor(modContentPack)
            : null;

        foreach (var step in Steps)
        {
            yield return step.Label;
            info?.Start(step.ProfilerCategory);
            DeepProfiler.Start(step.DeepProfilerLabel);
            try
            {
                step.Reload(modContentPack);
            }
            finally
            {
                DeepProfiler.End();
            }
            _ = info?.Stop(step.ProfilerCategory);
        }
    }
}

internal static partial class LongEventHandler_ExecuteToExecuteWhenFinished_Patches
{
    private static class ReloadContentIntFinder
    {
        private static readonly CodeMatch[] toMatch =
        [
            new(
                OpCodes.Call,
                AccessTools.Method(typeof(ModContentPack), nameof(ModContentPack.ReloadContentInt))
            ),
        ];

        /// <summary>
        /// Finds the closure method that calls ReloadContentInt, and the closure's field holding
        /// the ModContentPack it's called on.
        /// </summary>
        public static IEnumerable<(MethodInfo method, FieldInfo thisField)> FindMethodCalling() =>
            Utilities
                .FindMethodsDoing(typeof(ModContentPack), toMatch)
                .Select(method =>
                    (
                        method,
                        AccessTools
                            .GetDeclaredFields(method.DeclaringType)
                            .Single(f => f.Name.Contains("this", StringComparison.Ordinal))
                    )
                );
    }
}
