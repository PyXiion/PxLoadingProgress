using ru.pyxiion.modrim.LoadingProgress.FasterGameLoading;
using ru.pyxiion.modrim.LoadingProgress.StartupImpact;

namespace ru.pyxiion.modrim.LoadingProgress;

[HarmonyPatch(typeof(LongEventHandler), nameof(LongEventHandler.ExecuteToExecuteWhenFinished))]
internal static partial class LongEventHandler_ExecuteToExecuteWhenFinished_Patches
{
    private static bool _hasWarnedAboutReloadIntPatches;

    private static bool Prepare()
    {
        if (!LoadingProgressMod.Settings.PatchInitialization)
        {
            LoadingProgressMod.Message(
                "Patching of initialization code is disabled " + "in the settings, skipping patch."
            );
            return false;
        }
        return true;
    }

    private static bool Prefix()
    {
        if (
            LongEventHandler.toExecuteWhenFinished.Count > 0
            && LoadingProgressWindow.CurrentStage != LoadingStage.Finished
        )
        {
            //LoadingProgressMod.Debug("Running Enumerable version of ExecuteToExecuteWhenFinished() called with " + LongEventHandler.toExecuteWhenFinished.Count + " actions to execute.\n" + Environment.StackTrace);
            Utilities.LongEventHandlerPrependQueue(() =>
            {
                LongEventHandler.QueueLongEvent(
                    ExecuteToExecuteWhenFinished(),
                    "LoadingProgress.ExecuteToExecuteWhenFinished"
                );
            });
            return false;
        }
        return true;
    }

    private static readonly Lazy<MethodInfo?> StaticConstructorOnStartupUtilityCallAllMethod =
        new(() =>
        {
            var methods = StaticConstructorOnStartupCallAllFinder.FindMethodCalling().ToList();
            if (methods.Count == 1)
            {
                return methods[0];
            }
            LoadingProgressMod.Error(
                "Could not find call to StaticConstructorOnStartupUtility.CallAll "
                    + "in PlayDataLoader; "
                    + "static constructor execution will be done without showing progress."
            );
            return null;
        });

    private static readonly Lazy<(
        MethodInfo? method,
        FieldInfo? modContentPackField
    )> ReloadContentIntMethod = new(() =>
    {
        var methodFields = ReloadContentIntFinder.FindMethodCalling().ToList();
        if (methodFields.Count == 1)
        {
            return methodFields[0];
        }
        LoadingProgressMod.Error(
            "Could not find call to ModContentPack.ReloadContentInt in ModContentPack; "
                + "reloading content will be done without showing detailed progress."
        );
        return (null, null);
    });

    private static void RunProfiled(Action action, string label)
    {
        var methodAssembly = action.Method.DeclaringType.Assembly;
        var key = "LoadingProgress.StartupImpact.ExecuteToExecuteWhenFinished|" + label;
        if (methodAssembly.FullName.StartsWith("Assembly-CSharp", StringComparison.Ordinal))
        {
            StartupImpactProfilerUtil.StartBaseGameProfiler(key);
            try
            {
                action();
            }
            finally
            {
                StartupImpactProfilerUtil.StopBaseGameProfiler(key);
            }
        }
        else
        {
            var assemblyMod = Utilities.FindModByAssembly(methodAssembly);
            StartupImpactProfilerUtil.StartModProfiler(assemblyMod, key);
            try
            {
                action();
            }
            finally
            {
                StartupImpactProfilerUtil.StopModProfiler(assemblyMod, key);
            }
        }
    }

    internal static IEnumerable ExecuteToExecuteWhenFinished()
    {
        // Once we start with the ExecuteToExecuteWhenFinished,
        // we need to switch the active thread ID
        LoadingProgressMod.instance.StartupImpact.UpdateActiveThreadId();

        var patchReloadContent = LoadingProgressMod.Settings.PatchReloadContent;

        if (LongEventHandler.executingToExecuteWhenFinished)
        {
            Log.Warning("Already executing.");
            yield break;
        }

        var fasterGameLoadingLoadedMods = FasterGameLoadingUtils.HasFasterGameLoading
            ? FasterGameLoadingUtils.LoadedMods
            : null;

        var staticConstructorOnStartupUtilityCallAllMethod =
            StaticConstructorOnStartupUtilityCallAllMethod.Value;
        var (reloadContentIntMethod, reloadContentIntModContentPackField) =
            ReloadContentIntMethod.Value;

        var actions = LongEventHandler.toExecuteWhenFinished;
        LongEventHandler.executingToExecuteWhenFinished = true;
        if (actions.Count > 0)
        {
            DeepProfiler.Start("ExecuteToExecuteWhenFinished()");
        }
        var reloadProgress = new ReloadContentProgress(
            actions.Count(te => te.Method.Name.Contains("ReloadContent", StringComparison.Ordinal))
                * ReloadContentIntReplacement.StepCount
        );
        for (var i = 0; i < actions.Count; i++)
        {
            var action = actions[i];

            if (
                !StaticConstructorOnStartupUtilityReplacement._callAllCalled
                && action.Method == staticConstructorOnStartupUtilityCallAllMethod
            )
            {
                // If this is the StaticConstructorOnStartupUtility.CallAll method, we want to
                // run it and bail to let it do its own QueueLongEvent.
                StaticConstructorOnStartupUtilityReplacement.Interject();

                DeepProfiler.End();
                // Remove index i too: CallAllAndRest() runs every step of this closure itself,
                // so it must not run a second time when we resume.
                actions.RemoveRange(0, i + 1);
                LongEventHandler.executingToExecuteWhenFinished = false;
                yield break;
            }

            if (patchReloadContent && action.Method == reloadContentIntMethod)
            {
                if (
                    action.Target.GetType() == reloadContentIntMethod.DeclaringType
                    && reloadContentIntModContentPackField is not null
                )
                {
                    var modContentPack = (ModContentPack)
                        reloadContentIntModContentPackField.GetValue(action.Target)!;
                    foreach (
                        var value in ReloadModContent(
                            modContentPack,
                            fasterGameLoadingLoadedMods,
                            reloadProgress
                        )
                    )
                    {
                        yield return value;
                    }
                    continue;
                }

                LoadingProgressMod.Error(
                    "ReloadContentInt was called with target being "
                        + action.Target.GetType().FullName
                        + ":"
                        + action.Target
                        + ", but we expected it to be "
                        + reloadContentIntMethod.DeclaringType.FullName
                        + ":"
                        + reloadContentIntMethod
                );
            }

            ModContentPack_ReloadContentInt_Patch.CurrentModContentPack = null;

            var label = action.Method.DeclaringType.ToString() + " -> " + action.Method.ToString();
            ShowActionProgress(label, i, actions.Count);
            yield return null;

            RunAction(action, label);
        }
        if (actions.Count > 0)
        {
            DeepProfiler.End();
        }
        actions.Clear();
        LongEventHandler.executingToExecuteWhenFinished = false;
        FasterGameLoading_DelayedActions_LateUpdate_Patches._pauseFasterGameLoading_DelayedActions_LateUpdate =
            false;
    }

