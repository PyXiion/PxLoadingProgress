using System.Reflection.Emit;

namespace ru.pyxiion.modrim.LoadingProgress;

[HarmonyPatch(typeof(DirectXmlCrossRefLoader))]
internal static class DirectXmlCrossRefLoader_ResolveAllWantedCrossReferences_LoadingDataTracker_Patches
{
    [HarmonyPatch(nameof(DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences))]
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
                AccessTools.Method(
                    typeof(DirectXmlCrossRefLoader.WantedRef),
                    nameof(DirectXmlCrossRefLoader.WantedRef.Apply)
                )
            )
        );
        if (codeMatcher.IsInvalid)
        {
            LoadingProgressMod.Error(
                "XmlInheritance.ResolveXmlNodes: Could not find a call to List<>.get_Item."
            );
            return original;
        }

        _ = codeMatcher
            .Advance(1)
            .Insert(
                [
                    new(
                        OpCodes.Call,
                        AccessTools.Method(
                            typeof(DirectXmlCrossRefLoader_ResolveAllWantedCrossReferences_LoadingDataTracker_Patches),
                            nameof(Stage2Progress)
                        )
                    ),
                ]
            );

        return codeMatcher.Instructions();
    }

    private static void Stage2Progress()
    {
        if (
            LoadingProgressWindow.CurrentStage
            <= LoadingStage.ResolveCrossReferencesBetweenNonImpliedDefsStage2
        )
        {
            LoadingProgressWindow.CurrentStage =
                LoadingStage.ResolveCrossReferencesBetweenNonImpliedDefsStage2;
        }
        LoadingProgressWindow.StageProgress = (
            LoadingDataTracker.WantedRefApplyCount++,
            DirectXmlCrossRefLoader.wantedRefs.Count
        );
    }
}
