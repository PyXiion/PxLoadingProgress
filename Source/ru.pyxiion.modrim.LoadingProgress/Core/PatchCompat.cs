using System.Text;

namespace ru.pyxiion.modrim.LoadingProgress;

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
        var patches = Harmony.GetPatchInfo(method);
        if (patches == null)
        {
            return;
        }

        HashSet<Assembly> ignoredAssemblySet =
        [
            Assembly.GetExecutingAssembly(),
            .. ignoredAssemblies ?? [],
        ];
        HashSet<MethodBase> ignoredMethodsSet = [.. ignoredMethods ?? []];

        (PatchKinds kind, IEnumerable<Patch> patches, string label)[] patchGroups =
        [
            (PatchKinds.Prefix, patches.Prefixes, "prefixes"),
            (PatchKinds.Transpiler, patches.Transpilers, "transpilers"),
            (PatchKinds.Postfix, patches.Postfixes, "postfixes"),
            (PatchKinds.Finalizer, patches.Finalizers, "finalizers"),
        ];

        var problematic = patchGroups
            .Where(group => (warnKinds & group.kind) != 0)
            .Select(group =>
                (
                    group.label,
                    methods: group
                        .patches.Select(patch => patch.PatchMethod)
                        .Where(patchMethod =>
                            !ignoredAssemblySet.Contains(patchMethod.DeclaringType.Assembly)
                            && !ignoredMethodsSet.Contains(patchMethod)
                        )
                        .ToList()
                )
            )
            .Where(group => group.methods.Count > 0)
            .ToList();
        if (problematic.Count == 0)
        {
            return;
        }

        var sb = new StringBuilder();
        _ = sb.Append("These patches may not work as expected because ")
            .Append($"Loading Progress replaces {method.DeclaringType}:{method}.\n");

        if (stillCallsOriginal)
        {
            _ = sb.Append("Note: The original method is still called; unless patches are ");
            _ = sb.Append("extremely timing-sensitive, they should still work.\n");
        }

        foreach (var (label, methods) in problematic)
        {
            _ = sb.Append($"Potentially problematic {label} ")
                .Append($"({methods.Count}):\n  - ")
                .Append(string.Join("\n  - ", methods.Select(m => $"{m.DeclaringType}:{m}")))
                .Append('\n');
        }

        LoadingProgressMod.Warning(sb.ToString().TrimEnd());
    }
}
