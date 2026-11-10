using UnityEngine;

namespace TinykinHotCold.Radar
{
    /// <summary>How much of one target type is found in the current level, from the game's own counters.</summary>
    internal struct FoundProgress
    {
        /// <summary>
        /// Share of a level's pollen or tinykin that must be found before the radar unlocks there.
        /// Fixed on purpose: the radar is for the last few, not for the whole level.
        /// </summary>
        internal const float UnlockFraction = 0.9f;

        internal FoundProgress(int found, int total)
        {
            Found = found;
            Total = total;
        }

        internal int Found { get; }

        internal int Total { get; }

        internal int Remaining => Mathf.Max(0, Total - Found);

        internal bool HasAny => Total > 0;

        internal bool IsComplete => Remaining == 0;

        internal float Fraction => Total > 0 ? (float)Found / Total : 1f;

        internal bool IsUnlocked => Total > 0 && Fraction >= UnlockFraction;

        internal int Percent => Mathf.FloorToInt(Fraction * 100f);

        internal static FoundProgress For(TargetType type)
        {
            SceneData level = LevelDescription.INSTANCE.sceneData;
            return type == TargetType.Pollen
                ? new FoundProgress(Game.INSTANCE.pollenCount.Value, level.GetAllPollenCount())
                : new FoundProgress(Game.PLAYER.NewlyFoundSbirzCount, level.TotalSbirzCount);
        }
    }
}
