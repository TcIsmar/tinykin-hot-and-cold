using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TinykinHotCold.Radar
{
    /// <summary>
    /// Finds what is still missing in the current level and which of it is nearest to the player.
    /// Pollen: loose pollen and unopened containers. Tinykin: unhatched eggs and unbroken cages.
    /// Only distances leave this class; positions are never shown or logged.
    /// </summary>
    internal sealed class TargetScanner
    {
        private static readonly FieldInfo ContainerFirstIndex =
            typeof(CollectibleContainer).GetField("firstIndex", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly List<Collectible> pollen = new List<Collectible>();
        private readonly List<CollectibleContainer> containers = new List<CollectibleContainer>();
        private readonly List<SbirzEgg> eggs = new List<SbirzEgg>();
        private readonly List<Cage> cages = new List<Cage>();

        internal void Clear()
        {
            pollen.Clear();
            containers.Clear();
            eggs.Clear();
            cages.Clear();
        }

        /// <summary>Collects every candidate object in the loaded level. Cheap enough to repeat every few seconds.</summary>
        internal void Discover()
        {
            Clear();
            AddSceneObjects(pollen);
            AddSceneObjects(containers);
            AddSceneObjects(eggs);
            AddSceneObjects(cages);
        }

        internal Nearest FindNearest(TargetType type, Vector3 from)
        {
            var nearest = new Nearest { Distance = float.PositiveInfinity };
            if (type == TargetType.Pollen)
            {
                foreach (Collectible c in pollen)
                {
                    // Collected pollen gets deactivated. Pollen still inside a container is inactive too;
                    // the container stands in for it below.
                    if (c == null || c.IsCollected || !c.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    Consider(ref nearest, from, c.transform.position, c, hidden: false);
                }

                foreach (CollectibleContainer c in containers)
                {
                    if (c == null || !c.gameObject.activeInHierarchy || RemainingIn(c) <= 0)
                    {
                        continue;
                    }

                    Vector3 position = c.col != null && c.col.enabled ? c.col.bounds.center : c.transform.position;
                    Consider(ref nearest, from, position, c, hidden: true);
                }
            }
            else
            {
                foreach (SbirzEgg e in eggs)
                {
                    if (e == null || e.IsCollected || !e.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    // An egg whose trigger is off can't hatch yet: something in the level has to happen first.
                    bool locked = e.trigger != null && !e.trigger.enabled;
                    Consider(ref nearest, from, e.transform.position, e, hidden: locked);
                }

                foreach (Cage c in cages)
                {
                    if (c == null || c.IsCompleted || !c.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    Consider(ref nearest, from, c.transform.position, c, hidden: true);
                }
            }

            return nearest;
        }

        private static int RemainingIn(CollectibleContainer c) => c.pollenCount - (int)ContainerFirstIndex.GetValue(c);

        private static void Consider(ref Nearest nearest, Vector3 from, Vector3 position, Object target, bool hidden)
        {
            float distance = Vector3.Distance(from, position);
            if (distance < nearest.Distance)
            {
                nearest.Found = true;
                nearest.Distance = distance;
                nearest.Hidden = hidden;
                nearest.TargetId = target.GetInstanceID();
            }
        }

        private static void AddSceneObjects<T>(List<T> into) where T : Component
        {
            foreach (T item in Resources.FindObjectsOfTypeAll<T>())
            {
                // FindObjectsOfTypeAll also returns prefabs and assets; keep only objects in loaded scenes.
                Scene scene = item.gameObject.scene;
                if (scene.IsValid() && scene.isLoaded)
                {
                    into.Add(item);
                }
            }
        }

        internal struct Nearest
        {
            public bool Found;
            public float Distance;

            /// <summary>The item is inside or behind something (a container, a cage, a locked egg).</summary>
            public bool Hidden;

            public int TargetId;
        }
    }
}
