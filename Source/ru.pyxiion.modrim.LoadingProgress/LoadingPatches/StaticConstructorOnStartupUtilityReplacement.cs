using System.Reflection.Emit;

namespace ru.pyxiion.modrim.LoadingProgress;

internal sealed class StaticConstructorOnStartupUtilityReplacement
{
    internal static void Interject() =>
        Utilities.LongEventHandlerPrependQueue(() =>
        {
            LongEventHandler.QueueLongEvent(CallAll(), "LoadingProgress.CallAll");
            // When we're done, resume the original ExecuteToExecuteWhenFinished method.
            LongEventHandler.QueueLongEvent(
                LongEventHandler_ExecuteToExecuteWhenFinished_Patches.ExecuteToExecuteWhenFinished(),
                "LoadingProgress.ExecuteToExecuteWhenFinished"
            );
        });

    internal static bool _callAllCalled;

    private static IEnumerable CallAll()
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
