using System.Reflection.Emit;

namespace ru.pyxiion.modrim.LoadingProgress;

internal sealed class StaticConstructorOnStartupUtilityReplacement
{
    internal static void Interject() =>
        Utilities.LongEventHandlerPrependQueue(() =>
        {
            LongEventHandler.QueueLongEvent(CallAllAndRest(), "LoadingProgress.CallAll");
            // When we're done, resume the original ExecuteToExecuteWhenFinished method to
            // process whatever toExecuteWhenFinished entries are left.
            LongEventHandler.QueueLongEvent(
                LongEventHandler_ExecuteToExecuteWhenFinished_Patches.ExecuteToExecuteWhenFinished(),
                "LoadingProgress.ExecuteToExecuteWhenFinished"
            );
        });

    internal static bool _callAllCalled;

    // Vanilla's PlayDataLoader.DoPlayLoad() bundles StaticConstructorOnStartupUtility.CallAll(),
    // FloatMenuMakerMap.Init(), GlobalTextureAtlasManager.BakeStaticAtlases(), cache clearing,
    // a forced GC.Collect() and Resources.UnloadUnusedAssets() into a single ExecuteWhenFinished
    // delegate. We run each of those steps here ourselves, yielding between them so the loading
    // screen can repaint, and the original closure is removed from toExecuteWhenFinished (see
    // LongEventHandler_ExecuteToExecuteWhenFinished_Patches) so it doesn't run a second time.
    private static IEnumerable CallAllAndRest()
    {
        _callAllCalled = true;
        DeepProfiler.Start("StaticConstructorOnStartupUtilityReplacement.CallAll()");
        var list = GenTypes.AllTypesWithAttribute<StaticConstructorOnStartup>();
        for (var i = 0; i < list.Count; i++)
        {
            var item = list[i];

            LoadingProgressWindow.SetCurrentLoadingActivityRaw(item.ToString());
            LoadingProgressWindow.StageProgress = (i + 1, list.Count);
            yield return null;

            var info = StartupImpact.Profiler.Enabled
                ? LoadingProgressMod.instance.StartupImpact.Modlist.GetModInfoFor(
                    Utilities.FindModByAssembly(item.Assembly)
                )
                : null;
            info?.Start("LoadingProgress.StartupImpact.StaticConstructorOnStartupUtilityCallAll");

            try
            {
                //LoadingProgressMod.Debug($"About to run static constructor for {item} @ {now:HH:mm:ss.fff}");
                RuntimeHelpers.RunClassConstructor(item.TypeHandle);
                //LoadingProgressMod.Debug($"Finished running static constructor for {item} @ {DateTime.Now:HH:mm:ss.fff}; took {DateTime.Now - now:mm\\:ss\\.fff}");
            }
            catch (Exception ex)
            {
                Log.Error("Error in static constructor of " + item?.ToString() + ": " + ex);
            }

            _ = info?.Stop(
                "LoadingProgress.StartupImpact.StaticConstructorOnStartupUtilityCallAll"
            );
        }
        DeepProfiler.End();
        StaticConstructorOnStartupUtility.coreStaticAssetsLoaded = true;

        // Run the real StaticConstructorOnStartupUtility.CallAll() too, purely so third-party
        // Harmony patches on it still fire. The constructors themselves are no-ops the second
        // time (RunClassConstructor does nothing for an already-initialized type).
        LoadingProgressWindow.SetCurrentLoadingActivityRaw(string.Empty);
        yield return null;
        DeepProfiler.Start("Static constructor calls");
        try
        {
            StaticConstructorOnStartupUtility.CallAll();
            if (Prefs.DevMode)
            {
                StaticConstructorOnStartupUtility.ReportProbablyMissingAttributes();
            }
        }
        finally
        {
            DeepProfiler.End();
        }
        yield return null;

        FloatMenuMakerMap.Init();
        yield return null;

        DeepProfiler.Start("Atlas baking.");
        try
        {
            GlobalTextureAtlasManager.BakeStaticAtlases();
        }
        finally
        {
            DeepProfiler.End();
        }
        yield return null;

        DeepProfiler.Start("Garbage Collection");
        try
        {
            RimWorld.IO.AbstractFilesystem.ClearAllCache();
            GC.Collect(int.MaxValue, GCCollectionMode.Forced);
            _ = Resources.UnloadUnusedAssets();
        }
        finally
        {
            DeepProfiler.End();
        }
        yield return null;
    }
}

internal static partial class LongEventHandler_ExecuteToExecuteWhenFinished_Patches
{
    private static class StaticConstructorOnStartupCallAllFinder
    {
        private static readonly CodeMatch[] toMatch =
        [
            new(
                OpCodes.Call,
                AccessTools.Method(
                    typeof(StaticConstructorOnStartupUtility),
                    nameof(StaticConstructorOnStartupUtility.CallAll)
                )
            ),
        ];

        public static IEnumerable<MethodInfo> FindMethodCalling() =>
            Utilities.FindMethodsDoing(typeof(PlayDataLoader), toMatch);
    }
}
