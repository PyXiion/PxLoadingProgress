using System.Reflection.Emit;

namespace ru.pyxiion.modrim.LoadingProgress.StartupImpact.Patches;

[HarmonyPatch(typeof(LoadedModManager), nameof(LoadedModManager.ErrorCheckPatches))]
[HarmonyPatchCategory("StartupImpact")]
internal static class LoadedModManager_ErrorCheckPatches_Patches
{
    private static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator
    ) =>
        TranspilerHelper.ProfileCurrentMoveNext(
            instructions,
            generator,
            "LoadedModManager.ErrorCheckPatches",
            i => i.Calls(
                AccessTools.PropertyGetter(
                    typeof(List<ModContentPack>.Enumerator),
                    nameof(IEnumerator.Current)
                )
            ),
            2,
            i => i.Calls(
                AccessTools.Method(
                    typeof(List<ModContentPack>.Enumerator),
                    nameof(IEnumerator.MoveNext)
                )
            ),
            [
                new(OpCodes.Ldloc_1),
                new(
                    OpCodes.Call,
                    AccessTools.Method(
                        typeof(LoadedModManager_ErrorCheckPatches_Patches),
                        nameof(BeforeErrorCheckPatches)
                    )
                ),
            ],
            [
                new(OpCodes.Ldloc_1),
                new(
                    OpCodes.Call,
                    AccessTools.Method(
                        typeof(LoadedModManager_ErrorCheckPatches_Patches),
                        nameof(AfterErrorCheckPatches)
                    )
                ),
            ]
        );

    private static void BeforeErrorCheckPatches(ModContentPack modContentPack) =>
        StartupImpactProfilerUtil.StartModProfiler(
            modContentPack,
            "LoadingProgress.StartupImpact.ErrorCheckPatches"
        );

    private static void AfterErrorCheckPatches(ModContentPack modContentPack) =>
        StartupImpactProfilerUtil.StopModProfiler(
            modContentPack,
            "LoadingProgress.StartupImpact.ErrorCheckPatches"
        );
}
