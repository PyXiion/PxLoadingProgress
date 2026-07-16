using System.Reflection.Emit;
using System.Xml;

namespace ru.pyxiion.modrim.LoadingProgress;

[HarmonyPatch(typeof(LoadedModManager))]
internal static class LoadedModManager_LoadingDataTracker_Patches
{
    [HarmonyPatch(nameof(LoadedModManager.CombineIntoUnifiedXML))]
    [HarmonyPrefix]
    private static void CombineIntoUnifiedXMLPrefix(List<LoadableXmlAsset> xmls)
    {
        if (LoadingProgressWindow.CurrentStage != LoadingStage.CombineIntoUnifiedXml)
        {
            return;
        }

        var total = xmls.SelectMany(x => x.xmlDoc.DocumentElement.ChildNodes.Cast<XmlNode>())
            .Count();
        LoadingProgressWindow.StageProgress = (0, total);
    }

    [HarmonyPatch(nameof(LoadedModManager.CombineIntoUnifiedXML))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> CombineIntoUnifiedXMLTranspiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator
    )
    {
        var original = instructions.ToList();

        var codeMatcher = new CodeMatcher(original, generator);

        _ = codeMatcher.SearchForward(i =>
            i.opcode == OpCodes.Castclass && i.operand is Type type && type == typeof(XmlNode)
        );
        if (codeMatcher.IsInvalid)
        {
            LoadingProgressMod.Error(
                "LoadedModManager.CombineIntoUnifiedXML: Could not find a cast to XmlNode."
            );
            return original;
        }

        _ = codeMatcher
            .Advance(2)
            .Insert(
                [
                    new(
                        OpCodes.Call,
                        AccessTools.Method(
                            typeof(LoadedModManager_LoadingDataTracker_Patches),
                            nameof(CombineIntoUnifiedXMLProgress)
                        )
                    ),
                ]
            );

        return codeMatcher.Instructions();
    }

    private static void CombineIntoUnifiedXMLProgress()
    {
        if (
            LoadingProgressWindow.CurrentStage == LoadingStage.CombineIntoUnifiedXml
            && LoadingProgressWindow.StageProgress is (float index, float total)
        )
        {
            LoadingProgressWindow.StageProgress = ((int)index + 1, total);
        }
    }
}
