namespace ilyvion.LoadingProgress;

internal static class LoadingDataTracker
{
    public static string? Previous;
    public static string? Current;
    public static bool ModChanged => Previous != Current;

    internal static Def? LastDef;
    internal static int WantedRefTryResolveCount;
    internal static int WantedRefApplyCount;
}
