using System.Reflection.Emit;

namespace ilyvion.LoadingProgress;

[HarmonyPatch(typeof(XmlInheritance))]
internal static class XmlInheritance_LoadingDataTracker_Patches
{
    [HarmonyPatch(nameof(XmlInheritance.TryRegister))]
    [HarmonyPrefix]
    private static void TryRegisterPrefix(ModContentPack? mod)
    {
        if (LoadingProgressWindow.CurrentStage != LoadingStage.ParseAndProcessXml)
        {
            return;
        }

        if (mod != null)
        {
            LoadingDataTracker.Previous = LoadingDataTracker.Current;
            LoadingDataTracker.Current = mod.Name;
        }
    }

    [HarmonyPatch(nameof(XmlInheritance.ResolveXmlNodes))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> ResolveXmlNodesTranspiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator
    )
    {
        var original = instructions.ToList();

        var codeMatcher = new CodeMatcher(original, generator);

        _ = codeMatcher.SearchForward(i =>
            i.Calls(
                AccessTools
                    .Indexer(typeof(List<XmlInheritance.XmlInheritanceNode>), [typeof(int)])
                    .GetGetMethod()
            )
        );
        if (codeMatcher.IsInvalid)
        {
            LoadingProgressMod.Error(
                "XmlInheritance.ResolveXmlNodes: Could not find a call to List<>.get_Item."
            );
            return original;
        }

        _ = codeMatcher.Insert(
            [
                new(
                    OpCodes.Call,
                    AccessTools.Method(
                        typeof(XmlInheritance_LoadingDataTracker_Patches),
                        nameof(XmlInheritanceProgress)
                    )
                ),
                new(OpCodes.Ldloc_0),
                new(OpCodes.Ldloc_1),
            ]
        );

        return codeMatcher.Instructions();
    }

    private static void XmlInheritanceProgress(
        List<XmlInheritance.XmlInheritanceNode> unresolvedNodes,
        int index
    )
    {
        if (
            LoadingProgressWindow.CurrentStage == LoadingStage.XmlInheritanceResolve
            && unresolvedNodes.Count > 0
        )
        {
            LoadingProgressWindow.StageProgress = (index + 1, unresolvedNodes.Count);
        }
    }
}
