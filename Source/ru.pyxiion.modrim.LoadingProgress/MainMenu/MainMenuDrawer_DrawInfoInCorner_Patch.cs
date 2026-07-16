namespace ru.pyxiion.modrim.LoadingProgress;

[HarmonyPatch(typeof(MainMenuDrawer), nameof(MainMenuDrawer.DoMainMenuControls))]
internal static class MainMenuDrawer_DrawInfoInCorner_Patch
{
    internal static void Postfix(Rect rect)
    {
        if (Current.ProgramState != ProgramState.Playing)
        {
            // You are not in the game
            return;
        }
        rect.x += rect.width;
        rect.y += rect.height;
        rect.width = 0;
        rect.height = 0;
        VersionControl_DrawInfoInCorner_Patch.DrawLoadingTime(rect);
    }
}
