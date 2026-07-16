namespace ru.pyxiion.modrim.LoadingProgress.StartupImpact.Dialog;

internal static class StartupImpactSessionStorage
{
    private const string SaveFileName = "StartupImpactData.json";

    internal static string SaveFilePath =>
        Path.Combine(GenFilePaths.SaveDataFolderPath, GenText.SanitizeFilename(SaveFileName));

    internal static void Save(StartupImpactSessionData sessionData)
    {
        var json = SimpleJson.Serialize(sessionData);
        File.WriteAllText(SaveFilePath, json);
    }

    internal static StartupImpactSessionData? Load()
    {
        if (!File.Exists(SaveFilePath))
        {
            return null;
        }
        var json = File.ReadAllText(SaveFilePath);
        return SimpleJson.DeserializeSession(json);
    }
}
