using static ru.pyxiion.modrim.LoadingProgress.Constants;

namespace ru.pyxiion.modrim.LoadingProgress.FasterGameLoading;

internal static class FasterGameLoadingProgressWindow
{
    private static readonly Vector2 BaseWindowSize = new(776f, 110f);

    /// <summary>
    /// Shown only while Faster Game Loading is loading mod content early in the background.
    /// </summary>
    private static bool IsVisible =>
        FasterGameLoadingUtils.HasFasterGameLoading
        && FasterGameLoadingUtils.EarlyModContentLoading
        && !FasterGameLoadingUtils.FasterGameLoadingEarlyModContentLoadingIsFinished;

    internal static Vector2 WindowSize => IsVisible ? BaseWindowSize : Vector2.zero;

    internal static ModContentPack? LoadingMod { get; set; }

    internal static void DrawWindow(Rect statusRect)
    {
        if (!IsVisible)
        {
            return;
        }

        Find.WindowStack.ImmediateWindow(
            1217160,
            statusRect,
            WindowLayer.Super,
            delegate
            {
                DrawContents(statusRect.AtZero());
            }
        );
    }

    internal static void DrawContents(Rect rect)
    {
        if (!IsVisible)
        {
            return;
        }

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.UpperLeft;

        var loadingProgressRect = rect;
        loadingProgressRect.x += HorizontalMargin;
        loadingProgressRect.y += 10f;
        loadingProgressRect.width -= 2 * HorizontalMargin;
        loadingProgressRect.height = Text.LineHeight;

        Widgets.Label(
            loadingProgressRect,
            Translations.GetTranslation($"LoadingProgress.FasterGameLoadingEarlyModContentLoading")
        );

        var loadingActivityRect = loadingProgressRect;
        loadingProgressRect.y += loadingProgressRect.height + VerticalWidgetMargin;
        Text.Font = GameFont.Small;
        loadingActivityRect.height = Text.LineHeight;

        var label = string.Empty;
        if (LoadingMod != null)
        {
            label = Translations.GetTranslation(
                $"LoadingProgress.FasterGameLoadingEarlyModContentLoading.ForMod",
                LoadingMod.Name
            );
        }

        if (!string.IsNullOrEmpty(label))
        {
            var ellipsisRect = loadingProgressRect;
            ellipsisRect.width -= 10f;
            Widgets.Label(
                loadingProgressRect,
                Utilities.ClampTextWithEllipsisMarkupAware(ellipsisRect, label)
            );
        }

        var progressRect = loadingProgressRect;
        progressRect.y += loadingActivityRect.height + VerticalWidgetMargin;
        progressRect.height = ProgressBarHeight;

        Widgets_Progressbar.DrawHorizontalProgressBar(
            progressRect,
            FasterGameLoadingUtils.LoadedMods!.Count,
            LoadedModManager.RunningModsListForReading.Count
        );
    }
}
