using System.Diagnostics;

namespace ru.pyxiion.modrim.LoadingProgress.StartupImpact;

internal sealed class StartupImpact
{
    private static int _activeThreadId;
    private readonly Stopwatch _loadingStopwatch = new();

    public ModInfoList Modlist { get; } = new();

    /// <summary>
    /// The total loading time, set only after FinishLoading() is called.
    /// </summary>
    public float TotalLoadingTime { get; private set; }
    public Profiler BaseGameProfiler { get; }

    public StartupImpact()
    {
        _activeThreadId = Environment.CurrentManagedThreadId;

        BaseGameProfiler = new Profiler("base game");

        Profiler.Enabled = LoadingProgressMod.Settings.TrackStartupLoadingImpact;
        if (Profiler.Enabled)
        {
            _loadingStopwatch.Start();
            ModClassProfiler.Active = true;
        }
    }

    private bool _loadingTimeMeasured;

    public void FinishLoading()
    {
        if (!_loadingTimeMeasured)
        {
            _loadingTimeMeasured = true;
            _loadingStopwatch.Stop();
            TotalLoadingTime = (float)_loadingStopwatch.Elapsed.TotalMilliseconds;

            // Unpatching is deferred to the main menu (see LoadingProgressMod.OnMainMenu) so it
            // doesn't add to the loading time; the patched methods are loading-only anyway.

            // FinishLoading can run off the main thread — defer the save.
            LongEventHandler.ExecuteWhenFinished(static () =>
            {
                try
                {
                    Dialog.StartupImpactSessionStorage.Save(
                        Dialog.StartupImpactSessionData.FromCurrentSession()
                    );
                }
                catch (Exception e)
                {
                    LoadingProgressMod.Error(
                        "Failed to auto-save startup impact report: " + e
                    );
                }
            });
        }
    }

#pragma warning disable CA1822 // Mark members as static
    public void UpdateActiveThreadId() => _activeThreadId = Environment.CurrentManagedThreadId;
#pragma warning restore CA1822 // Mark members as static

    public static bool IsActiveThread() => Environment.CurrentManagedThreadId == _activeThreadId;
}
