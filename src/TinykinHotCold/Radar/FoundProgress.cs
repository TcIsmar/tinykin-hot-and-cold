using UnityEngine;

namespace TinykinHotCold.Radar
{
    /// <summary>How much of the pollen or tinykin is found in the current level, from the game's own counters.</summary>
    internal struct FoundProgress
    {
        /// <summary>
        /// Share of a level's pollen and of its tinykin that must both be found before the radar unlocks there.
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

        /// <summary>A level with none of this kind counts as fully found.</summary>
        internal float Fraction => Total > 0 ? (float)Found / Total : 1f;

        internal bool IsUnlocked => Fraction >= UnlockFraction;

        internal int Percent => Mathf.FloorToInt(Fraction * 100f);

        internal static FoundProgress Pollen() =>
            new FoundProgress(Game.INSTANCE.pollenCount.Value, LevelDescription.INSTANCE.sceneData.GetAllPollenCount());

        internal static FoundProgress Tinykin() =>
            new FoundProgress(Game.PLAYER.NewlyFoundSbirzCount, LevelDescription.INSTANCE.sceneData.TotalSbirzCount);
    }
}
