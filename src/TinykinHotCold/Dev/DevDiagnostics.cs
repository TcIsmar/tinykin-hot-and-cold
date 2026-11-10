#if DEBUG
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;

namespace TinykinHotCold.Dev
{
    /// <summary>
    /// Phase 1 development helper (Debug builds only). Logs game state changes, runs the level
    /// census, and tests keyboard/controller input, sounds and rumble.
    /// </summary>
    internal sealed class DevDiagnostics : MonoBehaviour
    {
        private const float CensusDelayAfterStart = 1.5f;
        private const float CounterPollInterval = 1f;

        private readonly List<SoundTest> soundTests = new List<SoundTest>();
        private int nextSound;
        private AudioSource beepSource;
        private AudioClip beepClip;

        private bool wasStarted;
        private bool wasPaused;
        private bool wasLocked;
        private bool wasGoggles;
        private bool initialCensusDone;
        private float initialCensusAt;
        private float nextCounterPoll;
        private int lastPollen = -1;
        private int lastTinykin = -1;

        private sealed class SoundTest
        {
            public string Name;
            public Action Play;
        }

        private void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            Plugin.Log.LogInfo("Dev keys: F6 / D-pad Up = census, F7 / D-pad Left or Right = nearest distance, F8 / D-pad Down = sound + rumble test");
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Update()
        {
            TrackState();
            HandleInput();
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
                    soundTests.Clear();
                    nextSound = 0;
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

        private void HandleInput()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad pad = Gamepad.current;

            string by = PressedBy(keyboard?.f6Key, "F6", pad?.dpad.up, "D-pad Up");
            if (by != null)
            {
                LogInput(by, pad);
                if (RequireLevel())
                {
                    RunCensus("manual, " + by, full: true);
                }
            }

            by = PressedBy(keyboard?.f7Key, "F7", pad?.dpad.left, "D-pad Left") ?? PressedBy(null, null, pad?.dpad.right, "D-pad Right");
            if (by != null)
            {
                LogInput(by, pad);
                if (RequireLevel())
                {
                    LevelCensus.LogNearest(LevelCensus.Take());
                }
            }

            by = PressedBy(keyboard?.f8Key, "F8", pad?.dpad.down, "D-pad Down");
            if (by != null)
            {
                LogInput(by, pad);
                if (RequireLevel())
                {
                    PlayNextSound();
                    StartCoroutine(RumblePulse());
                }
            }
        }

        private static string PressedBy(ButtonControl key, string keyName, ButtonControl button, string buttonName)
        {
            if (key != null && key.wasPressedThisFrame)
            {
                return keyName;
            }

            if (button != null && button.wasPressedThisFrame)
            {
                return buttonName;
            }

            return null;
        }

        private static void LogInput(string by, Gamepad pad)
        {
            string device = by.StartsWith("D-pad") && pad != null ? $" on '{pad.displayName}'" : string.Empty;
            bool paused = Game.INSTANCE != null && Game.INSTANCE.IsPaused;
            bool locked = Game.PLAYER != null && Game.PLAYER.ControlsLocked;
            Plugin.Log.LogInfo($"Input: {by}{device} (paused: {paused}, controls locked: {locked})");
        }

        private static bool RequireLevel()
        {
            if (LevelCensus.IsLevelReady)
            {
                return true;
            }

            Plugin.Log.LogInfo("  (not in a level yet)");
            return false;
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

        private void PlayNextSound()
        {
            if (soundTests.Count == 0)
            {
                BuildSoundTests();
            }

            int index = nextSound % soundTests.Count;
            nextSound++;
            SoundTest test = soundTests[index];
            Plugin.Log.LogInfo($"Sound test {index + 1}/{soundTests.Count}: {test.Name}");
            try
            {
                test.Play();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"  sound test failed: {ex.Message}");
            }
        }

        private void BuildSoundTests()
        {
            GameData data = GameData.DATA;
            AddWwise("UI contextual icon", data.UI.wwiseEvent_ContextualIcon);
            AddWwise("UI target lock", data.UI.wwiseEvent_targetLock);
            AddWwise("UI tinykin type", data.UI.wwiseEvent_sbirzType);
            AddWwise("UI quest progress", data.UI.wwiseEvent_questProgress);
            AddWwise("Pollen attraction", data.collectibles.attractionFeedback.wwiseEvent);
            soundTests.Add(new SoundTest { Name = "Unity beep (built-in audio)", Play = PlayUnityBeep });
        }

        private void AddWwise(string name, AK.Wwise.Event wwiseEvent)
        {
            soundTests.Add(new SoundTest
            {
                Name = name + " (Wwise)",
                Play = () =>
                {
                    if (wwiseEvent == null || !wwiseEvent.IsValid())
                    {
                        Plugin.Log.LogWarning("  event is not valid");
                        return;
                    }

                    GameGUI.PostWwiseEvent(wwiseEvent);
                }
            });
        }

        private void PlayUnityBeep()
        {
            if (beepClip == null)
            {
                const int rate = 44100;
                const int length = rate / 10;
                var samples = new float[length];
                for (int i = 0; i < length; i++)
                {
                    float attack = Mathf.Clamp01(i / (rate * 0.005f));
                    float release = Mathf.Clamp01((length - i) / (rate * 0.03f));
                    samples[i] = Mathf.Sin(2f * Mathf.PI * 1000f * i / rate) * 0.5f * attack * release;
                }

                beepClip = AudioClip.Create("HotColdBeep", length, 1, rate, false);
                beepClip.SetData(samples, 0);
                beepSource = gameObject.AddComponent<AudioSource>();
                beepSource.playOnAwake = false;
                beepSource.spatialBlend = 0f;
                beepSource.ignoreListenerPause = true;
            }

            int listeners = FindObjectsOfType<AudioListener>().Length;
            Plugin.Log.LogInfo($"  Unity audio: output sample rate {AudioSettings.outputSampleRate}, listeners {listeners}, listener volume {AudioListener.volume}");
            if (listeners == 0)
            {
                gameObject.AddComponent<AudioListener>();
                Plugin.Log.LogInfo("  no AudioListener in the scene; added one for this test");
            }

            beepSource.PlayOneShot(beepClip, 1f);
        }

        private static IEnumerator RumblePulse()
        {
            PlayerController player = Game.PLAYER;
            if (player == null)
            {
                yield break;
            }

            player.StartRumble(0.8f);
            yield return new WaitForSecondsRealtime(0.15f);
            if (player != null)
            {
                player.StopRumble();
            }
        }
    }
}
#endif
