using System.Reflection.Emit;

namespace ru.pyxiion.modrim.LoadingProgress.StartupImpact;

internal static class TranspilerHelper
{
    internal static IEnumerable<CodeInstruction> ProfileCurrentMoveNext(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator,
        string errorPrefix,
        Func<CodeInstruction, bool> currentPredicate,
        int advanceAfterCurrent,
        Func<CodeInstruction, bool> moveNextPredicate,
        CodeInstruction[] beforeInstructions,
        CodeInstruction[] afterInstructions)
    {
        var original = instructions.ToList();
        var codeMatcher = new CodeMatcher(original, generator);

        _ = codeMatcher.SearchForward(currentPredicate);
        if (codeMatcher.IsInvalid)
        {
            LoadingProgressMod.Error(
                $"{errorPrefix}: Could not find a call to IEnumerator.Current."
            );
            return original;
        }

        _ = codeMatcher
            .Advance(advanceAfterCurrent)
            .InsertAndAdvance(beforeInstructions);

        _ = codeMatcher.SearchForward(moveNextPredicate);
        if (codeMatcher.IsInvalid)
        {
            LoadingProgressMod.Error(
                $"{errorPrefix}: Could not find a call to IEnumerator.MoveNext."
            );
            return original;
        }

        _ = codeMatcher
            .Advance(1)
            .InsertAndAdvance(afterInstructions);

        return codeMatcher.Instructions();
    }
}
