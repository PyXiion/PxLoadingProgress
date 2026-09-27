using ru.pyxiion.modrim.LoadingProgress.FasterGameLoading;

namespace ru.pyxiion.modrim.LoadingProgress;

/// <summary>
/// Vertical layout of the loading screen: the vanilla status box, then our progress window,
/// then (optionally) the Faster Game Loading window, all positioned per the placement setting.
/// </summary>
internal static class LoadingScreenLayout
{
    private const float Gap = 10f;

    /// <summary>
    /// The y coordinate of the top of our progress window.
    /// </summary>
    internal static float ProgressWindowTop
    {
        get
        {
            var windowsHeight =
                LoadingProgressWindow.WindowSize.y + FasterGameLoadingProgressWindow.WindowSize.y;
            return LoadingProgressMod.Settings.LoadingWindowPlacement switch
            {
                LoadingWindowPlacement.Top => Gap + LongEventHandler.StatusRectSize.y + Gap,
                LoadingWindowPlacement.Middle => (UI.screenHeight - windowsHeight) / 2f,
                LoadingWindowPlacement.Bottom => UI.screenHeight
                    - windowsHeight
                    - Gap
                    - (FasterGameLoadingProgressWindow.WindowSize.y > 0 ? Gap : 0f),
                LoadingWindowPlacement.Custom => 0f,
                _ => 0f,
            };
        }
    }

    /// <summary>
    /// The y coordinate of the top of the vanilla status box, which sits right above our window.
    /// </summary>
    internal static float StatusRectTop =>
        LoadingProgressMod.Settings.LoadingWindowPlacement
            is LoadingWindowPlacement.Top
                or LoadingWindowPlacement.Middle
                or LoadingWindowPlacement.Bottom
            ? ProgressWindowTop - LongEventHandler.StatusRectSize.y - Gap
            : 0f;

    internal static Rect CenteredRect(float y, Vector2 size) =>
        new((UI.screenWidth - size.x) / 2f, y, size.x, size.y);

    internal static float GapBelow(Rect rect) => rect.yMax + Gap;
}
