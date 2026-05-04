namespace ilyvion.LoadingProgress;

internal sealed class Settings : ModSettings
{
    private bool _patchInitialization = true;
    public bool PatchInitialization
    {
        get => _patchInitialization;
        set => _patchInitialization = value;
    }

    private bool _patchReloadContent = true;
    public bool PatchReloadContent
    {
        get => _patchReloadContent;
        set => _patchReloadContent = value;
    }

    private LoadingWindowPlacement _loadingWindowPlacement = LoadingWindowPlacement.Middle;
    public LoadingWindowPlacement LoadingWindowPlacement
    {
        get => _loadingWindowPlacement;
        set => _loadingWindowPlacement = value;
    }

    private float _lastLoadingTime = -1f;
    public float LastLoadingTime
    {
        get => _lastLoadingTime;
        set => _lastLoadingTime = value;
    }

    private int _lastLoadingModHash = -1;
    public int LastLoadingModHash
    {
        get => _lastLoadingModHash;
        set => _lastLoadingModHash = value;
    }

    private List<float> _loadingTimes = [];
    public List<float> LoadingTimes => _loadingTimes;

    public int LoadingTimeSampleCount => Math.Min(_loadingTimes.Count, _loadingTimesCapacity);

    private int _loadingTimesCapacity = 10;
    public int LoadingTimesCapacity
    {
        get => _loadingTimesCapacity;
        set => _loadingTimesCapacity = value;
    }

    private bool _clearEstimatesOnModListChange = true;
    public bool ClearEstimatesOnModListChange
    {
        get => _clearEstimatesOnModListChange;
        set => _clearEstimatesOnModListChange = value;
    }

    // The 10 most-recent entries get decreasing weights 10→1; everything older gets weight 1.
    // This means old entries are never squeezed out no matter how large the history grows.
    private const int WeightSpread = 10;

    public float? AverageLoadingTime
    {
        get
        {
            if (_loadingTimes.Count == 0)
            {
                return null;
            }
            var weightedSum = 0f;
            var totalWeight = 0f;
            var count = _loadingTimes.Count;
            var startIdx = Math.Max(count - _loadingTimesCapacity, 0);
            for (var i = startIdx; i < count; i++)
            {
                var distFromNewest = count - 1 - i;
                var weight = Math.Max(WeightSpread - distFromNewest, 1f);
                weightedSum += _loadingTimes[i] * weight;
                totalWeight += weight;
            }
            return weightedSum / totalWeight;
        }
    }

    private bool _showLastLoadingTime = true;
    public bool ShowLastLoadingTime
    {
        get => _showLastLoadingTime;
        set => _showLastLoadingTime = value;
    }

    private bool _showLoadingTimeAsCountDown;
    public bool ShowLoadingTimeAsCountDown
    {
        get => _showLoadingTimeAsCountDown;
        set => _showLoadingTimeAsCountDown = value;
    }

    private bool _showLastLoadingTimeProgressBar = true;
    public bool ShowLastLoadingTimeProgressBar
    {
        get => _showLastLoadingTimeProgressBar;
        set => _showLastLoadingTimeProgressBar = value;
    }

    private bool _showLastLoadingTimeInCorner = true;
    public bool ShowLastLoadingTimeInCorner
    {
        get => _showLastLoadingTimeInCorner;
        set => _showLastLoadingTimeInCorner = value;
    }

    private bool _showFasterGameLoadingEarlyModContentLoading = true;
    public bool ShowFasterGameLoadingEarlyModContentLoading
    {
        get => _showFasterGameLoadingEarlyModContentLoading;
        set => _showFasterGameLoadingEarlyModContentLoading = value;
    }

    private bool _trackStartupLoadingImpact;
    public bool TrackStartupLoadingImpact
    {
        get => _trackStartupLoadingImpact;
        set => _trackStartupLoadingImpact = value;
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(ref _patchInitialization, "patchInitialization", true);
        Scribe_Values.Look(ref _patchReloadContent, "patchReloadContent", true);
        Scribe_Values.Look(
            ref _loadingWindowPlacement,
            "loadingWindowPlacement",
            LoadingWindowPlacement.Middle
        );
        Scribe_Values.Look(ref _lastLoadingTime, "lastLoadingTime", -1f);
        Scribe_Values.Look(ref _lastLoadingModHash, "lastLoadingModHash", -1);
        Scribe_Collections.Look(ref _loadingTimes, "loadingTimes", LookMode.Value);
        _loadingTimes ??= [];
        Scribe_Values.Look(ref _loadingTimesCapacity, "loadingTimesCapacity", 10);
        Scribe_Values.Look(
            ref _clearEstimatesOnModListChange,
            "clearEstimatesOnModListChange",
            true
        );

        if (
            Scribe.mode == LoadSaveMode.LoadingVars
            && _lastLoadingTime > 0
            && _loadingTimes.Count == 0
        )
        {
            // Migrate from old versions where only the last loading time was saved,
            // to the new system where a history of loading times is saved.
            // Put the last loading time into the history so it isn't lost,
            // and so it can be used in the average loading time calculation.
            _loadingTimes.Add(_lastLoadingTime);
            _lastLoadingTime = -1f;
        }
        Scribe_Values.Look(ref _showLastLoadingTime, "showLastLoadingTime", true);
        Scribe_Values.Look(ref _showLoadingTimeAsCountDown, "showLoadingTimeAsCountDown", false);
        Scribe_Values.Look(
            ref _showLastLoadingTimeProgressBar,
            "showLastLoadingTimeProgressBar",
            true
        );
        Scribe_Values.Look(ref _showLastLoadingTimeInCorner, "showLastLoadingTimeInCorner", true);
        Scribe_Values.Look(
            ref _showFasterGameLoadingEarlyModContentLoading,
            "showFasterGameLoadingEarlyModContentLoading",
            true
        );
        Scribe_Values.Look(ref _trackStartupLoadingImpact, "trackStartupLoadingImpact", false);
    }

