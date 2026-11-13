using UnityEngine;

namespace TinykinHotCold.Radar
{
    /// <summary>
    /// Turns the distance to the nearest missing item into hot/cold feedback. Distances are in world
    /// units; the Living Room is about 156 x 56 x 118 units. Fixed on purpose: hint strength is not
    /// configurable.
    /// </summary>
    internal static class HeatModel
    {
        // Upper distance limits for Burning, Hot, Toasty, Warm, Cool, Chilly and Cold; anything further is
        // Freezing. Each step is about 1.6 times closer than the one before it, from 75 units down to 4.
        private static readonly float[] UpperLimits = { 4f, 6.5f, 10.5f, 17f, 28f, 46f, 75f };

        // How far past a band edge you must move before the level changes, so it doesn't flicker.
        private const float Hysteresis = 0.75f;

        private const float NearDistance = 2f;
        private const float FarDistance = 100f;
        private const float SlowestTick = 2.5f;
        private const float FastestTick = 0.12f;

        internal static HeatLevel LevelFor(float distance)
        {
            for (int i = 0; i < UpperLimits.Length; i++)
            {
                if (distance < UpperLimits[i])
                {
                    return (HeatLevel)((int)HeatLevel.Burning - i);
                }
            }

            return HeatLevel.Freezing;
        }

        /// <summary>Like <see cref="LevelFor(float)"/>, but keeps the previous level near a band edge.</summary>
        internal static HeatLevel LevelFor(float distance, HeatLevel previous)
        {
            HeatLevel hotter = LevelFor(Mathf.Max(0f, distance - Hysteresis));
            HeatLevel colder = LevelFor(distance + Hysteresis);
            return previous <= hotter && previous >= colder ? previous : LevelFor(distance);
        }

        /// <summary>Seconds between ticks (sound, pulse, rumble): slow when far away, fast when close.</summary>
        internal static float TickInterval(float distance)
        {
            float d = Mathf.Clamp(distance, NearDistance, FarDistance);
            float heat = 1f - Mathf.Log(d / NearDistance) / Mathf.Log(FarDistance / NearDistance);
            return Mathf.Lerp(SlowestTick, FastestTick, heat);
        }
    }
}
