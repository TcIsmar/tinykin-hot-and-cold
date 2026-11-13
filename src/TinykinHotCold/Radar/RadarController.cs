using System;
using System.Collections;
using TinykinHotCold.Input;
using TinykinHotCold.Integration;
using UnityEngine;

namespace TinykinHotCold.Radar
{
    /// <summary>
    /// Runs the hot/cold radar: follows level loads, reads input, finds the nearest missing item a few
    /// times per second and drives the panel, tick sound and rumble.
    /// </summary>
    internal sealed class RadarController : MonoBehaviour
    {
        private const float StartDelay = 1f;
        private const float EvaluateInterval = 0.1f;
        private const float RediscoverInterval = 10f;
        private const float RumbleDuration = 0.07f;

        private readonly TargetScanner scanner = new TargetScanner();
        private readonly RadarHudView view = new RadarHudView();
        private readonly HudState hudState = new HudState();

        private RadarSettings settings;
        private RadarActions actions;
        private ControlsMenuRows controlsMenu;

        private Game level;
        private float levelReadyAt;
        private bool levelPrepared;
        private float nextDiscoverAt;
        private float nextEvaluateAt;
        private float nextTickAt;

        private bool radarOn;
        private HeatLevel heatLevel = HeatLevel.Freezing;
        private TargetScanner.Nearest nearest;
        private bool soundFailed;

        private void Awake()
        {
            settings = Plugin.Settings;
            actions = new RadarActions(settings);
            controlsMenu = new ControlsMenuRows(actions);
        }

        private void OnDestroy()
        {
            controlsMenu?.Dispose();
        }

        private void Update()
        {
            controlsMenu.Update();
            if (!UpdateRadar())
            {
                view.Hide();
            }
        }

        /// <summary>Returns true while the panel should be on screen.</summary>
        private bool UpdateRadar()
        {
            // Hidden, silent and deaf to its keys in menus, dialogues and cutscenes.
            bool playing = TrackLevel() && !Game.INSTANCE.IsPaused && !Game.PLAYER.ControlsLocked;
            actions.SetListening(playing);
            if (!playing)
            {
                return false;
            }

            if (!levelPrepared)
            {
                levelPrepared = true;
                view.UseAssets(GameUiAssets.Find());
                if (radarOn)
                {
                    ResetFeedback();
                }
            }

            if (actions.Toggle.Pressed)
            {
                Toggle();
            }

            if (!radarOn)
            {
                return false;
            }

            float now = Time.unscaledTime;
            if (now >= nextDiscoverAt)
            {
                scanner.Discover();
                nextDiscoverAt = now + RediscoverInterval;
            }

            if (now >= nextEvaluateAt)
            {
                Evaluate();
                nextEvaluateAt = now + EvaluateInterval;
            }

            if (hudState.HasHeat && now >= nextTickAt)
            {
                Tick(now);
            }

            view.Show(hudState, settings.HudPosition.Value, settings.HudScale.Value);
            return true;
        }

        /// <summary>True once a level is running and its saved state has been applied.</summary>
        private bool TrackLevel()
        {
            bool ready = Game.IsGameStarted && Game.INSTANCE != null && Game.PLAYER != null &&
                         LevelDescription.INSTANCE != null && LevelDescription.INSTANCE.sceneData != null;
            if (!ready)
            {
                if (!ReferenceEquals(level, null))
                {
                    level = null;
                    scanner.Clear();
                }

                return false;
            }

            if (!ReferenceEquals(level, Game.INSTANCE))
            {
                level = Game.INSTANCE;
                levelReadyAt = Time.unscaledTime + StartDelay;
                levelPrepared = false;
                nextDiscoverAt = levelReadyAt;
                scanner.Clear();
                Plugin.Log.LogInfo($"Level ready: {LevelDescription.INSTANCE.sceneData.sceneName}");
            }

            return Time.unscaledTime >= levelReadyAt;
        }

