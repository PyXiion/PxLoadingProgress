using System.Diagnostics;

namespace ru.pyxiion.modrim.LoadingProgress;

internal sealed class LoadingProgressMod : Mod
{
#pragma warning disable CS8618 // Set by constructor
    internal static LoadingProgressMod instance;
    internal StartupImpact.StartupImpact StartupImpact;
    internal Harmony harmony;
#pragma warning restore CS8618

    public LoadingProgressMod(ModContentPack content)
        : base(content)
    {
        instance = this;
        StartupImpact = new StartupImpact.StartupImpact();

        harmony = new(content.PackageId);
        harmony.PatchAllUncategorized(Assembly.GetExecutingAssembly());

        if (Settings.TrackStartupLoadingImpact)
        {
            harmony.PatchCategory(Assembly.GetExecutingAssembly(), "StartupImpact");
        }

        Message("Loading Progress initialized! Enjoy the rest of your loading experience!");
    }

    // Read on hot paths during loading; GetSettings does a type check on every call.
    public static Settings Settings => field ??= instance.GetSettings<Settings>();

    private static bool _loadingPatchesRemoved;

    /// <summary>
    /// Called every frame from the main menu. The first time loading is found to be finished,
    /// removes the patches that only matter during loading, so they cost nothing in-game.
    /// Doing it here rather than at the end of loading keeps the cost out of the loading time.
    /// </summary>
    internal static void RemoveLoadingPatchesIfFinished()
    {
        if (_loadingPatchesRemoved || LoadingProgressWindow.CurrentStage != LoadingStage.Finished)
        {
            return;
        }
        _loadingPatchesRemoved = true;

        try
        {
            var harmony = instance.harmony;
            harmony.Unpatch(
                AccessTools.Method(typeof(DeepProfiler), nameof(DeepProfiler.Start)),
                HarmonyPatchType.Prefix,
                harmony.Id
            );
            if (Settings.TrackStartupLoadingImpact)
            {
                harmony.UnpatchCategory(Assembly.GetExecutingAssembly(), "StartupImpact");
            }
        }
        catch (Exception e)
        {
            Exception("Failed to remove loading-only patches", e);
        }
    }

    public override void DoSettingsWindowContents(Rect inRect) =>
        Settings.DoSettingsWindowContents(inRect);

    public override string SettingsCategory() => Content.Name;

    public static void Message(string msg) => Log.Message("[Loading Progress] " + msg);

    public static void DevMessage(string msg)
    {
        if (Prefs.DevMode)
        {
            Log.Message($"[Loading Progress][DEV] " + msg);
        }
    }

    [Conditional("DEBUG")]
    public static void Debug(string message)
    {
        Log.ResetMessageCount();
        DevMessage(message);
    }

    public static void Warning(string msg) => Log.Warning("[Loading Progress] " + msg);

    public static void Error(string msg) => Log.Error("[Loading Progress] " + msg);

    public static void Exception(string msg, Exception? e = null)
    {
        Message(msg);
        if (e != null)
        {
            Log.Error(e.ToString());
        }
    }
}
