using System.Reflection.Emit;

namespace ru.pyxiion.modrim.LoadingProgress.StartupImpact.Patches;

[HarmonyPatch(typeof(LoadedModManager), nameof(LoadedModManager.ClearCachedPatches))]
[HarmonyPatchCategory("StartupImpact")]
internal static class LoadedModManager_ClearCachedPatches_Patches
{
    private static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator
    ) =>
        TranspilerHelper.ProfileCurrentMoveNext(
            instructions,
            generator,
            "LoadedModManager.ClearCachedPatches",
            i => i.opcode == OpCodes.Call
                && i.operand is MethodInfo method
                && method.Name == "get_Current",
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
                        typeof(LoadedModManager_ClearCachedPatches_Patches),
                        nameof(BeforeClearCachedPatches)
                    )
                ),
            ],
            [
                new(OpCodes.Ldloc_1),
                new(
                    OpCodes.Call,
                    AccessTools.Method(
                        typeof(LoadedModManager_ClearCachedPatches_Patches),
                        nameof(AfterClearCachedPatches)
                    )
                ),
            ]
        );

    private static void BeforeClearCachedPatches(ModContentPack modContentPack) =>
        StartupImpactProfilerUtil.StartModProfiler(
            modContentPack,
            "LoadingProgress.StartupImpact.ClearCachedPatches"
        );

    private static void AfterClearCachedPatches(ModContentPack modContentPack) =>
        StartupImpactProfilerUtil.StopModProfiler(
            modContentPack,
            "LoadingProgress.StartupImpact.ClearCachedPatches"
        );
}
