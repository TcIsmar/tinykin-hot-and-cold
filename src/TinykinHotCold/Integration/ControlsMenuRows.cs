using System;
using System.Collections.Generic;
using System.Linq;
using TinykinHotCold.Input;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TinykinHotCold.Integration
{
    /// <summary>
    /// Adds the radar's actions to the game's Controls menu (main menu and pause menu) by cloning one of
    /// the game's own control rows, so players can see and rebind them like any other control.
    /// Rebinds are saved in the mod's config file; the game's "reset controls" resets them too.
    /// </summary>
    internal sealed class ControlsMenuRows : IDisposable
    {
        private const string RowPrefix = "HotCold_";
        private const float ScanInterval = 0.5f;

        private readonly RadarActions actions;
        private float nextScanAt;
        private bool handlingChange;
        private bool failed;

        internal ControlsMenuRows(RadarActions actions)
        {
            this.actions = actions;
            SettingsControlEntry.OnBindingsChanged += OnBindingsChanged;
        }

        public void Dispose()
        {
            SettingsControlEntry.OnBindingsChanged -= OnBindingsChanged;
        }

        /// <summary>Call every frame. Twice a second it looks for Controls menus that don't have our rows yet.</summary>
        internal void Update()
        {
            if (failed || Time.unscaledTime < nextScanAt)
            {
                return;
            }

            nextScanAt = Time.unscaledTime + ScanInterval;
            try
            {
                AddRowsWhereMissing();
            }
            catch (Exception ex)
            {
                failed = true;
                Plugin.Log.LogError($"Couldn't add the radar to the Controls menu: {ex}");
            }
        }

        private void AddRowsWhereMissing()
        {
            // Group the game's control rows by the settings tab they belong to.
            var menus = new Dictionary<Transform, List<SettingsControlEntry>>();
            foreach (SettingsControlEntry row in Resources.FindObjectsOfTypeAll<SettingsControlEntry>())
            {
                Scene scene = row.gameObject.scene;
                if (!scene.IsValid() || !scene.isLoaded || row.transform.parent == null)
                {
                    continue;
                }

                Transform menu = MenuRootOf(row.transform);
                if (!menus.TryGetValue(menu, out List<SettingsControlEntry> rows))
                {
                    menus[menu] = rows = new List<SettingsControlEntry>();
                }

                rows.Add(row);
            }

            foreach (KeyValuePair<Transform, List<SettingsControlEntry>> menu in menus)
            {
                if (!menu.Value.Any(r => r.name.StartsWith(RowPrefix, StringComparison.Ordinal)))
                {
                    AddRows(menu.Value);
                }
            }
        }

        private void AddRows(List<SettingsControlEntry> gameRows)
        {
            List<SettingsControlEntry> candidates = gameRows.Where(r => r.gameObject.activeSelf).ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            SettingsControlEntry template = candidates.OrderBy(r => r.transform, HierarchyOrder.Instance).Last();
            Transform parent = template.transform.parent;
            int insertAt = template.transform.GetSiblingIndex() + 1;

            var created = new List<SettingsControlEntry>();
            foreach (RadarAction action in actions.All)
            {
                SettingsControlEntry row = CloneInactive(template, parent);
                row.name = RowPrefix + action.Id;
                Configure(row, action);
                row.transform.SetSiblingIndex(insertAt++);
                created.Add(row);
            }

            List<SettingsControlEntry> siblings = candidates.Where(r => r.transform.parent == parent).ToList();
            PlaceBelow(parent, siblings, created);
            LinkNavigation(template, created);
            foreach (SettingsControlEntry row in created)
            {
                row.gameObject.SetActive(true);
            }

            Plugin.Log.LogInfo($"Added the radar controls to '{PathOf(parent)}'.");
#if DEBUG
            var rect = (RectTransform)template.transform;
            Plugin.Log.LogInfo(
                $"[dev] controls menu: {gameRows.Count} rows, layout group: {Describe(parent.GetComponent<LayoutGroup>())}, " +
                $"size fitter: {parent.GetComponent<ContentSizeFitter>() != null}, scroll content: {ScrollRectFor(parent) != null}, " +
                $"navigation: {template.navigation.mode}, template '{template.name}' pos {rect.anchoredPosition} size {rect.rect.size}");
#endif
        }

        private static SettingsControlEntry CloneInactive(SettingsControlEntry template, Transform parent)
        {
            // Clone while the source is inactive so the copy's Awake/OnEnable only run after we've pointed
            // it at our action.
            GameObject source = template.gameObject;
            bool wasActive = source.activeSelf;
            source.SetActive(false);
            GameObject copy = Object.Instantiate(source, parent, false);
            source.SetActive(wasActive);
            return copy.GetComponent<SettingsControlEntry>();
        }

        private static void Configure(SettingsControlEntry row, RadarAction action)
        {
            row.controlEntry = action.Reference;
            row.compositeIndex = 0;
            row.rebinds = SettingsControlEntry.Rebinds.Gamepad | SettingsControlEntry.Rebinds.KeyboardAndMouse;
            foreach (InputDisplayUI display in new[] { row.gamepadDisplayUI, row.keyboardDisplayUI })
            {
                if (display != null)
                {
                    display.action = action.Reference;
                    display.compositeIndex = 0;
                }
            }

            SetLabel(row, action.Label);
        }

        private static void SetLabel(SettingsControlEntry row, string text)
        {
            // The label is the row's text that isn't part of a binding display or a "press a key" prompt.
            var skip = new List<Transform>();
            if (row.gamepadDisplayUI != null)
            {
                skip.Add(row.gamepadDisplayUI.transform);
            }

            if (row.keyboardDisplayUI != null)
            {
                skip.Add(row.keyboardDisplayUI.transform);
            }

            if (row.gamepadRebindText != null)
            {
                skip.Add(row.gamepadRebindText.transform);
            }

            if (row.keyboardRebindText != null)
            {
                skip.Add(row.keyboardRebindText.transform);
            }

            foreach (TextMeshProUGUI label in row.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (skip.Any(s => label.transform.IsChildOf(s)))
                {
                    continue;
                }

                // Remove the localization component, or it would put the game's original label back.
                foreach (MonoBehaviour behaviour in label.GetComponents<MonoBehaviour>())
                {
                    if (behaviour != null && behaviour.GetType().Name == "Localize")
                    {
                        Object.DestroyImmediate(behaviour);
                    }
                }

                label.text = text;
                return;
            }
        }

        private static void PlaceBelow(Transform parent, List<SettingsControlEntry> siblings, List<SettingsControlEntry> created)
        {
            if (parent.GetComponent<LayoutGroup>() != null || siblings.Count == 0)
            {
                return; // a layout group places the new rows by itself
            }

            // Rows are placed by hand: continue the spacing of the last two rows.
            List<RectTransform> rows = siblings.OrderBy(r => r.transform.GetSiblingIndex()).Select(r => (RectTransform)r.transform).ToList();
            RectTransform last = rows[rows.Count - 1];
            Vector2 step = rows.Count > 1
                ? last.anchoredPosition - rows[rows.Count - 2].anchoredPosition
                : new Vector2(0f, -last.rect.height);
            for (int i = 0; i < created.Count; i++)
            {
                ((RectTransform)created[i].transform).anchoredPosition = last.anchoredPosition + step * (i + 1);
            }

            // Make room in a scroll list whose height isn't driven by a size fitter.
            if (parent is RectTransform content && ScrollRectFor(parent) != null && parent.GetComponent<ContentSizeFitter>() == null)
            {
                content.sizeDelta += new Vector2(0f, Mathf.Abs(step.y) * created.Count);
            }
        }

        private static void LinkNavigation(SettingsControlEntry template, List<SettingsControlEntry> created)
        {
            if (template.navigation.mode != Navigation.Mode.Explicit)
            {
                return; // automatic navigation finds the new rows by itself
            }

            Selectable after = template.navigation.selectOnDown;
            Selectable previous = template;
            foreach (SettingsControlEntry row in created)
            {
                SetDown(previous, row);
                Navigation navigation = row.navigation;
                navigation.selectOnUp = previous;
                row.navigation = navigation;
                previous = row;
            }

            SetDown(previous, after);
            if (after != null && after.navigation.mode == Navigation.Mode.Explicit && after.navigation.selectOnUp == template)
            {
                Navigation navigation = after.navigation;
                navigation.selectOnUp = previous;
                after.navigation = navigation;
            }
        }

        private static void SetDown(Selectable from, Selectable to)
        {
            Navigation navigation = from.navigation;
            navigation.selectOnDown = to;
            from.navigation = navigation;
        }

        private void OnBindingsChanged()
        {
            if (handlingChange)
            {
                return;
            }

            handlingChange = true;
            try
            {
                if (SettingsControlEntry.IsRebinding)
                {
                    // A rebind just finished (on one of our rows or one of the game's).
                    HashSet<string> gameplay = RadarActions.GameplayPaths();
                    foreach (RadarAction action in actions.All)
                    {
                        action.ResolveConflicts(gameplay);
                        action.SaveToSettings();
                    }
                }
                else
                {
                    // Apart from finished rebinds, the game only raises this when "reset controls" is confirmed.
                    foreach (RadarAction action in actions.All)
                    {
                        action.ResetToDefaults();
                    }

                    Plugin.Log.LogInfo("Controls were reset, so the radar keys are back to their defaults.");
                }

                // Refresh every row's button icons, ours included.
                SettingsControlEntry.BindingChanged();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Couldn't update the radar keys: {ex}");
            }
            finally
            {
                handlingChange = false;
            }
        }

        private static Transform MenuRootOf(Transform row)
        {
            for (Transform t = row.parent; t != null; t = t.parent)
            {
                if (t.GetComponent<TabContents>() != null)
                {
                    return t;
                }
            }

            return row.parent;
        }

        private static ScrollRect ScrollRectFor(Transform content)
        {
            for (Transform t = content.parent; t != null; t = t.parent)
            {
                ScrollRect scroll = t.GetComponent<ScrollRect>();
                if (scroll != null)
                {
                    return scroll.content == content ? scroll : null;
                }
            }

            return null;
        }

        private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;

#if DEBUG
        private static string Describe(Component c) => c == null ? "none" : c.GetType().Name;
#endif

        /// <summary>Orders transforms the way they appear in the hierarchy.</summary>
        private sealed class HierarchyOrder : IComparer<Transform>
        {
            internal static readonly HierarchyOrder Instance = new HierarchyOrder();

            public int Compare(Transform a, Transform b)
            {
                List<int> pathA = SiblingPath(a);
                List<int> pathB = SiblingPath(b);
                for (int i = 0; i < Math.Min(pathA.Count, pathB.Count); i++)
                {
                    if (pathA[i] != pathB[i])
                    {
                        return pathA[i].CompareTo(pathB[i]);
                    }
                }

                return pathA.Count.CompareTo(pathB.Count);
            }

            private static List<int> SiblingPath(Transform t)
            {
                var path = new List<int>();
                for (; t != null; t = t.parent)
                {
                    path.Insert(0, t.GetSiblingIndex());
                }

                return path;
            }
        }
    }
}
