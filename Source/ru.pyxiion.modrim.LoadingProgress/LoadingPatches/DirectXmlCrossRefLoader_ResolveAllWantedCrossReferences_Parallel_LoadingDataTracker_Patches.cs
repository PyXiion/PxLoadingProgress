using System.Reflection.Emit;

namespace ru.pyxiion.modrim.LoadingProgress;

[HarmonyPatch]
internal static class DirectXmlCrossRefLoader_ResolveAllWantedCrossReferences_Parallel_LoadingDataTracker_Patches
{
    internal static bool Prepare()
    {
        var methods = WantedRef_TryResolveFinder.FindMethod();

        if (methods.Count() == 1)
        {
            return true;
        }
        else
        {
            LoadingProgressMod.Error(
                "Could not patch DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences, could not locate call to DirectXmlCrossRefLoader+WantedRef.TryResolve."
            );
            return false;
        }
    }

    internal static IEnumerable<MethodBase> TargetMethods() =>
        WantedRef_TryResolveFinder.FindMethod();

    internal static void Postfix() =>
        _ = Interlocked.Increment(ref LoadingDataTracker.WantedRefTryResolveCount);

    private static class WantedRef_TryResolveFinder
    {
        private static readonly MethodInfo _method_WantedRef_TryResolve = AccessTools.Method(
            typeof(DirectXmlCrossRefLoader.WantedRef),
            nameof(DirectXmlCrossRefLoader.WantedRef.TryResolve)
        );

        private static readonly CodeMatch[] toMatch =
        [
            new(OpCodes.Callvirt, _method_WantedRef_TryResolve),
        ];

        public static IEnumerable<MethodInfo> FindMethod()
        {
            var candidates = Utilities.FindInTypeAndInnerTypeMethods(
                typeof(DirectXmlCrossRefLoader),
                m => !m.IsGenericMethod && !m.IsAbstract && !m.DeclaringType.IsGenericType
            );

            foreach (var method in candidates)
            {
                List<CodeInstruction> instructions;
                try
                {
                    instructions = PatchProcessor.GetCurrentInstructions(method);
                }
                catch (Exception ex)
                {
                    LoadingProgressMod.Error(
                        $"Error while processing method {method}({method.FullDescription()}): {ex}"
                    );
                    continue;
                }
                var matched = instructions.Matches(toMatch);
                if (matched)
                {
                    yield return method;
                }
            }
            yield break;
        }
    }
}
