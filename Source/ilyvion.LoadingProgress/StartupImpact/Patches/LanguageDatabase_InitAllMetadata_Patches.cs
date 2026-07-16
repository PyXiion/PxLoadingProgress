using System.Reflection.Emit;

namespace ilyvion.LoadingProgress.StartupImpact.Patches;

[HarmonyPatch(typeof(LanguageDatabase), nameof(LanguageDatabase.InitAllMetadata))]
[HarmonyPatchCategory("StartupImpact")]
internal static class LanguageDatabase_InitAllMetadata_Patches
{
    private static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator
    )
    {
        var mcpLocal = generator.DeclareLocal(typeof(ModContentPack));

        return TranspilerHelper.ProfileCurrentMoveNext(
            instructions,
            generator,
            "LanguageDatabase.InitAllMetadata",
            i => i.Calls(
                AccessTools.PropertyGetter(
                    typeof(IEnumerator<ModContentPack>),
                    nameof(IEnumerator.Current)
                )
            ),
            1,
            i => i.Calls(
                AccessTools.Method(typeof(IEnumerator), nameof(IEnumerator.MoveNext))
            ),
            [
                new(OpCodes.Dup),
                new(OpCodes.Dup),
                new(OpCodes.Stloc, mcpLocal.LocalIndex),
                new(
                    OpCodes.Call,
                    AccessTools.Method(
                        typeof(LanguageDatabase_InitAllMetadata_Patches),
                        nameof(BeforeInitAllMetadata)
                    )
                ),
            ],
            [
                new(OpCodes.Ldloc, mcpLocal.LocalIndex),
                new(
                    OpCodes.Call,
                    AccessTools.Method(
                        typeof(LanguageDatabase_InitAllMetadata_Patches),
                        nameof(AfterInitAllMetadata)
                    )
                ),
            ]
        );
    }

    private static void BeforeInitAllMetadata(ModContentPack modContentPack) =>
        StartupImpactProfilerUtil.StartModProfiler(
            modContentPack,
            "LoadingProgress.StartupImpact.LanguageDatabaseInitAllMetadata"
        );

    private static void AfterInitAllMetadata(ModContentPack modContentPack) =>
        StartupImpactProfilerUtil.StopModProfiler(
            modContentPack,
            "LoadingProgress.StartupImpact.LanguageDatabaseInitAllMetadata"
        );
}
