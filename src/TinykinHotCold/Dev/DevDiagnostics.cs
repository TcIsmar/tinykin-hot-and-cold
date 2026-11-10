#if DEBUG
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace TinykinHotCold.Dev
{
    /// <summary>
    /// Development helper (Debug builds only). Logs game state changes and runs the level census
    /// on level start, when a counter changes, and on demand (F10 census, F11 nearest distance).
    /// </summary>
    internal sealed class DevDiagnostics : MonoBehaviour
    {
        private const float CensusDelayAfterStart = 1.5f;
        private const float CounterPollInterval = 1f;

        private bool wasStarted;
        private bool wasPaused;
        private bool wasLocked;
        private bool wasGoggles;
        private bool initialCensusDone;
        private float initialCensusAt;
        private float nextCounterPoll;
        private int lastPollen = -1;
        private int lastTinykin = -1;

        private void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            Plugin.Log.LogInfo("Dev keys: F10 = census, F11 = nearest distance");
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Update()
        {
            TrackState();
            HandleKeys();
            AutoCensus();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Plugin.Log.LogInfo($"Scene loaded: '{scene.name}' ({mode})");
        }

        private void TrackState()
        {
            bool started = Game.IsGameStarted && Game.INSTANCE != null;
            if (started != wasStarted)
            {
                wasStarted = started;
                Plugin.Log.LogInfo($"Game started: {started}");
                if (started)
                {
                    initialCensusDone = false;
                    initialCensusAt = Time.realtimeSinceStartup + CensusDelayAfterStart;
                    lastPollen = -1;
                    lastTinykin = -1;
                }
            }

            if (!started)
            {
                return;
            }

            bool paused = Game.INSTANCE.IsPaused;
            if (paused != wasPaused)
            {
                wasPaused = paused;
                Plugin.Log.LogInfo($"Paused: {paused}");
            }

            PlayerController player = Game.PLAYER;
            if (player == null)
            {
                return;
            }

            bool locked = player.ControlsLocked;
            if (locked != wasLocked)
            {
                wasLocked = locked;
                Plugin.Log.LogInfo($"Controls locked: {locked}");
            }

            bool goggles = player.GlassesMode;
            if (goggles != wasGoggles)
            {
                wasGoggles = goggles;
                Plugin.Log.LogInfo($"Goggles: {goggles}");
            }
        }

        private void HandleKeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !LevelCensus.IsLevelReady)
            {
                return;
            }

            if (keyboard.f10Key.wasPressedThisFrame)
            {
                RunCensus("manual", full: true);
            }

            if (keyboard.f11Key.wasPressedThisFrame)
            {
                LevelCensus.LogNearest(LevelCensus.Take());
            }
        }

        private void AutoCensus()
        {
            if (!LevelCensus.IsLevelReady)
            {
                return;
            }

            float now = Time.realtimeSinceStartup;
            if (!initialCensusDone)
            {
                if (now >= initialCensusAt)
                {
                    initialCensusDone = true;
                    RunCensus("level started", full: true);
                }

                return;
            }

            if (now < nextCounterPoll)
            {
                return;
            }

            nextCounterPoll = now + CounterPollInterval;
            if (LevelCensus.PollenCollected != lastPollen || LevelCensus.TinykinFound != lastTinykin)
            {
                RunCensus("counter changed", full: false);
            }
        }

        private void RunCensus(string reason, bool full)
        {
            try
            {
                LevelCensus.Result result = LevelCensus.Take();
                lastPollen = result.PollenCollected;
                lastTinykin = result.TinykinFound;
                if (full)
                {
                    LevelCensus.LogFull(result, reason);
                }
                else
                {
                    LevelCensus.LogCompact(result, reason);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Census failed: {ex}");
            }
        }
    }
}
#endif
