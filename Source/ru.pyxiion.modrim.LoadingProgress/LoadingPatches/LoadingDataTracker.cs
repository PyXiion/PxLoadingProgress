namespace ru.pyxiion.modrim.LoadingProgress;

internal static class LoadingDataTracker
{
    public static string? Previous;
    public static string? Current;
    public static bool ModChanged => Previous != Current;

    public static void SwitchTo(string? current)
    {
        Previous = Current;
        Current = current;
    }

    internal static Def? LastDef;
    internal static int WantedRefApplyCount;
}
