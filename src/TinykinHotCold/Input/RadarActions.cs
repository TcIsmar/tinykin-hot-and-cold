using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TinykinHotCold.Input
{
    /// <summary>
    /// The radar's input actions. They live in the mod's own small action asset (never the game's), with
    /// one keyboard and one gamepad binding each, so the game's Controls menu can show and rebind them.
    /// Bindings are stored in the mod's config file, never in the game's save.
    /// </summary>
    internal sealed class RadarActions
    {
        internal const string KeyboardGroup = "KeyboardAndMouse";
        internal const string GamepadGroup = "Gamepad";

        private readonly InputActionAsset asset;
        private bool listening;

        internal RadarActions(RadarSettings settings)
        {
            asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "Tinykin Hot & Cold";
            asset.hideFlags = HideFlags.HideAndDontSave;
            InputActionMap map = asset.AddActionMap("Hot & Cold");

            Toggle = new RadarAction(map, "ToggleRadar", "Radar on/off", settings.ToggleRadarKeyboard, settings.ToggleRadarGamepad);
            All = new[] { Toggle };

            // Starts disabled; see SetListening.
            foreach (RadarAction action in All)
            {
                Plugin.Log.LogInfo($"{action.Label}: {action.Describe()}");
            }
        }

        internal RadarAction Toggle { get; }

        internal IReadOnlyList<RadarAction> All { get; }

        /// <summary>
        /// The radar only listens while you're playing. That keeps it quiet in menus and is required for
        /// the Controls menu: Unity can't rebind an action while it's enabled.
        /// </summary>
        internal void SetListening(bool listen)
        {
            if (listen == listening)
            {
                return;
            }

            listening = listen;
            if (listen)
            {
                asset.Enable();
            }
            else
            {
                asset.Disable();
            }
        }

        /// <summary>Effective paths of the game's gameplay bindings (lower case), for conflict checks.</summary>
        internal static HashSet<string> GameplayPaths()
        {
            var paths = new HashSet<string>();
            PlayerInput input = Game.PLAYER != null ? Game.PLAYER.playerInput : Bootstrap.PlayerInput;
            InputActionMap gameplay = input != null && input.actions != null ? input.actions.FindActionMap("PlayerInputs") : null;
            if (gameplay == null)
            {
                return paths;
            }

            foreach (InputBinding binding in gameplay.bindings)
            {
                if (!binding.isComposite && !string.IsNullOrEmpty(binding.effectivePath))
                {
                    paths.Add(binding.effectivePath.ToLowerInvariant());
                }
            }

            return paths;
        }
    }

    /// <summary>One radar action with a keyboard binding (index 0) and a gamepad binding (index 1).</summary>
    internal sealed class RadarAction
    {
        private const int KeyboardIndex = 0;
        private const int GamepadIndex = 1;

        private readonly ConfigEntry<string> keyboardSetting;
        private readonly ConfigEntry<string> gamepadSetting;

        internal RadarAction(InputActionMap map, string id, string label, ConfigEntry<string> keyboard, ConfigEntry<string> gamepad)
        {
            Id = id;
            Label = label;
            keyboardSetting = keyboard;
            gamepadSetting = gamepad;

            Action = map.AddAction(id, InputActionType.Button);
            Action.expectedControlType = "Button"; // rebinding then only accepts buttons and keys
            Action.AddBinding((string)keyboard.DefaultValue, groups: RadarActions.KeyboardGroup);
            Action.AddBinding((string)gamepad.DefaultValue, groups: RadarActions.GamepadGroup);
            ApplySetting(KeyboardIndex, keyboard.Value);
            ApplySetting(GamepadIndex, gamepad.Value);

            Reference = InputActionReference.Create(Action);
            Reference.hideFlags = HideFlags.HideAndDontSave;
        }

        internal string Id { get; }

        internal string Label { get; }

        internal InputAction Action { get; }

        /// <summary>What the game's Controls menu rows point at.</summary>
        internal InputActionReference Reference { get; }

        internal bool Pressed => Action.triggered;

        internal string Describe() =>
            $"{Show(Action.bindings[KeyboardIndex].effectivePath)}, {Show(Action.bindings[GamepadIndex].effectivePath)}";

        internal void SaveToSettings()
        {
            keyboardSetting.Value = Action.bindings[KeyboardIndex].effectivePath ?? string.Empty;
            gamepadSetting.Value = Action.bindings[GamepadIndex].effectivePath ?? string.Empty;
        }

        internal void ResetToDefaults()
        {
            Action.RemoveAllBindingOverrides();
            SaveToSettings();
        }

        /// <summary>
        /// Radar keys never take a key that the game's gameplay controls use. A fresh rebind onto one is
        /// undone; if a game rebind took one of ours, ours is cleared (the game's controls win).
        /// </summary>
        internal void ResolveConflicts(HashSet<string> gameplayPaths)
        {
            Resolve(KeyboardIndex, keyboardSetting, gameplayPaths);
            Resolve(GamepadIndex, gamepadSetting, gameplayPaths);
        }

        private void Resolve(int index, ConfigEntry<string> setting, HashSet<string> gameplayPaths)
        {
            string path = Action.bindings[index].effectivePath;
            if (string.IsNullOrEmpty(path) || !gameplayPaths.Contains(path.ToLowerInvariant()))
            {
                return;
            }

            bool justRebound = !string.Equals(path, setting.Value, StringComparison.OrdinalIgnoreCase);
            string fallback = justRebound ? setting.Value : string.Empty;
            if (!string.IsNullOrEmpty(fallback) && gameplayPaths.Contains(fallback.ToLowerInvariant()))
            {
                fallback = string.Empty;
            }

            Action.ApplyBindingOverride(index, fallback);
            Plugin.Log.LogWarning(justRebound
                ? $"{Label}: {Show(path)} is already used by the game's controls, keeping {Show(fallback)}."
                : $"{Label}: the game's controls now use {Show(path)}, so it was removed from the radar.");
        }

        private void ApplySetting(int index, string path)
        {
            if (path == null || string.Equals(path, Action.bindings[index].path, StringComparison.OrdinalIgnoreCase))
            {
                Action.RemoveBindingOverride(index);
            }
            else
            {
                Action.ApplyBindingOverride(index, path);
            }
        }

        private static string Show(string path) => string.IsNullOrEmpty(path) ? "(unbound)" : path;
    }
}
