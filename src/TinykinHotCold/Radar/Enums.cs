namespace TinykinHotCold.Radar
{
    /// <summary>What a radar target is, for the log. The panel never says which kind it is leading to.</summary>
    internal enum TargetKind
    {
        Pollen,
        Tinykin,
        Collectible,
        QuestStep,
    }

    internal enum HeatLevel
    {
        Freezing,
        Cold,
        Chilly,
        Cool,
        Warm,
        Toasty,
        Hot,
        Burning,
    }

    internal enum HudAnchor
    {
        TopLeft,
        TopCenter,
        TopRight,
        BottomLeft,
        BottomCenter,
        BottomRight,
    }
}
