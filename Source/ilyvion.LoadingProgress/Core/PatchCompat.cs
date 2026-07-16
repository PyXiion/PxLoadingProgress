using System.Text;

namespace ilyvion.LoadingProgress;

[Flags]
internal enum PatchKinds
{
    None = 0,
    Prefix = 1 << 0,
    Transpiler = 1 << 1,
    Postfix = 1 << 2,
    Finalizer = 1 << 3,
    All = Prefix | Transpiler | Postfix | Finalizer,
}

internal static class PatchCompat
{
    public static void WarnAboutPatches(
        MethodBase method,
        bool stillCallsOriginal,
        Assembly[]? ignoredAssemblies = null,
        MethodBase[]? ignoredMethods = null,
        PatchKinds warnKinds = PatchKinds.All
    )
    {
        HashSet<Assembly> ignoredAssemblySet =
        [
            Assembly.GetExecutingAssembly(),
            .. ignoredAssemblies ?? [],
        ];
        HashSet<MethodBase> ignoredMethodsSet = [.. ignoredMethods ?? []];

        var patches = Harmony.GetPatchInfo(method);
        if (patches != null)
        {
            var potentiallyProblematicPrefixes = CollectPotentiallyProblematicPatches(
                warnKinds,
                PatchKinds.Prefix,
                patches.Prefixes,
                ignoredAssemblySet,
                ignoredMethodsSet
            );

            var potentiallyProblematicTranspilers = CollectPotentiallyProblematicPatches(
                warnKinds,
                PatchKinds.Transpiler,
                patches.Transpilers,
                ignoredAssemblySet,
                ignoredMethodsSet
            );

            var potentiallyProblematicPostfixes = CollectPotentiallyProblematicPatches(
                warnKinds,
                PatchKinds.Postfix,
                patches.Postfixes,
                ignoredAssemblySet,
                ignoredMethodsSet
            );

            var potentiallyProblematicFinalizers = CollectPotentiallyProblematicPatches(
                warnKinds,
                PatchKinds.Finalizer,
                patches.Finalizers,
                ignoredAssemblySet,
                ignoredMethodsSet
            );

            var totalCount =
                (potentiallyProblematicPrefixes?.Count ?? 0)
                + (potentiallyProblematicTranspilers?.Count ?? 0)
                + (potentiallyProblematicPostfixes?.Count ?? 0)
                + (potentiallyProblematicFinalizers?.Count ?? 0);
            if (totalCount > 0)
            {
                var sb = new StringBuilder();
                _ = sb.Append("These patches may not work as expected because ")
                    .Append($"Loading Progress replaces {method.DeclaringType}:{method}.\n");

                if (stillCallsOriginal)
                {
                    _ = sb.Append("Note: The original method is still called; unless patches are ");
                    _ = sb.Append("extremely timing-sensitive, they should still work.\n");
                }

                AppendPatchWarning(
                    sb,
                    warnKinds,
                    PatchKinds.Prefix,
                    potentiallyProblematicPrefixes,
                    "prefixes"
                );
                AppendPatchWarning(
                    sb,
                    warnKinds,
                    PatchKinds.Transpiler,
                    potentiallyProblematicTranspilers,
                    "transpilers"
                );
                AppendPatchWarning(
                    sb,
                    warnKinds,
                    PatchKinds.Postfix,
                    potentiallyProblematicPostfixes,
                    "postfixes"
                );
                AppendPatchWarning(
                    sb,
                    warnKinds,
                    PatchKinds.Finalizer,
                    potentiallyProblematicFinalizers,
                    "finalizers"
                );

                LoadingProgressMod.Warning(sb.ToString().TrimEnd());
            }
        }
    }

    private static void AppendPatchWarning(
        StringBuilder sb,
        PatchKinds warnFlags,
        PatchKinds warnCheckedFlag,
        List<MethodInfo>? methods,
        string label
    )
    {
        if ((warnFlags & warnCheckedFlag) != 0 && methods != null && methods.Count > 0)
        {
            _ = sb.Append($"Potentially problematic {label} ");
            _ = sb.Append($"({methods.Count}):\n  - ")
                .Append(string.Join("\n  - ", methods.Select(m => $"{m.DeclaringType}:{m}")))
                .Append('\n');
        }
    }

    private static List<MethodInfo>? CollectPotentiallyProblematicPatches(
        PatchKinds warnFlags,
        PatchKinds warnCheckedFlag,
        IEnumerable<Patch> patchesEnumerable,
        HashSet<Assembly> ignoredAssemblySet,
        HashSet<MethodBase> ignoredMethodsSet
    )
    {
        List<MethodInfo>? potentiallyProblematicPatches = null;
        if ((warnFlags & warnCheckedFlag) != 0)
        {
            potentiallyProblematicPatches = [];
            foreach (var patch in patchesEnumerable)
            {
                if (
                    ignoredAssemblySet.Contains(patch.PatchMethod.DeclaringType.Assembly)
                    || ignoredMethodsSet.Contains(patch.PatchMethod)
                )
                {
                    continue;
                }
                potentiallyProblematicPatches.Add(patch.PatchMethod);
            }
        }

        return potentiallyProblematicPatches;
    }
}
