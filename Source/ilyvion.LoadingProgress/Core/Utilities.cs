using System.Collections.ObjectModel;
using System.Text;

namespace ilyvion.LoadingProgress;

internal static class Utilities
{
    public static void LongEventHandlerPrependQueue(
        Action prependAction,
        string keepPrefix = "LoadingProgress."
    )
    {
        var keepEvents = LongEventHandler
            .eventQueue.Where(e =>
                e.eventTextKey != null
                && e.eventTextKey.StartsWith(keepPrefix, StringComparison.Ordinal)
            )
            .ToList();
        var queue = LongEventHandler
            .eventQueue.Where(e =>
                e.eventTextKey == null
                || !e.eventTextKey.StartsWith(keepPrefix, StringComparison.Ordinal)
            )
            .ToList();
        LongEventHandler.eventQueue.Clear();

        foreach (var kept in keepEvents)
        {
            LongEventHandler.eventQueue.Enqueue(kept);
        }

        prependAction();

        foreach (var queuedEvent in queue)
        {
            LongEventHandler.eventQueue.Enqueue(queuedEvent);
        }
    }

    public static string FormatDuration(TimeSpan t)
    {
        if (t < TimeSpan.Zero)
        {
            var abs = -t;
            return abs.TotalHours >= 1 ? $"-{(int)abs.TotalHours}:{abs:mm\\:ss}" : $"{t:mm\\:ss}";
        }
        return t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t:mm\\:ss}" : $"{t:mm\\:ss}";
    }

    public static Color Darken(this Color color, float amount)
    {
        Color.RGBToHSV(color, out var h, out var s, out var v);
        v = Mathf.Max(0, v - amount); // reduce lightness
        return Color.HSVToRGB(h, s, v);
    }

    public static Color Brighten(this Color color, float amount)
    {
        Color.RGBToHSV(color, out var h, out var s, out var v);
        v = Mathf.Min(1, v + amount); // increase lightness
        return Color.HSVToRGB(h, s, v);
    }

    public static string ClampTextWithEllipsisMarkupAware(Rect rect, string text)
    {
        if (text.Length <= 4)
        {
            return text;
        }

        if (Text.CalcSize(text).x <= rect.width - 13f)
        {
            return text;
        }

        var output = new StringBuilder();
        var stack = new Stack<string>();
        var visibleChars = 0;

        // forward pass to capture tag info
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '<')
            {
                var closing = text.IndexOf('>', i);
                if (closing == -1)
                {
                    break;
                }

                var tag = text.Substring(i, closing - i + 1);
                _ = output.Append(tag);

                if (!tag.StartsWith("</", StringComparison.Ordinal))
                {
                    var spaceIdx = tag.IndexOf(' ', StringComparison.Ordinal);
                    var tagNameEnd = spaceIdx != -1 ? spaceIdx : tag.Length - 1;
                    stack.Push(tag[1..tagNameEnd]);
                }
                else if (stack.Count > 0)
                {
                    _ = stack.Pop();
                }
                i = closing;
            }
            else
            {
                _ = output.Append(text[i]);
                visibleChars++;
                if (Text.CalcSize(output.ToString() + "...").x > rect.width - 13f)
                {
                    output.Length -= 1; // remove last character
                    break;
                }
            }
        }

        _ = output.Append("...");
        // close tags
        while (stack.Count > 0)
        {
            _ = output.Append("</" + stack.Pop() + ">");
        }

        return output.ToString();
    }

    private static readonly Dictionary<Assembly, ModContentPack?> _modAssemblyCache = [];

    public static ModContentPack? FindModByAssembly(Assembly assembly)
    {
        if (_modAssemblyCache.TryGetValue(assembly, out var modContentPack))
        {
            return modContentPack;
        }

        modContentPack = (
            from modpack in LoadedModManager.RunningMods
            where modpack.assemblies.loadedAssemblies.Contains(assembly)
            select modpack
        ).FirstOrDefault();

        _modAssemblyCache[assembly] = modContentPack;
        return modContentPack;
    }

    public static HashSet<MethodInfo> FindInTypeAndInnerTypeMethods(
        Type type,
        Func<MethodInfo, bool>? predicate = null
    )
    {
        predicate ??= _ => true;

        // Find all possible candidates, both from the wrapping type and all nested types.
        var candidates = AccessTools.GetDeclaredMethods(type).Where(predicate).ToHashSet();
        candidates.AddRange(
            type.GetNestedTypes(AccessTools.all)
                .SelectMany(AccessTools.GetDeclaredMethods)
                .Where(predicate)
        );

        return candidates;
    }

    public static IEnumerable<MethodInfo> FindMethodsDoing(Type containingType, CodeMatch[] toMatch)
    {
        // Find all possible candidates, both from the wrapping type and all nested types.
        var candidates = FindInTypeAndInnerTypeMethods(containingType, m => !m.IsGenericMethod);

        //check all candidates for the target instructions, return those that match.
        foreach (var method in candidates)
        {
            var instructions = PatchProcessor.GetCurrentInstructions(method);
            var matched = instructions.Matches(toMatch);
            if (matched)
            {
                yield return method;
            }
        }
        yield break;
    }

    public static void ColorPicker(
        this Listing_Standard listingStandard,
        string labelKey,
        string tipKey,
        Color color,
        Color defaultColor,
        Action<Color> setColor
    )
    {
        var row = listingStandard.GetRect(Text.LineHeight);
        listingStandard.Gap(listingStandard.verticalSpacing);
        var labelRect = row.LeftPart(0.6f);
        var swatchRect = new Rect(
            row.xMax - 50f - 34f, // 34 = 4px gap + 30px reset button width
            row.y,
            50f,
            row.height
        );
        var resetRect = new Rect(swatchRect.xMax + 4f, row.y, 30f, row.height);
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(labelRect, labelKey.Translate());
        Text.Anchor = TextAnchor.UpperLeft;
        TooltipHandler.TipRegion(labelRect, tipKey.Translate());
        Widgets.DrawBoxSolid(swatchRect, color);
        Widgets.DrawBox(swatchRect);
        if (Widgets.ButtonInvisible(swatchRect))
        {
            Find.WindowStack.Add(new ColorPicker.Dialog_ColorPicker(color, setColor));
        }
        TooltipHandler.TipRegion(swatchRect, tipKey.Translate());
        if (Widgets.ButtonText(resetRect, "↺"))
        {
            setColor(defaultColor);
        }
        TooltipHandler.TipRegion(resetRect, "LoadingProgress.Reset".Translate());
    }

    public static ReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary
    )
        where TKey : notnull => new(dictionary);
}
