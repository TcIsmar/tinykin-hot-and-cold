#if DEBUG
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TinykinHotCold.Dev
{
    /// <summary>
    /// Phase 1 verification (Debug builds only). Counts everything the radar would treat as
    /// "remaining" and compares it with the game's own counters. Logs counts and distances only,
    /// never positions.
    /// </summary>
    internal static class LevelCensus
    {
        private const float UnlockThreshold = 0.9f;

        private static readonly FieldInfo ContainerFirstIndex =
            typeof(CollectibleContainer).GetField("firstIndex", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static bool IsLevelReady =>
            Game.IsGameStarted && Game.INSTANCE != null && Game.PLAYER != null &&
            LevelDescription.INSTANCE != null && LevelDescription.INSTANCE.sceneData != null;

        internal static int PollenCollected => Game.INSTANCE.pollenCount.Value;

        internal static int TinykinFound => Game.PLAYER.NewlyFoundSbirzCount;

        internal sealed class Result
        {
            public string Level;

            // Pollen: game counters
            public int PollenTotal;
            public int PollenCollected;
            public int PollenRequired;

            // Pollen: census
            public int Loose;
            public int LooseFromContainers;
            public int Collecting;
            public int CollectedObjects;
            public int HeldInContainers;
            public int InactiveNotCollected;
            public int ContainerRemaining;
            public int ContainersWithPollen;
            public int InactiveContainerRemaining;
            public readonly Dictionary<string, int> ContainerKinds = new Dictionary<string, int>();
            public readonly Dictionary<string, int> PollenRemainingByScene = new Dictionary<string, int>();
            public readonly Dictionary<string, int> InactiveNotCollectedByScene = new Dictionary<string, int>();

            // Tinykin: game counters
            public int TinykinTotal;
            public int TinykinFound;

            // Tinykin: census
            public int EggsRegistered;
            public int EggsInScenes;
            public int EggsFound;
            public int EggsRemaining;
            public int EggsLocked;
            public int EggsInactive;
            public int CagesBroken;
            public int CagesUnbroken;
            public int CageTinykinRemaining;
            public readonly Dictionary<string, int> TinykinRemainingByScene = new Dictionary<string, int>();

            // Distances from the player (no directions or positions)
            public float NearestPollen = float.PositiveInfinity;
            public string NearestPollenKind = "none";
            public float NearestTinykin = float.PositiveInfinity;
            public string NearestTinykinKind = "none";

            // Rough level size, for calibrating the hot/cold distance bands
            public Bounds PollenSpread;
            public bool HasSpread;

            public int PollenExpected => PollenTotal - PollenCollected;

            public int PollenCounted => Loose + LooseFromContainers + ContainerRemaining;

            public int TinykinExpected => TinykinTotal - TinykinFound;

            public int TinykinCounted => EggsRemaining + CageTinykinRemaining;
        }

        internal static Result Take()
        {
            var r = new Result();
            SceneData level = LevelDescription.INSTANCE.sceneData;
            r.Level = level.sceneName;
            r.PollenTotal = level.GetAllPollenCount();
            r.PollenCollected = PollenCollected;
            r.PollenRequired = level.requiredPollen;
            r.TinykinTotal = level.TotalSbirzCount;
            r.TinykinFound = TinykinFound;

            Vector3 player = Game.PLAYER.transform.position;
            CountPollen(r, player);
            CountContainers(r, player);
            CountTinykin(r, player);
            return r;
        }

        private static void CountPollen(Result r, Vector3 player)
        {
            foreach (Collectible c in Resources.FindObjectsOfTypeAll<Collectible>())
            {
                if (!IsSceneObject(c))
                {
                    continue;
                }

                GameObject go = c.gameObject;
                if (c.parentScene != null)
                {
                    Encapsulate(r, c.transform.position);
                }

                if (go.activeInHierarchy)
                {
                    if (c.IsCollected)
                    {
                        r.Collecting++;
                        continue;
                    }

                    if (c.parentScene == null)
                    {
                        r.LooseFromContainers++;
                    }
                    else
                    {
                        r.Loose++;
                    }

                    Increment(r.PollenRemainingByScene, go.scene.name, 1);
                    Nearest(ref r.NearestPollen, ref r.NearestPollenKind, player, c.transform.position, "loose pollen");
                }
                else if (c.IsCollected)
                {
                    r.CollectedObjects++;
                }
                else if (HasAncestor<CollectibleContainer>(c.transform))
                {
                    r.HeldInContainers++;
                }
                else
                {
                    r.InactiveNotCollected++;
                    Increment(r.InactiveNotCollectedByScene, go.scene.name, 1);
                }
            }
        }

        private static void CountContainers(Result r, Vector3 player)
        {
            Dictionary<CollectibleContainer, string> owners = MapContainerOwners();
            foreach (CollectibleContainer cc in Resources.FindObjectsOfTypeAll<CollectibleContainer>())
            {
                if (!IsSceneObject(cc))
                {
                    continue;
                }

                int remaining = cc.pollenCount - (int)ContainerFirstIndex.GetValue(cc);
                if (remaining <= 0)
                {
                    continue;
                }

                string kind = owners.TryGetValue(cc, out string owner) ? owner : (cc.selfDestruct ? "touch" : "scripted");
                r.ContainersWithPollen++;
                r.ContainerRemaining += remaining;
                Increment(r.ContainerKinds, kind, remaining);
                Increment(r.PollenRemainingByScene, cc.gameObject.scene.name, remaining);

                if (cc.gameObject.activeInHierarchy)
                {
                    Vector3 position = cc.col != null && cc.col.enabled ? cc.col.bounds.center : cc.transform.position;
                    Nearest(ref r.NearestPollen, ref r.NearestPollenKind, player, position, "pollen in " + kind);
                }
                else
                {
                    r.InactiveContainerRemaining += remaining;
                }
            }
        }

        private static void CountTinykin(Result r, Vector3 player)
        {
            SbirzEgg[] eggs = Game.INSTANCE.GetAllSbirz();
            r.EggsRegistered = eggs.Length;
            foreach (SbirzEgg egg in eggs)
            {
                if (egg == null)
                {
                    continue;
                }

                if (egg.IsCollected)
                {
                    r.EggsFound++;
                    continue;
                }

                r.EggsRemaining++;
                Increment(r.TinykinRemainingByScene, egg.gameObject.scene.name, 1);
                if (!egg.gameObject.activeInHierarchy)
                {
                    r.EggsInactive++;
                    continue;
                }

                bool locked = egg.trigger != null && !egg.trigger.enabled;
                if (locked)
                {
                    r.EggsLocked++;
                }

                Nearest(ref r.NearestTinykin, ref r.NearestTinykinKind, player, egg.transform.position, locked ? "locked egg" : "egg");
            }

            r.EggsInScenes = Resources.FindObjectsOfTypeAll<SbirzEgg>().Count(IsSceneObject);

            foreach (Cage cage in Resources.FindObjectsOfTypeAll<Cage>())
            {
                if (!IsSceneObject(cage))
                {
                    continue;
                }

                if (cage.IsCompleted)
                {
                    r.CagesBroken++;
                    continue;
                }

                r.CagesUnbroken++;
                r.CageTinykinRemaining += cage.amount;
                Increment(r.TinykinRemainingByScene, cage.gameObject.scene.name, cage.amount);
                Nearest(ref r.NearestTinykin, ref r.NearestTinykinKind, player, cage.transform.position, "cage");
            }
        }

        private static Dictionary<CollectibleContainer, string> MapContainerOwners()
        {
            var map = new Dictionary<CollectibleContainer, string>();
            foreach (Obstacle obstacle in Resources.FindObjectsOfTypeAll<Obstacle>())
            {
                if (IsSceneObject(obstacle) && obstacle.collectiblesContainer != null)
                {
                    map[obstacle.collectiblesContainer] = obstacle is Cage ? "cage" : "breakable";
                }
            }

            foreach (SoapCollectible soap in Resources.FindObjectsOfTypeAll<SoapCollectible>())
            {
                if (IsSceneObject(soap) && soap.pollenContainer != null)
                {
                    map[soap.pollenContainer] = "soap";
                }
            }

            foreach (SbirzTrigger trigger in Resources.FindObjectsOfTypeAll<SbirzTrigger>())
            {
                if (!IsSceneObject(trigger))
                {
                    continue;
                }

                CollectibleContainer container = trigger.GetComponent<CollectibleContainer>();
                if (container != null)
                {
                    map[container] = "tinykin-interaction";
                }
            }

            return map;
        }

        internal static void LogFull(Result r, string reason)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"==== Census ({reason}) - level '{r.Level}' ====");
            sb.AppendLine(SceneSummary());
            sb.AppendLine($"POLLEN  game: {r.PollenCollected}/{r.PollenTotal} collected ({Pct(r.PollenCollected, r.PollenTotal)}), required {r.PollenRequired} -> expected remaining {r.PollenExpected}");
            sb.AppendLine($"        census: {r.Loose} loose + {r.LooseFromContainers} loose from opened containers + {r.ContainerRemaining} in {r.ContainersWithPollen} containers{Kinds(r.ContainerKinds)} = {r.PollenCounted} -> {Verdict(r.PollenCounted, r.PollenExpected)}");
            sb.AppendLine($"        objects: {r.CollectedObjects} collected, {r.Collecting} mid-collect, {r.HeldInContainers} held in containers, {r.InactiveNotCollected} inactive-not-collected{ByScene(r.InactiveNotCollectedByScene)}, {r.InactiveContainerRemaining} pollen in inactive containers");
            sb.AppendLine($"        remaining by scene:{ByScene(r.PollenRemainingByScene)}");
            sb.AppendLine($"TINYKIN game: {r.TinykinFound}/{r.TinykinTotal} found ({Pct(r.TinykinFound, r.TinykinTotal)}) -> expected remaining {r.TinykinExpected}");
            sb.AppendLine($"        census: {r.EggsRemaining} eggs ({r.EggsLocked} locked, {r.EggsInactive} inactive) + {r.CageTinykinRemaining} in {r.CagesUnbroken} unbroken cages = {r.TinykinCounted} -> {Verdict(r.TinykinCounted, r.TinykinExpected)}");
            sb.AppendLine($"        eggs: {r.EggsRegistered} registered, {r.EggsInScenes} in scenes, {r.EggsFound} found; cages: {r.CagesBroken} broken");
            sb.AppendLine($"        remaining by scene:{ByScene(r.TinykinRemainingByScene)}");
            sb.AppendLine($"RADAR GATE ({UnlockThreshold:P0}): pollen {Gate(r.PollenCollected, r.PollenTotal)}, tinykin {Gate(r.TinykinFound, r.TinykinTotal)}");
            if (r.HasSpread)
            {
                Vector3 size = r.PollenSpread.size;
                sb.AppendLine($"Level size (pollen spread): {size.x:F0} x {size.y:F0} x {size.z:F0} units");
            }

            Plugin.Log.LogInfo(sb.ToString().TrimEnd());
        }

        internal static void LogCompact(Result r, string reason)
        {
            Plugin.Log.LogInfo(
                $"Census ({reason}): pollen {r.PollenCollected}/{r.PollenTotal}, remaining {r.PollenExpected} (census {r.PollenCounted}, {Verdict(r.PollenCounted, r.PollenExpected)}); " +
                $"tinykin {r.TinykinFound}/{r.TinykinTotal}, remaining {r.TinykinExpected} (census {r.TinykinCounted}, {Verdict(r.TinykinCounted, r.TinykinExpected)})");
        }

        internal static void LogNearest(Result r)
        {
            Plugin.Log.LogInfo(
                $"Nearest remaining: pollen {Distance(r.NearestPollen)} ({r.NearestPollenKind}), " +
                $"tinykin {Distance(r.NearestTinykin)} ({r.NearestTinykinKind})");
        }

        private static string SceneSummary()
        {
            var names = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                names.Add(scene.isLoaded ? scene.name : scene.name + " (loading)");
            }

            SceneData level = LevelDescription.INSTANCE.sceneData;
            int subSceneObjects = Object.FindObjectsOfType<SubScene>().Length;
            return $"Scenes: {names.Count} [{string.Join(", ", names)}]; level data: {level.subScenes.Length} sub-scenes, " +
                   $"{subSceneObjects} SubScene objects, all loaded: {level.AreSubScenesLoaded()}";
        }

        private static bool IsSceneObject(Component c)
        {
            if (c == null)
            {
                return false;
            }

            Scene scene = c.gameObject.scene;
            return scene.IsValid() && scene.isLoaded;
        }

        private static bool HasAncestor<T>(Transform t) where T : Component
        {
            for (Transform parent = t.parent; parent != null; parent = parent.parent)
            {
                if (parent.GetComponent<T>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static void Nearest(ref float best, ref string bestKind, Vector3 from, Vector3 to, string kind)
        {
            float distance = Vector3.Distance(from, to);
            if (distance < best)
            {
                best = distance;
                bestKind = kind;
            }
        }

        private static void Encapsulate(Result r, Vector3 position)
        {
            if (r.HasSpread)
            {
                r.PollenSpread.Encapsulate(position);
            }
            else
            {
                r.PollenSpread = new Bounds(position, Vector3.zero);
                r.HasSpread = true;
            }
        }

        private static void Increment(Dictionary<string, int> map, string key, int amount)
        {
            map.TryGetValue(key, out int current);
            map[key] = current + amount;
        }

        private static string Pct(int part, int total) => total > 0 ? $"{100f * part / total:F1}%" : "n/a";

        private static string Gate(int part, int total) =>
            total <= 0 ? "n/a (none in level)" : ((float)part / total >= UnlockThreshold ? "unlocked" : "locked");

        private static string Verdict(int counted, int expected) =>
            counted == expected ? "MATCH" : $"MISMATCH ({counted - expected:+#;-#;0})";

        private static string Distance(float d) => float.IsInfinity(d) ? "-" : $"{d:F1} units";

        private static string Kinds(Dictionary<string, int> kinds) =>
            kinds.Count == 0 ? string.Empty : " [" + string.Join(", ", kinds.Select(k => $"{k.Key}: {k.Value}")) + "]";

        private static string ByScene(Dictionary<string, int> byScene) =>
            byScene.Count == 0 ? " none" : " " + string.Join(", ", byScene.Select(k => $"{k.Key}={k.Value}"));
    }
}
#endif