    public void DoSettingsWindowContents(Rect inRect)
    {
        Listing_Standard listingStandard = new();
        listingStandard.Begin(inRect);

        listingStandard.CheckboxLabeled(
            "LoadingProgress.PatchInitialization".Translate(),
            ref _patchInitialization,
            "LoadingProgress.PatchInitialization.Tip".Translate()
        );

        listingStandard.CheckboxLabeled(
            "LoadingProgress.PatchReloadContent".Translate(),
            ref _patchReloadContent,
            "LoadingProgress.PatchReloadContent.Tip".Translate()
        );

        listingStandard.CheckboxLabeled(
            "LoadingProgress.LastLoadingTime".Translate(),
            ref _showLastLoadingTime,
            "LoadingProgress.LastLoadingTime.Tip".Translate()
        );

        _loadingTimesCapacity = (int)
            listingStandard.SliderLabeled(
                "LoadingProgress.LoadingTimesCapacity".Translate(_loadingTimesCapacity),
                _loadingTimesCapacity,
                1f,
                50f,
                tooltip: "LoadingProgress.LoadingTimesCapacity.Tip".Translate()
            );

        listingStandard.CheckboxLabeled(
            "LoadingProgress.ClearEstimatesOnModListChange".Translate(),
            ref _clearEstimatesOnModListChange,
            "LoadingProgress.ClearEstimatesOnModListChange.Tip".Translate()
        );

        listingStandard.CheckboxLabeled(
            "LoadingProgress.LoadingTimeAsCountDown".Translate(),
            ref _showLoadingTimeAsCountDown,
            "LoadingProgress.LoadingTimeAsCountDown.Tip".Translate()
        );

        listingStandard.CheckboxLabeled(
            "LoadingProgress.LastLoadingTimeProgressBar".Translate(),
            ref _showLastLoadingTimeProgressBar,
            "LoadingProgress.LastLoadingTimeProgressBar.Tip".Translate()
        );

        listingStandard.CheckboxLabeled(
            "LoadingProgress.LastLoadingTimeInCorner".Translate(),
            ref _showLastLoadingTimeInCorner,
            "LoadingProgress.LastLoadingTimeInCorner.Tip".Translate()
        );

        listingStandard.CheckboxLabeled(
            "LoadingProgress.ShowFasterGameLoadingEarlyModContentLoading".Translate(),
            ref _showFasterGameLoadingEarlyModContentLoading,
            "LoadingProgress.ShowFasterGameLoadingEarlyModContentLoading.Tip".Translate()
        );

        listingStandard.CheckboxLabeled(
            "LoadingProgress.TrackStartupLoadingImpact".Translate(),
            ref _trackStartupLoadingImpact,
            "LoadingProgress.TrackStartupLoadingImpact.Tip".Translate()
        );

        if (
            listingStandard.ButtonTextLabeledPct(
                "LoadingProgress.LoadingWindowPlacement".Translate(),
                $"LoadingProgress.{_loadingWindowPlacement}".Translate(),
                0.6f,
                TextAnchor.MiddleLeft
            )
        )
        {
            List<FloatMenuOption> list =
            [
                new FloatMenuOption(
                    "LoadingProgress.Top".Translate(),
                    () => _loadingWindowPlacement = LoadingWindowPlacement.Top
                ),
                new FloatMenuOption(
                    "LoadingProgress.Middle".Translate(),
                    () => _loadingWindowPlacement = LoadingWindowPlacement.Middle
                ),
                new FloatMenuOption(
                    "LoadingProgress.Bottom".Translate(),
                    () => _loadingWindowPlacement = LoadingWindowPlacement.Bottom
                ),
                // new FloatMenuOption(
                //     "LoadingProgress.Custom".Translate(),
                //     () => _loadingWindowPlacement = LoadingWindowPlacement.Custom)
            ];
            Find.WindowStack.Add(new FloatMenu(list));
        }

        listingStandard.End();
    }
}

internal enum LoadingWindowPlacement
{
    Top,
    Middle,
    Bottom,
    Custom,
}