        private void Toggle()
        {
            radarOn = !radarOn;
            if (!radarOn)
            {
                Plugin.Log.LogInfo("Radar off");
                return;
            }

            ResetFeedback();
            scanner.Discover();
            nextDiscoverAt = Time.unscaledTime + RediscoverInterval;
            Plugin.Log.LogInfo($"Radar on. Can lead to: {scanner.Summary()}");
        }

        private void ResetFeedback()
        {
            heatLevel = HeatLevel.Freezing;
            nextEvaluateAt = 0f;
            nextTickAt = 0f;
        }

        private void Evaluate()
        {
            FoundProgress pollen = FoundProgress.Pollen();
            FoundProgress tinykin = FoundProgress.Tinykin();
            hudState.HasHeat = false;
            hudState.Hint = null;

            if (!pollen.IsUnlocked || !tinykin.IsUnlocked)
            {
                hudState.Status = LockedMessage(pollen, tinykin);
                return;
            }

            // One shared bar for everything that's left; the panel never says what or how many.
            nearest = scanner.FindNearest(Game.PLAYER.transform.position);
            if (!nearest.Found)
            {
                hudState.Status = pollen.IsComplete && tinykin.IsComplete
                    ? "Nothing left to track here!"
                    : "Can't locate the rest right now";
                return;
            }

            heatLevel = HeatModel.LevelFor(nearest.Distance, heatLevel);
            hudState.HasHeat = true;
            hudState.Level = heatLevel;
            hudState.Hint = heatLevel == HeatLevel.Burning && nearest.Hidden ? "Something here is hiding it" : null;
        }

        /// <summary>Unlocks once both are at 90%; a level without pollen or without tinykin only needs the other.</summary>
        private static string LockedMessage(FoundProgress pollen, FoundProgress tinykin)
        {
            int unlockPercent = Mathf.RoundToInt(FoundProgress.UnlockFraction * 100f);
            if (pollen.HasAny && tinykin.HasAny)
            {
                return $"Radar locked\nFind {unlockPercent}% of pollen and tinykin\n" +
                       $"(pollen {pollen.Percent}%, tinykin {tinykin.Percent}%)";
            }

            string noun = pollen.HasAny ? "pollen" : "tinykin";
            int percent = pollen.HasAny ? pollen.Percent : tinykin.Percent;
            return $"Radar locked\nFind {unlockPercent}% of the {noun} first\n(you're at {percent}%)";
        }

        private void Tick(float now)
        {
            nextTickAt = now + HeatModel.TickInterval(nearest.Distance);
            view.Pulse();
            if (settings.Sound.Value)
            {
                PlayTickSound();
            }

            if (settings.Rumble.Value && heatLevel >= HeatLevel.Warm)
            {
                StartCoroutine(RumblePulse(RumbleStrength(heatLevel)));
            }
        }

        /// <summary>Stronger the hotter it gets: 0.25 at Warm up to 0.7 at Burning.</summary>
        private static float RumbleStrength(HeatLevel level)
        {
            float t = (float)(level - HeatLevel.Warm) / (HeatLevel.Burning - HeatLevel.Warm);
            return Mathf.Lerp(0.25f, 0.7f, t);
        }

        private void PlayTickSound()
        {
            if (soundFailed)
            {
                return;
            }

            try
            {
                // The game's "contextual icon" UI sound, picked in playtesting.
                AK.Wwise.Event tick = GameData.DATA.UI.wwiseEvent_ContextualIcon;
                if (tick != null && tick.IsValid())
                {
                    GameGUI.PostWwiseEvent(tick);
                }
            }
            catch (Exception ex)
            {
                soundFailed = true;
                Plugin.Log.LogWarning($"Tick sound turned off after an error: {ex.Message}");
            }
        }

        private static IEnumerator RumblePulse(float strength)
        {
            PlayerController player = Game.PLAYER;
            if (player == null)
            {
                yield break;
            }

            // Goes through the game's own rumble, which honors its vibration setting.
            player.StartRumble(strength);
            yield return new WaitForSecondsRealtime(RumbleDuration);
            if (player != null)
            {
                player.StopRumble();
            }
        }
    }
}
