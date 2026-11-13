using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace TinykinHotCold.Radar
{
    /// <summary>
    /// Finds everything the radar still leads to in the current level, and which of it is nearest:
    /// pollen (loose, or in unopened containers), tinykin (unhatched eggs, unbroken cages), the level's
    /// artifacts and bubble, and the unfinished steps of quests you have started.
    /// A box that pays out when your tinykin bring it something (a letter for a mailbox) leads to that
    /// thing instead, and a reward that only appears once something is done (a soap bar that shows up when
    /// every candle is lit) leads to what's left to do for it.
    /// Only distances and counts leave this class; positions are never shown or logged.
    /// </summary>
    internal sealed class TargetScanner
    {
        private static readonly FieldInfo ContainerFirstIndex =
            typeof(CollectibleContainer).GetField("firstIndex", BindingFlags.Instance | BindingFlags.NonPublic);

        // The unique collectibles the game's level progress page counts (its "soap" counter is the bubble).
        private static readonly string[] LevelCollectibleTypes = { "Artifact", "Bubble" };

        private readonly List<Collectible> pollen = new List<Collectible>();
        private readonly List<CollectibleContainer> containers = new List<CollectibleContainer>();
        private readonly Dictionary<CollectibleContainer, SbirzTrigger> deliveries = new Dictionary<CollectibleContainer, SbirzTrigger>();
        private readonly List<SbirzEgg> eggs = new List<SbirzEgg>();
        private readonly List<Cage> cages = new List<Cage>();
        private readonly List<UniqueCollectible> collectibles = new List<UniqueCollectible>();
        private readonly Dictionary<CollectibleContainer, SoapCollectible> soaps = new Dictionary<CollectibleContainer, SoapCollectible>();
        private readonly Dictionary<RareCollectible, List<Trigger>> revealers = new Dictionary<RareCollectible, List<Trigger>>();
        private readonly List<Quest> quests = new List<Quest>();
        private readonly Dictionary<Quest, int[]> savedProgress = new Dictionary<Quest, int[]>();
        private readonly List<Candidate> candidates = new List<Candidate>();

        // Only used when nothing else is left: spots of rewards that aren't out yet when the level doesn't
        // say what reveals them.
        private readonly List<Candidate> lastResort = new List<Candidate>();

        internal void Clear()
        {
            pollen.Clear();
            containers.Clear();
            deliveries.Clear();
            eggs.Clear();
            cages.Clear();
            collectibles.Clear();
            soaps.Clear();
            revealers.Clear();
            quests.Clear();
            savedProgress.Clear();
            candidates.Clear();
            lastResort.Clear();
        }

        /// <summary>Collects every candidate object in the loaded level. Cheap enough to repeat every few seconds.</summary>
        internal void Discover()
        {
            Clear();
            pollen.AddRange(SceneObjects<Collectible>());
            containers.AddRange(SceneObjects<CollectibleContainer>());
            eggs.AddRange(SceneObjects<SbirzEgg>());
            cages.AddRange(SceneObjects<Cage>());
            collectibles.AddRange(SceneObjects<UniqueCollectible>().Where(IsLevelCollectible));

            foreach (CollectibleContainer c in containers)
            {
                SbirzTrigger trigger = c.GetComponent<SbirzTrigger>();
                if (trigger != null && !trigger.TriggersWithNoObject)
                {
                    deliveries[c] = trigger;
                }
            }

            foreach (SoapCollectible soap in SceneObjects<SoapCollectible>())
            {
                if (soap.pollenContainer != null)
                {
                    soaps[soap.pollenContainer] = soap;
                }
            }

            foreach (Trigger trigger in SceneObjects<Trigger>())
            {
                if (trigger is Quest quest && quest.objectives != null)
                {
                    savedProgress[quest] = SavedProgress(quest);
                    quests.Add(quest);
                }

                // The level wires "when this is done, show that reward" into the trigger's events.
                FindReveals(trigger, trigger.OnExecute);
                if (trigger.filteredOnExecutes != null)
                {
                    foreach (FilteredOnExecute filtered in trigger.filteredOnExecutes)
                    {
                        FindReveals(trigger, filtered?.OnExecute);
                    }
                }
            }
        }

        private void FindReveals(Trigger trigger, UnityEventBase onExecute)
        {
            if (onExecute == null)
            {
                return;
            }

            for (int i = 0; i < onExecute.GetPersistentEventCount(); i++)
            {
                if (onExecute.GetPersistentTarget(i) is RareCollectible reward && reward != null &&
                    onExecute.GetPersistentMethodName(i) == nameof(RareCollectible.Show))
                {
                    if (!revealers.TryGetValue(reward, out List<Trigger> list))
                    {
                        revealers[reward] = list = new List<Trigger>();
                    }

                    list.Add(trigger);
                }
            }
        }

        internal Nearest FindNearest(Vector3 from)
        {
            Gather();
            Nearest nearest = NearestOf(candidates, from);
            return nearest.Found ? nearest : NearestOf(lastResort, from);
        }

        private static Nearest NearestOf(List<Candidate> list, Vector3 from)
        {
            var nearest = new Nearest { Distance = float.PositiveInfinity };
            foreach (Candidate c in list)
            {
                float distance = Vector3.Distance(from, c.Position);
                if (distance < nearest.Distance)
                {
                    nearest.Found = true;
                    nearest.Distance = distance;
                    nearest.Hidden = c.Hidden;
                }
            }

            return nearest;
        }

        /// <summary>How many places the radar can lead to right now, by kind, for the log. No positions.</summary>
        internal string Summary()
        {
            Gather();
            if (candidates.Count == 0 && lastResort.Count == 0)
            {
                return "nothing";
            }

            return string.Join(", ", candidates.Select(c => Label(c, string.Empty))
                .Concat(lastResort.Select(c => Label(c, ", last resort")))
                .GroupBy(label => label)
                .OrderBy(g => g.Key)
                .Select(g => $"{g.Count()} {g.Key}"));
        }

        private static string Label(Candidate c, string suffix) =>
            $"{KindName(c.Kind)} ({c.Source.GetType().Name}{(c.Hidden ? ", hidden" : string.Empty)}{suffix})";

        private void Gather()
        {
            candidates.Clear();
            lastResort.Clear();
            GatherPollen();
            GatherTinykin();
            GatherCollectibles();
            GatherQuestSteps();
        }

        private void GatherPollen()
        {
            foreach (Collectible c in pollen)
            {
                // Collected pollen gets deactivated. Pollen still inside a container is inactive too;
                // the container stands in for it below.
                if (c == null || c.IsCollected || !c.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Add(TargetKind.Pollen, c.transform.position, c, hidden: false);
            }

            foreach (CollectibleContainer c in containers)
            {
                if (c == null || !c.gameObject.activeInHierarchy || RemainingIn(c) <= 0)
                {
                    continue;
                }

                Vector3 position = c.col != null && c.col.enabled ? c.col.bounds.center : c.transform.position;
                if (deliveries.TryGetValue(c, out SbirzTrigger box) && AddDelivery(TargetKind.Pollen, box, position, hidden: true))
                {
                    continue;
                }

                // A soap bar that isn't out yet (one appears once every candle is lit) can't be picked up:
                // lead to what's left to do for it. When the level doesn't say what that is, its spot is only
                // a last resort, since the quest that reveals it is usually tracked on its own.
                if (soaps.TryGetValue(c, out SoapCollectible soap) && !IsOut(soap))
                {
                    if (!AddRevealers(TargetKind.Pollen, soap))
                    {
                        lastResort.Add(new Candidate(TargetKind.Pollen, position, c, hidden: true));
                    }

                    continue;
                }

                Add(TargetKind.Pollen, position, c, hidden: true);
            }
        }

        private void GatherTinykin()
        {
            foreach (SbirzEgg e in eggs)
            {
                if (e == null || e.IsCollected || !e.gameObject.activeInHierarchy)
                {
                    continue;
                }

                // An egg whose trigger is off can't hatch yet: something in the level has to happen first.
                bool locked = e.trigger != null && !e.trigger.enabled;
                Add(TargetKind.Tinykin, e.transform.position, e, hidden: locked);
            }

            foreach (Cage c in cages)
            {
                if (c == null || c.IsCompleted || !c.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Add(TargetKind.Tinykin, c.transform.position, c, hidden: true);
            }
        }

        private void GatherCollectibles()
        {
            foreach (UniqueCollectible c in collectibles)
            {
                if (c == null || c.Collected || c.collectibleItem.Collected || !c.gameObject.activeInHierarchy)
                {
                    continue;
                }

                // A collectible that isn't out yet leads to what's left to do to earn it, when the level says.
                // Otherwise its spot is a last resort, or means nothing at all if it will be handed to you
                // (the quest step that earns it stands in).
                if (!IsOut(c))
                {
                    if (!AddRevealers(TargetKind.Collectible, c) && !c.appearOnPlayer)
                    {
                        lastResort.Add(new Candidate(TargetKind.Collectible, c.transform.position, c, hidden: true));
                    }

                    continue;
                }

                Add(TargetKind.Collectible, c.transform.position, c, hidden: false);
            }
        }

        private void GatherQuestSteps()
        {
            foreach (Quest quest in quests)
            {
                // Only quests you have started, so the radar never gives away one you haven't found.
                if (quest != null && IsStarted(quest))
                {
                    AddQuestSteps(TargetKind.QuestStep, quest, hidden: false);
                }
            }
        }

        /// <summary>
        /// Main and side quests save that you've started them. Optional ones (lighting every candle) don't,
        /// but their steps remember being done, so one counts as started once any of its steps is.
        /// </summary>
        private static bool IsStarted(Quest quest)
        {
            if (quest.Status != Quest.QuestStatus.Hidden || quest.questType != Quest.QuestType.Optional)
            {
                return quest.Status == Quest.QuestStatus.Active;
            }

            foreach (QuestObjective objective in quest.objectives)
            {
                if (objective != null && objective.Progress > 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Something that only shows up once you've done something else, like a soap bar that appears when
        /// every candle in the level is lit: leads to what's still left to do for it, whatever kind of quest
        /// that is. Returns false when the level doesn't say what reveals it, or nothing is left to do.
        /// </summary>
        private bool AddRevealers(TargetKind kind, RareCollectible reward)
        {
            if (!revealers.TryGetValue(reward, out List<Trigger> list))
            {
                return false;
            }

            bool added = false;
            foreach (Trigger trigger in list)
            {
                added |= trigger is Quest quest ? AddQuestSteps(kind, quest, hidden: true) : AddStep(kind, trigger, hidden: true);
            }

            return added;
        }

        private bool AddQuestSteps(TargetKind kind, Quest quest, bool hidden)
        {
            if (quest == null || quest.Status == Quest.QuestStatus.Completed || quest.objectives == null)
            {
                return false;
            }

            savedProgress.TryGetValue(quest, out int[] saved);
            bool added = false;
            for (int i = 0; i < quest.objectives.Length; i++)
            {
                QuestObjective objective = quest.objectives[i];
                if (objective == null || objective.triggers == null)
                {
                    continue;
                }

                // Individual steps don't all remember being done after a reload, but the objective's
                // progress is saved: an objective that is complete in either is skipped entirely.
                int savedSteps = saved != null && i < saved.Length ? saved[i] : 0;
                if (Mathf.Max(objective.Progress, savedSteps) >= StepsIn(objective))
                {
                    continue;
                }

                foreach (Trigger step in objective.triggers)
                {
                    added |= AddStep(kind, step, hidden);
                }
            }

            return added;
        }

        private bool AddStep(TargetKind kind, Trigger step, bool hidden)
        {
            if (step == null || step.IsCompleted || step is Quest || !step.gameObject.activeInHierarchy)
            {
                return false;
            }

            switch (step)
            {
                case SbirzTrigger box when !box.TriggersWithNoObject:
                    Vector3 boxPosition = PositionOf(box);
                    if (!AddDelivery(kind, box, boxPosition, hidden))
                    {
                        Add(kind, boxPosition, box, hidden);
                    }

                    return true;

                case LiftableBase liftable when liftable.ArrivedAtDestination:
                    return false;

                case LiftableBase liftable when liftable.IsBeingLifted:
                    // Your tinykin are carrying it: lead to where it goes, when the game says where that is.
                    if (liftable.finalDestination == null)
                    {
                        return false;
                    }

                    Add(kind, liftable.finalDestination.position, liftable, hidden);
                    return true;

                case RareCollectible rare when rare.Collected:
                    return false;
            }

            Add(kind, PositionOf(step), step, hidden);
            return true;
        }

        /// <summary>A soap bar, artifact or bubble is out once it can be picked up.</summary>
        private static bool IsOut(RareCollectible c) => c.trigger != null && c.trigger.enabled;

        /// <summary>
        /// A box that does something when your tinykin deliver an item to it, like a letter to a mailbox.
        /// Leads to the items that still have to be found; once your tinykin are carrying one, leads back to
        /// the box. Returns false when none of its items can be found, so the box itself can stand in.
        /// </summary>
        private bool AddDelivery(TargetKind kind, SbirzTrigger box, Vector3 boxPosition, bool hidden)
        {
            bool added = false;
            bool carrying = false;
            foreach (SmallLiftable item in box.requiredObjects)
            {
                // Delivered items stay delivered across saves (the game restores ArrivedAtDestination).
                if (item == null || item.ArrivedAtDestination)
                {
                    continue;
                }

                // A carried item moves with you, so it would always read Burning: point at where it goes instead.
                if (item.IsBeingLifted)
                {
                    carrying = true;
                    continue;
                }

                if (!item.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Add(kind, PositionOf(item), item, hidden);
                added = true;
            }

            if (carrying)
            {
                Add(kind, boxPosition, box, hidden);
                added = true;
            }

            return added;
        }

        private void Add(TargetKind kind, Vector3 position, Object source, bool hidden)
        {
            candidates.Add(new Candidate(kind, position, source, hidden));
        }

        private static int RemainingIn(CollectibleContainer c) => c.pollenCount - (int)ContainerFirstIndex.GetValue(c);

        private static int StepsIn(QuestObjective objective)
        {
            int steps = 0;
            foreach (Trigger step in objective.triggers)
            {
                if (step != null)
                {
                    steps += step.Count;
                }
            }

            return steps;
        }

        private static int[] SavedProgress(Quest quest)
        {
            var saved = new int[quest.objectives.Length];
            string key = LevelDescription.INSTANCE.sceneData.GetQuestSaveKey(quest.questName);
            for (int i = 0; i < saved.Length; i++)
            {
                saved[i] = SaveGameSystem.LoadInt(SceneData.GetQuestObjectifSaveKey(i, key));
            }

            return saved;
        }

        private static bool IsLevelCollectible(UniqueCollectible c)
        {
            CollectibleItem item = c.collectibleItem;
            if (item == null || item.ParentData == null || item.item < 0 || item.item >= item.ParentData.items.Length)
            {
                return false;
            }

            return LevelCollectibleTypes.Contains(item.ParentData.type) && !item.ItemData.ignoreCollected;
        }

        private static Vector3 PositionOf(Component c)
        {
            if (c is InteractionTrigger interaction && interaction.mainCollider != null && interaction.mainCollider.enabled)
            {
                return interaction.mainCollider.bounds.center;
            }

            return c.transform.position;
        }

        private static string KindName(TargetKind kind) =>
            kind == TargetKind.QuestStep ? "quest step" : kind.ToString().ToLowerInvariant();

        private static IEnumerable<T> SceneObjects<T>() where T : Component
        {
            foreach (T item in Resources.FindObjectsOfTypeAll<T>())
            {
                // FindObjectsOfTypeAll also returns prefabs and assets; keep only objects in loaded scenes.
                Scene scene = item.gameObject.scene;
                if (scene.IsValid() && scene.isLoaded)
                {
                    yield return item;
                }
            }
        }

        internal struct Nearest
        {
            public bool Found;
            public float Distance;

            /// <summary>The target is inside or behind something (a container, a cage, a locked egg).</summary>
            public bool Hidden;
        }

        private readonly struct Candidate
        {
            internal Candidate(TargetKind kind, Vector3 position, Object source, bool hidden)
            {
                Kind = kind;
                Position = position;
                Source = source;
                Hidden = hidden;
            }

            internal TargetKind Kind { get; }

            internal Vector3 Position { get; }

            internal Object Source { get; }

            internal bool Hidden { get; }
        }

#if DEBUG
        /// <summary>For the census: each reward the level reveals through an event, and what reveals it.</summary>
        internal string DescribeReveals()
        {
            IEnumerable<string> known = revealers.Select(r =>
            {
                string by = string.Join(", ", r.Value.Select(t =>
                    t is Quest q ? $"quest '{q.questName}' ({q.questType}, {q.Status})" : $"{t.GetType().Name} '{t.name}'"));
                return $"{Describe(r.Key)} <- {by}";
            });

            IEnumerable<string> unknown = soaps.Values.Cast<RareCollectible>()
                .Concat(collectibles)
                .Where(c => c != null && !c.Collected && !IsOut(c) && !revealers.ContainsKey(c))
                .Select(c => $"{Describe(c)} <- not found, {(c.appearOnPlayer ? "reward" : "last resort")}");

            List<string> all = known.Concat(unknown).ToList();
            return all.Count == 0 ? "none" : string.Join("; ", all);
        }

        private static string Describe(RareCollectible c)
        {
            string state = c.Collected ? "collected" : IsOut(c) ? "out" : "not out";
            return $"{c.GetType().Name} '{c.name}' ({state})";
        }
#endif
    }
}
