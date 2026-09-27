using System.Diagnostics;

namespace ru.pyxiion.modrim.LoadingProgress.StartupImpact;

/// <summary>
/// Measures mod class constructors without patching every one of them.
/// <see cref="LoadedModManager.CreateModClasses"/> wraps each constructor in
/// <c>DeepProfiler.Start("Loading {type} mod class")</c>, which we already observe for the
/// progress window, so a constructor's time is the span between its label and the next
/// mod class label (or the first label after the mod class stage ends).
/// </summary>
internal static class ModClassProfiler
{
    private const string LabelPrefix = "Loading ";
    private const string LabelSuffix = " mod class";
    private const string Category = "LoadingProgress.StartupImpact.ModConstructor";

    /// <summary>
    /// True while mod classes may still be getting constructed and tracking is enabled.
    /// </summary>
    internal static bool Active;

    private static string? _currentTypeName;
    private static long _startTimestamp;
    private static Dictionary<string, Type>? _modTypesByName;

    internal static void OnDeepProfilerStart(string label)
    {
        if (
            label.Length > LabelPrefix.Length + LabelSuffix.Length
            && label.StartsWith(LabelPrefix, StringComparison.Ordinal)
            && label.EndsWith(LabelSuffix, StringComparison.Ordinal)
        )
        {
            var now = Stopwatch.GetTimestamp();
            Close(now);
            _currentTypeName = label.Substring(
                LabelPrefix.Length,
                label.Length - LabelPrefix.Length - LabelSuffix.Length
            );
            _startTimestamp = now;
        }
        else if (LoadingProgressWindow.CurrentStage > LoadingStage.LoadingModClasses)
        {
            // Labels coming from inside a constructor don't end the measurement; only the
            // first label of the next stage does.
            Close(Stopwatch.GetTimestamp());
            Active = false;
            _modTypesByName = null;
        }
    }

    private static void Close(long now)
    {
        if (_currentTypeName is not { } typeName)
        {
            return;
        }
        _currentTypeName = null;

        _modTypesByName ??= typeof(Mod)
            .InstantiableDescendantsAndSelf()
            .GroupBy(t => t.ToString())
            .ToDictionary(g => g.Key, g => g.First());
        if (!_modTypesByName.TryGetValue(typeName, out var type))
        {
            return;
        }

        var mod = Utilities.FindModByAssembly(type.Assembly);
        if (mod == null)
        {
            return;
        }

        LoadingProgressMod
            .instance.StartupImpact.Modlist.GetModInfoFor(mod)
            ?.Profiler.AddTicks(Category, now - _startTimestamp);
    }
}
