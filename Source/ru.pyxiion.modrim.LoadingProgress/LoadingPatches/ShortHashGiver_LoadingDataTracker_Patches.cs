namespace ru.pyxiion.modrim.LoadingProgress;

// ShortHashGiver.GiveAllShortHashes hands work out via Parallel.ForEach across all def types,
// with GiveShortHash called concurrently from multiple threads. It reports no progress of its
// own, so without this the stage just appears to hang for its full duration.
[HarmonyPatch(typeof(ShortHashGiver))]
internal static class ShortHashGiver_LoadingDataTracker_Patches
{
    [HarmonyPatch(nameof(ShortHashGiver.GiveAllShortHashes))]
    [HarmonyPrefix]
    private static void GiveAllShortHashesPrefix()
    {
        if (LoadingProgressWindow.CurrentStage != LoadingStage.ShortHashGiving)
        {
            return;
        }

        LoadingDataTracker.ShortHashesGivenCount = 0;
        LoadingProgressWindow.StageProgress = (0, CountAllDefsToHash());
    }

    [HarmonyPatch("GiveShortHash")]
    [HarmonyPostfix]
    private static void GiveShortHashPostfix()
    {
        if (LoadingProgressWindow.CurrentStage != LoadingStage.ShortHashGiving)
        {
            return;
        }

        var count = Interlocked.Increment(ref LoadingDataTracker.ShortHashesGivenCount);
        if (LoadingProgressWindow.StageProgress is (_, float total))
        {
            LoadingProgressWindow.StageProgress = (count, total);
        }
    }

    private static int CountAllDefsToHash()
    {
        var total = 0;
        foreach (var defType in GenDefDatabase.AllDefTypesWithDatabases())
        {
            var databaseType = typeof(DefDatabase<>).MakeGenericType(defType);
            var allDefs = (IList)databaseType.GetProperty("AllDefsListForReading").GetValue(null);
            total += allDefs.Count;
        }
        return total;
    }
}
