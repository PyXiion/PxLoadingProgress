namespace ru.pyxiion.modrim.LoadingProgress.StartupImpact.Dialog;

internal static class StartupImpactSessionStorage
{
    private const string SaveFileName = "StartupImpactData.xml";
    private const string SaveLabel = "sessionData";

    internal static string SaveFilePath =>
        Path.Combine(GenFilePaths.SaveDataFolderPath, GenText.SanitizeFilename(SaveFileName));

    internal static void Save(StartupImpactSessionData sessionData)
    {
        Scribe.saver.InitSaving(SaveFilePath, "StartupImpactSession");
        Scribe_Deep.Look(ref sessionData, SaveLabel);
        Scribe.saver.FinalizeSaving();
    }

    internal static StartupImpactSessionData? Load()
    {
        StartupImpactSessionData? sessionData = null;
        if (File.Exists(SaveFilePath))
        {
            Scribe.loader.InitLoading(SaveFilePath);
            Scribe_Deep.Look(ref sessionData, SaveLabel);
            Scribe.loader.FinalizeLoading();
        }
        return sessionData;
    }
}
