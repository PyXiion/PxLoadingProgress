using System.Reflection.Emit;

namespace ilyvion.LoadingProgress.StartupImpact.Patches;

[HarmonyPatch(typeof(LoadedModManager), nameof(LoadedModManager.ApplyPatches))]
[HarmonyPatchCategory("StartupImpact")]
internal static class LoadedModManager_ApplyPatches_Patches
{
    private static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator
    ) =>
        TranspilerHelper.ProfileCurrentMoveNext(
            instructions,
            generator,
            "LoadedModManager.ApplyPatches",
            i => i.Calls(
                AccessTools.PropertyGetter(
                    typeof(IEnumerator<PatchOperation>),
                    nameof(IEnumerator.Current)
                )
            ),
            2,
            i => i.Calls(
                AccessTools.Method(typeof(IEnumerator), nameof(IEnumerator.MoveNext))
            ),
            [
                new(OpCodes.Ldloc_1),
                new(
                    OpCodes.Call,
                    AccessTools.Method(
                        typeof(LoadedModManager_ApplyPatches_Patches),
                        nameof(BeforeApplyPatches)
                    )
                ),
            ],
            [
                new(OpCodes.Ldloc_1),
                new(
                    OpCodes.Call,
                    AccessTools.Method(
                        typeof(LoadedModManager_ApplyPatches_Patches),
                        nameof(AfterApplyPatches)
                    )
                ),
            ]
        );

    private static void BeforeApplyPatches(PatchOperation patchOperation)
    {
        if (patchOperation == null)
        {
            return;
        }

        if (
            !ModContentPack_LoadPatches_Patches.modContentPackTable.TryGetValue(
                patchOperation,
                out var modContentPack
            )
        )
        {
            LoadingProgressMod.Error(
                "LoadedModManager.BeforeApplyPatches: Could not find mod content pack for "
                    + patchOperation.ToString()
            );
            return;
        }
        StartupImpactProfilerUtil.StartModProfiler(
            modContentPack,
            "LoadingProgress.StartupImpact.ApplyPatches"
        );
    }

    private static void AfterApplyPatches(PatchOperation patchOperation)
    {
        if (patchOperation == null)
        {
            return;
        }

        if (
            !ModContentPack_LoadPatches_Patches.modContentPackTable.TryGetValue(
                patchOperation,
                out var modContentPack
            )
        )
        {
            LoadingProgressMod.Error(
                "LoadedModManager.AfterApplyPatches: Could not find mod content pack for "
                    + patchOperation.ToString()
            );
            return;
        }
        StartupImpactProfilerUtil.StopModProfiler(
            modContentPack,
            "LoadingProgress.StartupImpact.ApplyPatches"
        );
    }
}
