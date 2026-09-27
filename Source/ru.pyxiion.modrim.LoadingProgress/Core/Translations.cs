using RimWorld.IO;

namespace ru.pyxiion.modrim.LoadingProgress;

internal static class Translations
{
    private static Dictionary<string, string>? EnglishTranslationValues;
    private static bool _englishTranslationsLoaded;
    private static Dictionary<string, string>? ActiveLanguageTranslationValues;
    private static bool _activeLanguageTranslationsLoaded;

    public static void Clear()
    {
        EnglishTranslationValues = null;
        ActiveLanguageTranslationValues = null;
    }

    public static string GetTranslation(string translationKey, params object[] args)
    {
        if (translationKey == null)
        {
            return "[null translation key]";
        }

        if (!_englishTranslationsLoaded)
        {
            var englishLanguageDirectory = AbstractFilesystem
                .GetDirectory(
                    Path.Combine(
                        LoadingProgressMod.instance.Content.RootDir,
                        "Common",
                        "Languages",
                        LanguageDatabase.DefaultLangFolderName
                    )
                )
                .GetDirectory("Keyed");
            LoadLanguage(ref EnglishTranslationValues, englishLanguageDirectory);
            _englishTranslationsLoaded = true;
        }

        if (!_activeLanguageTranslationsLoaded && Prefs.LangFolderName != "English")
        {
            var languageFolderName = Prefs.LangFolderName;
            // Like RimWorld's own LoadedLanguage.AllDirectories, accept both the canonical
            // folder name (e.g. "Polish (Polski)") and the legacy one (e.g. "Polish").
            var parenIndex = languageFolderName.IndexOf('(', StringComparison.Ordinal);
            var legacyLanguageFolderName =
                parenIndex > 0 ? languageFolderName[..parenIndex].Trim() : languageFolderName;
            string[] candidateFolderNames =
                languageFolderName == legacyLanguageFolderName
                    ? [languageFolderName]
                    : [languageFolderName, legacyLanguageFolderName];
            foreach (var mod in LoadedModManager.RunningMods)
            {
                foreach (var loadFolder in mod.foldersToLoadDescendingOrder)
                {
                    foreach (var candidateFolderName in candidateFolderNames)
                    {
                        var languageDirectory = AbstractFilesystem
                            .GetDirectory(Path.Join(loadFolder, "Languages", candidateFolderName))
                            .GetDirectory("Keyed");
                        LoadLanguage(ref ActiveLanguageTranslationValues, languageDirectory);
                        if (ActiveLanguageTranslationValues is not null)
                        {
                            LoadingProgressMod.Message(
                                $"Loaded translations for {languageFolderName} from {mod.Name} from {languageDirectory.FullPath}."
                            );
                            break;
                        }
                    }
                    if (ActiveLanguageTranslationValues is not null)
                    {
                        break;
                    }
                }
                if (ActiveLanguageTranslationValues is not null)
                {
                    break;
                }
            }
            _activeLanguageTranslationsLoaded = true;
        }

        if (ActiveLanguageTranslationValues is not null)
        {
            if (
                ActiveLanguageTranslationValues.TryGetValue(
                    translationKey,
                    out var activeLanguageTranslation
                )
            )
            {
                return string.Format(CultureInfo.CurrentCulture, activeLanguageTranslation, args);
            }
        }

        if (EnglishTranslationValues!.TryGetValue(translationKey, out var englishTranslation))
        {
            return string.Format(CultureInfo.CurrentCulture, englishTranslation, args);
        }
        else
        {
            LoadingProgressMod.Warning($"No translation found for {translationKey}");
            return Translator.PseudoTranslated(translationKey);
        }
    }

    private static void LoadLanguage(
        ref Dictionary<string, string>? languageDictionary,
        VirtualDirectory languageDirectory
    )
    {
        // Goes through RimWorld's virtual filesystem so translations packed as .tar are found too.
        if (!languageDirectory.Exists)
        {
            return;
        }
        foreach (var file in languageDirectory.GetFiles("*.xml", SearchOption.AllDirectories))
        {
            try
            {
                var translationContent = file.ReadAllText();
                if (!translationContent.Contains("LoadingProgress.", StringComparison.Ordinal))
                {
                    continue;
                }
                foreach (var x in DirectXmlLoaderSimple.ValuesFromXmlFile(translationContent))
                {
                    languageDictionary ??= [];
                    languageDictionary[x.key] = x.value;
                }
            }
            catch (Exception e)
            {
                // A malformed language file shouldn't take down the loading screen.
                LoadingProgressMod.Warning($"Failed to load translations from {file.FullPath}: {e}");
            }
        }
    }
}