    private sealed class ReloadContentProgress(int total)
    {
        public int Total { get; } = total;
        public int Completed { get; set; }
    }

    /// <summary>
    /// Runs our step-by-step replacement of ModContentPack.ReloadContentInt for one mod.
    /// </summary>
    private static IEnumerable ReloadModContent(
        ModContentPack modContentPack,
        HashSet<ModContentPack>? fasterGameLoadingLoadedMods,
        ReloadContentProgress progress
    )
    {
        // Pause Faster Game Loading's content loader; we're taking over now.
        FasterGameLoading_DelayedActions_LateUpdate_Patches._pauseFasterGameLoading_DelayedActions_LateUpdate =
            true;
        WarnAboutReloadContentIntPatchesOnce();

        ModContentPack_ReloadContentInt_Patch.CurrentModContentPack = modContentPack;

        // Adding the mod to Faster Game Loading's list makes it skip the mod; if it was already
        // there, Faster Game Loading has loaded it and we skip it instead.
        if (fasterGameLoadingLoadedMods?.Add(modContentPack) == false)
        {
            progress.Completed += ReloadContentIntReplacement.StepCount;
            yield break;
        }

        foreach (var value in ReloadContentIntReplacement.ReloadContentInt(modContentPack))
        {
            LoadingDataTracker.Current = modContentPack.Name;
            LoadingProgressWindow.CurrentLoadingActivity = $"LP.Reload {value}";
            LoadingProgressWindow.StageProgress = (progress.Completed + 1, progress.Total);
            // These labels usually sit right in front of a slow synchronous step (a mod's
            // texture/audio/asset reload), so they need to land on screen even if it means
            // cutting the current 0.1s batch short.
            LongEventHandler_UpdateCurrentEnumeratorEvent_Patches.RequestImmediateRepaint();
            yield return value;
            progress.Completed++;
        }
        // Run the original method to let other mods' prefixes and postfixes run
        modContentPack.ReloadContentInt();
        yield return null;
    }

    /// <summary>
    /// We replace ReloadContentInt with our own enumerated implementation and do not let the
    /// original run, so transpilers might not work as expected. Warn players about it.
    /// </summary>
    private static void WarnAboutReloadContentIntPatchesOnce()
    {
        if (_hasWarnedAboutReloadIntPatches)
        {
            return;
        }
        _hasWarnedAboutReloadIntPatches = true;

        PatchCompat.WarnAboutPatches(
            AccessTools.Method(typeof(ModContentPack), nameof(ModContentPack.ReloadContentInt)),
            false,
            warnKinds: PatchKinds.Transpiler
        );
    }

    private static void ShowActionProgress(string label, int index, int count)
    {
        if (
            LoadingProgressWindow.CurrentStage
            is not (
                LoadingStage.ExecuteToExecuteWhenFinished
                or LoadingStage.ExecuteToExecuteWhenFinished2
            )
        )
        {
            return;
        }

        if (
            !label.Contains("ModContentPack", StringComparison.Ordinal)
            || !label.Contains("ReloadContent", StringComparison.Ordinal)
        )
        {
            LoadingProgressWindow.SetCurrentLoadingActivityRaw(label);
        }
        LoadingProgressWindow.StageProgress = (index + 1, count);
    }

    private static void RunAction(Action action, string label)
    {
        // The label never matches a loading stage, so don't make our DeepProfiler.Start patch
        // search for one.
        DeepProfiler_Start_Patches.Suppress = true;
        try
        {
            DeepProfiler.Start(label);
        }
        finally
        {
            DeepProfiler_Start_Patches.Suppress = false;
        }

        try
        {
            if (Profiler.Enabled)
            {
                RunProfiled(action, label);
            }
            else
            {
                action();
            }
        }
        catch (Exception ex)
        {
            Log.Error("Could not execute post-long-event action. Exception: " + ex);
        }
        finally
        {
            DeepProfiler.End();
        }
    }
}
