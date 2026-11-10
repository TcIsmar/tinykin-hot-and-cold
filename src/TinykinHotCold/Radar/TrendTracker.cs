using System.Collections.Generic;
using UnityEngine;

namespace TinykinHotCold.Radar
{
    /// <summary>Compares the current distance with about a second ago to say "warmer" or "colder".</summary>
    internal sealed class TrendTracker
    {
        private const float Window = 1f;
        private const float HoldTime = 0.6f;

        private readonly Queue<Sample> samples = new Queue<Sample>();
        private int targetId;
        private Trend shown;
        private float shownUntil;

        internal void Reset()
        {
            samples.Clear();
            shown = Trend.Steady;
            shownUntil = 0f;
        }

        internal Trend Update(float time, float distance, int nearestTargetId)
        {
            if (nearestTargetId != targetId)
            {
                // Another item became the nearest (or the old one was just found): start over,
                // so finding something never reads as "colder".
                targetId = nearestTargetId;
                Reset();
            }

            samples.Enqueue(new Sample { Time = time, Distance = distance });
            while (time - samples.Peek().Time > Window)
            {
                samples.Dequeue();
            }

            Sample oldest = samples.Peek();
            Trend current = Trend.Steady;
            if (time - oldest.Time >= Window * 0.5f)
            {
                float change = distance - oldest.Distance;
                float threshold = Mathf.Max(0.3f, distance * 0.03f);
                if (change < -threshold)
                {
                    current = Trend.Warmer;
                }
                else if (change > threshold)
                {
                    current = Trend.Colder;
                }
            }

            if (current != Trend.Steady)
            {
                shown = current;
                shownUntil = time + HoldTime;
            }
            else if (time > shownUntil)
            {
                shown = Trend.Steady;
            }

            return shown;
        }

        private struct Sample
        {
            public float Time;
            public float Distance;
        }
    }
}
