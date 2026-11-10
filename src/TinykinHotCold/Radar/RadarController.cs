using System;
using System.Collections;
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
        private readonly TrendTracker trend = new TrendTracker();
        private readonly RadarHud hud = new RadarHud();
        private readonly HudState hudState = new HudState();

        private RadarSettings settings;
        private RadarInput input;

        private Game level;
        private float levelReadyAt;
        private bool pickTypeWhenReady;
        private float nextDiscoverAt;
        private float nextEvaluateAt;
        private float nextTickAt;

        private bool radarOn;
        private TargetType type = TargetType.Pollen;
        private HeatLevel heatLevel = HeatLevel.Freezing;
        private TargetScanner.Nearest nearest;
        private bool soundFailed;

        private void Awake()
        {
            settings = Plugin.Settings;
            input = new RadarInput(settings.ToggleRadar.Value, settings.SwitchTarget.Value);
        }

        private void OnDestroy()
        {
            input?.Dispose();
        }

        private void Update()
        {
            hudState.Visible = false;
            if (!TrackLevel())
            {
                return;
            }

            // Hidden, silent and deaf to its keys in menus, dialogues and cutscenes.
            if (Game.INSTANCE.IsPaused || Game.PLAYER.ControlsLocked)
            {
                return;
            }

            if (pickTypeWhenReady)
            {
                pickTypeWhenReady = false;
                type = PickType();
                ResetFeedback();
            }

            if (input.TogglePressed)
            {
                Toggle();
            }

            if (input.SwitchPressed && radarOn)
            {
                SwitchTarget();
            }

            if (!radarOn)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now >= nextDiscoverAt)
            {
                scanner.Discover();
                nextDiscoverAt = now + RediscoverInterval;
            }

            if (now >= nextEvaluateAt)
            {
                Evaluate(now);
                nextEvaluateAt = now + EvaluateInterval;
            }

            hudState.Visible = true;
            if (hudState.HasHeat && now >= nextTickAt)
            {
                Tick(now);
            }
        }

        private void OnGUI()
        {
            if (hudState.Visible)
            {
                hud.Draw(hudState, settings.HudPosition.Value, settings.HudScale.Value);
            }
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
                nextDiscoverAt = levelReadyAt;
                scanner.Clear();
                pickTypeWhenReady = radarOn;
                Plugin.Log.LogInfo($"Level ready: {LevelDescription.INSTANCE.sceneData.sceneName}");
            }

            return Time.unscaledTime >= levelReadyAt;
        }

        private void Toggle()
        {
            radarOn = !radarOn;
            if (radarOn)
            {
                type = PickType();
                ResetFeedback();
            }

            Plugin.Log.LogInfo(radarOn ? $"Radar on ({type})" : "Radar off");
        }

        private void SwitchTarget()
        {
            type = type == TargetType.Pollen ? TargetType.Tinykin : TargetType.Pollen;
            ResetFeedback();
            Plugin.Log.LogInfo($"Radar target: {type}");
        }

        /// <summary>Starts on whichever type is unfinished here, preferring one the radar is unlocked for.</summary>
        private TargetType PickType()
        {
            FoundProgress pollen = FoundProgress.For(TargetType.Pollen);
            FoundProgress tinykin = FoundProgress.For(TargetType.Tinykin);
            if (!pollen.IsComplete && pollen.IsUnlocked)
            {
                return TargetType.Pollen;
            }

            if (!tinykin.IsComplete && tinykin.IsUnlocked)
            {
                return TargetType.Tinykin;
            }

            if (!pollen.IsComplete)
            {
                return TargetType.Pollen;
            }

            return !tinykin.IsComplete ? TargetType.Tinykin : type;
        }

        private void ResetFeedback()
        {
            trend.Reset();
            heatLevel = HeatLevel.Freezing;
            nextEvaluateAt = 0f;
            nextTickAt = 0f;
        }

        private void Evaluate(float now)
        {
            FoundProgress progress = FoundProgress.For(type);
            hudState.Title = type == TargetType.Pollen ? "POLLEN" : "TINYKIN";
            hudState.Count = progress.HasAny ? $"{progress.Remaining} left" : string.Empty;
            hudState.HasHeat = false;
            hudState.Hint = null;

            if (!progress.HasAny)
            {
                hudState.Status = type == TargetType.Pollen ? "No pollen in this level" : "No tinykin in this level";
                return;
            }

            if (progress.IsComplete)
            {
                hudState.Status = "All found here!";
                return;
            }

            if (!progress.IsUnlocked)
            {
                int unlockPercent = Mathf.RoundToInt(FoundProgress.UnlockFraction * 100f);
                hudState.Status = $"Unlocks at {unlockPercent}% · now {progress.Percent}%";
                return;
            }

            nearest = scanner.FindNearest(type, Game.PLAYER.transform.position);
            if (!nearest.Found)
            {
                hudState.Status = "Can't locate the rest";
                return;
            }

            heatLevel = HeatModel.LevelFor(nearest.Distance, heatLevel);
            hudState.HasHeat = true;
            hudState.Level = heatLevel;
            hudState.Trend = trend.Update(now, nearest.Distance, nearest.TargetId);
            hudState.Hint = heatLevel == HeatLevel.Burning && nearest.Hidden ? "Something here is hiding it" : null;
        }

        private void Tick(float now)
        {
            nextTickAt = now + HeatModel.TickInterval(nearest.Distance);
            hud.Pulse();
            if (settings.Sound.Value)
            {
                PlayTickSound();
            }

            if (settings.Rumble.Value && heatLevel >= HeatLevel.Warm)
            {
                StartCoroutine(RumblePulse(heatLevel == HeatLevel.Burning ? 0.7f : heatLevel == HeatLevel.Hot ? 0.45f : 0.25f));
            }
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
