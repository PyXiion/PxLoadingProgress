using System.Diagnostics;

namespace ru.pyxiion.modrim.LoadingProgress.StartupImpact;

/// <summary>
/// Lightweight nested-category profiler. Every thread gets its own state, so the hot path
/// (Start/Stop) takes no locks and does no allocations once a category has been seen; time is
/// kept as raw <see cref="Stopwatch"/> ticks and only converted to milliseconds for reports.
/// </summary>
internal sealed class Profiler(string measurementTarget) : IDisposable
{
    private static readonly double MsPerTick = 1000.0 / Stopwatch.Frequency;

    /// <summary>
    /// Global on/off switch, set once from the settings when startup impact tracking starts.
    /// </summary>
    internal static bool Enabled;

    private readonly string _measurementTarget = measurementTarget;

    private readonly ThreadLocal<ThreadState> _threadState = new(() => new(), true);

    private sealed class Accumulator
    {
        public long Ticks;
    }

    private sealed class ThreadState
    {
        public readonly Dictionary<string, Accumulator> OnThread = [];
        public readonly Dictionary<string, Accumulator> OffThread = [];
        public long OnThreadTicks;
        public long OffThreadTicks;

        public string[] Stack = new string[4];
        public int Depth;
        public long SegmentStart;

        // One-entry cache; the same category literal tends to be hit over and over.
        public string? CachedCategory;
        public bool CachedIsOnThread;
        public Accumulator? CachedAccumulator;
    }

    public static float TicksToMs(long ticks) => (float)(ticks * MsPerTick);

    public void Start(string category)
    {
        if (!Enabled || string.IsNullOrEmpty(category))
        {
            return;
        }

        var now = Stopwatch.GetTimestamp();
        var state = _threadState.Value;
        if (state.Depth > 0)
        {
            // Pause the enclosing category.
            Credit(state, state.Stack[state.Depth - 1], now - state.SegmentStart);
        }

        if (state.Depth == state.Stack.Length)
        {
            Array.Resize(ref state.Stack, state.Stack.Length * 2);
        }
        state.Stack[state.Depth++] = category;
        state.SegmentStart = now;
    }

    public float Stop(string category)
    {
        if (!Enabled)
        {
            return 0f;
        }

        var now = Stopwatch.GetTimestamp();
        var state = _threadState.Value;
        if (state.Depth == 0)
        {
            if (category != null)
            {
                Log.Error(
                    $"Stopping {_measurementTarget} profiler for [{category}] while it's already inactive."
                );
            }
            return 0f;
        }

        var actualCategory = state.Stack[--state.Depth];
        state.Stack[state.Depth] = null!;
        if (category != null && !ReferenceEquals(category, actualCategory) && category != actualCategory)
        {
            Log.Error(
                $"Stopping {_measurementTarget} profiler for [expected: {category}] but currently timing [actual: {actualCategory}]"
            );
        }

        var ticks = now - state.SegmentStart;
        Credit(state, actualCategory, ticks);
        // Resume the enclosing category, if any.
        state.SegmentStart = now;
        return TicksToMs(ticks);
    }

    /// <summary>
    /// Adds an externally measured duration to a category on the current thread.
    /// </summary>
    public void AddTicks(string category, long ticks)
    {
        if (!Enabled || ticks <= 0)
        {
            return;
        }

        Credit(_threadState.Value, category, ticks);
    }

    private static void Credit(ThreadState state, string category, long ticks)
    {
        var isOnThread = StartupImpact.IsActiveThread();

        Accumulator? accumulator;
        if (
            ReferenceEquals(category, state.CachedCategory)
            && isOnThread == state.CachedIsOnThread
        )
        {
            accumulator = state.CachedAccumulator!;
        }
        else
        {
            var map = isOnThread ? state.OnThread : state.OffThread;
            if (!map.TryGetValue(category, out accumulator))
            {
                accumulator = new();
                map.Add(category, accumulator);
            }
            state.CachedCategory = category;
            state.CachedIsOnThread = isOnThread;
            state.CachedAccumulator = accumulator;
        }

        accumulator.Ticks += ticks;
        if (isOnThread)
        {
            state.OnThreadTicks += ticks;
        }
        else
        {
            state.OffThreadTicks += ticks;
        }
    }

    public Dictionary<string, float> Metrics => Snapshot(true);
    public float TotalImpact => TicksToMs(_threadState.Values.Sum(s => s.OnThreadTicks));

    public Dictionary<string, float> OffThreadMetrics => Snapshot(false);
    public float OffThreadTotalImpact =>
        TicksToMs(_threadState.Values.Sum(s => s.OffThreadTicks));

    private Dictionary<string, float> Snapshot(bool onThread)
    {
        Dictionary<string, long> ticks = [];
        foreach (var state in _threadState.Values)
        {
            foreach (var entry in onThread ? state.OnThread : state.OffThread)
            {
                _ = ticks.TryGetValue(entry.Key, out var total);
                ticks[entry.Key] = total + entry.Value.Ticks;
            }
        }
        return ticks.ToDictionary(kv => kv.Key, kv => TicksToMs(kv.Value));
    }

    public void Dispose()
    {
        _threadState.Dispose();
        GC.SuppressFinalize(this);
    }
}
