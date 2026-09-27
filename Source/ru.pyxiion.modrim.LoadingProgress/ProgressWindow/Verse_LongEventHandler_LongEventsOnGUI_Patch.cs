using System.Reflection.Emit;
using ru.pyxiion.modrim.LoadingProgress.FasterGameLoading;

namespace ru.pyxiion.modrim.LoadingProgress;

[HarmonyPatch(typeof(LongEventHandler), nameof(LongEventHandler.DrawLongEventWindowContents))]
internal sealed class Verse_LongEventHandler_DrawLongEventWindowContents_Patch
{
    private static void Postfix()
    {
        if (LoadingProgressWindow.CurrentStage == LoadingStage.Finished)
        {
            return;
        }

        var useImmediateWindow =
            LongEventHandler.currentEvent.UseStandardWindow
            && Find.UIRoot != null
            && Find.WindowStack != null;

        var rect = LoadingScreenLayout.CenteredRect(
            LoadingScreenLayout.ProgressWindowTop,
            LoadingProgressWindow.WindowSize
        );
        DrawPanel(
            rect,
            useImmediateWindow,
            LoadingProgressWindow.DrawWindow,
            LoadingProgressWindow.DrawContents
        );

        rect = LoadingScreenLayout.CenteredRect(
            LoadingScreenLayout.GapBelow(rect),
            FasterGameLoadingProgressWindow.WindowSize
        );
        DrawPanel(
            rect,
            useImmediateWindow,
            FasterGameLoadingProgressWindow.DrawWindow,
            FasterGameLoadingProgressWindow.DrawContents
        );
    }

    private static void DrawPanel(
        Rect rect,
        bool useImmediateWindow,
        Action<Rect> drawWindow,
        Action<Rect> drawContents
    )
    {
        if (useImmediateWindow)
        {
            drawWindow(rect);
        }
        else
        {
            Widgets.DrawShadowAround(rect);
            Widgets.DrawWindowBackground(rect);
            drawContents(rect);
        }
    }
}

[HarmonyPatch(typeof(LongEventHandler), nameof(LongEventHandler.LongEventsOnGUI))]
internal sealed class Verse_LongEventHandler_LongEventsOnGUI_Patch
{
    private static readonly MethodInfo _method_GenUI_Rounded = AccessTools.Method(
        typeof(GenUI),
        nameof(GenUI.Rounded),
        [typeof(Rect)]
    );
    private static readonly MethodInfo _methodAdjustStatusWindowRect = AccessTools.Method(
        typeof(Verse_LongEventHandler_LongEventsOnGUI_Patch),
        nameof(AdjustStatusWindowRect)
    );

    private static Rect AdjustStatusWindowRect(Rect r)
    {
        if (LoadingProgressWindow.CurrentStage != LoadingStage.Finished)
        {
            r.y = LoadingScreenLayout.StatusRectTop;
        }
        return r;
    }

    private static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator
    )
    {
        var originalInstructionList = instructions.ToList();

        var codeMatcher = new CodeMatcher(originalInstructionList, generator);

        _ = codeMatcher.SearchForward(i =>
            i.opcode == OpCodes.Call && i.operand is MethodInfo m && m == _method_GenUI_Rounded
        );
        if (!codeMatcher.IsValid)
        {
            LoadingProgressMod.Error(
                $"Could not patch LongEventHandler.LongEventsOnGUI, IL does not match expectations ([call GenUI.Rounded])"
            );
            return originalInstructionList;
        }

        _ = codeMatcher.Advance(1).Insert([new(OpCodes.Call, _methodAdjustStatusWindowRect)]);

        return codeMatcher.Instructions();
    }
}
