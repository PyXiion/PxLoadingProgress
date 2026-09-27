using System.Reflection.Emit;

namespace ru.pyxiion.modrim.LoadingProgress.StartupImpact.Patches;

[HarmonyPatch]
[HarmonyPatchCategory("StartupImpact")]
internal static class DefDatabase_AddAllInMods_Patches
{
    private static readonly MethodInfo _method_GenGeneric_InvokeStaticMethodOnGenericType =
        AccessTools.Method(
            typeof(GenGeneric),
            nameof(GenGeneric.InvokeStaticMethodOnGenericType),
            [typeof(Type), typeof(string), typeof(string)]
        );

    private static readonly CodeMatch[] toMatch =
    [
        new(OpCodes.Ldstr, "AddAllInMods"),
        new(OpCodes.Call, _method_GenGeneric_InvokeStaticMethodOnGenericType),
    ];

    // Harmony calls Prepare and TargetMethods separately; scan the IL only once.
    private static readonly Lazy<List<MethodBase>> _targetMethods = new(() =>
    {
        var methods = Utilities
            .FindMethodsDoing(typeof(PlayDataLoader), toMatch)
            .Cast<MethodBase>()
            .ToList();
        if (methods.Count != 1)
        {
            LoadingProgressMod.Error(
                "Could not find call to GenGeneric.InvokeStaticMethodOnGenericType in PlayDataLoader"
            );
            return [];
        }
        return methods;
    });

    internal static bool Prepare() => _targetMethods.Value.Count == 1;

    internal static IEnumerable<MethodBase> TargetMethods() => _targetMethods.Value;

    internal static void Prefix() =>
        StartupImpactProfilerUtil.StartBaseGameProfiler(
            "LoadingProgress.StartupImpact.DefDatabaseAddAllInMods"
        );

    internal static void Postfix() =>
        StartupImpactProfilerUtil.StopBaseGameProfiler(
            "LoadingProgress.StartupImpact.DefDatabaseAddAllInMods"
        );
}
