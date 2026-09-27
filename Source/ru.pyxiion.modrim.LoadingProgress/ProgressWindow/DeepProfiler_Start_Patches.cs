using System.Diagnostics;
using ru.pyxiion.modrim.LoadingProgress.StartupImpact;

namespace ru.pyxiion.modrim.LoadingProgress;

[HarmonyPatch(typeof(DeepProfiler), nameof(DeepProfiler.Start))]
internal static class DeepProfiler_Start_Patches
{
    /// <summary>
    /// Set by our own code around DeepProfiler.Start calls whose labels are only meaningful to
    /// the DeepProfiler itself, so they skip the stage-matching logic entirely.
    /// </summary>
    [ThreadStatic]
    internal static bool Suppress;

    private static void Prefix(string label)
    {
        if (label == null)
        {
            var method = new StackTrace().GetFrame(2)?.GetMethod();
            var declaringType = method?.DeclaringType;
            var mod =
                declaringType != null ? Utilities.FindModByAssembly(declaringType.Assembly) : null;
            var callerDescription =
                declaringType != null && method != null
                    ? $"{declaringType.FullName}.{method.Name}"
                    : "[unknown caller]";
            LoadingProgressMod.Warning(
                $"Why is {callerDescription} from {mod?.Name ?? "{unknown}"} calling DeepProfiler.Start (and by extension our patch) with null?! Stop it."
            );
            return;
        }

        if (Suppress || LoadingProgressWindow.CurrentStage == LoadingStage.Finished)
        {
            return;
        }

        LoadingProgressWindow.CurrentLoadingActivity = label;

        if (ModClassProfiler.Active)
        {
            ModClassProfiler.OnDeepProfilerStart(label);
        }
    }
}
