using BepInEx.Configuration;
using TinykinHotCold.Radar;

namespace TinykinHotCold
{
    /// <summary>
    /// Comfort settings only. Hint strength (distance bands, unlock threshold) is fixed in code on purpose,
    /// so the radar stays a quality-of-life helper rather than a way to skip exploring.
    /// </summary>
    internal sealed class RadarSettings
    {
        private const string ControlsSection = "Controls";
        private const string BindingNote =
            " You can also rebind this in the game's Controls menu. Unity Input System control path; empty = unbound.";

        internal RadarSettings(ConfigFile config)
        {
            ToggleRadarKeyboard = config.Bind(ControlsSection, "ToggleRadarKeyboard", "<Keyboard>/f6",
                "Keyboard key that turns the radar on and off." + BindingNote);
            ToggleRadarGamepad = config.Bind(ControlsSection, "ToggleRadarGamepad", "<Gamepad>/dpad/up",
                "Controller button that turns the radar on and off." + BindingNote);
            PreviousTargetKeyboard = config.Bind(ControlsSection, "PreviousTargetKeyboard", "<Keyboard>/f7",
                "Keyboard key that switches the radar to the previous target (pollen or tinykin)." + BindingNote);
            PreviousTargetGamepad = config.Bind(ControlsSection, "PreviousTargetGamepad", "<Gamepad>/dpad/left",
                "Controller button that switches the radar to the previous target (pollen or tinykin)." + BindingNote);
            NextTargetKeyboard = config.Bind(ControlsSection, "NextTargetKeyboard", "<Keyboard>/f8",
                "Keyboard key that switches the radar to the next target (pollen or tinykin)." + BindingNote);
            NextTargetGamepad = config.Bind(ControlsSection, "NextTargetGamepad", "<Gamepad>/dpad/right",
                "Controller button that switches the radar to the next target (pollen or tinykin)." + BindingNote);

            Sound = config.Bind("Feedback", "Sound", true, "Plays a tick that speeds up as you get closer.");
            Rumble = config.Bind("Feedback", "Rumble", true,
                "Short controller rumble pulses when you're Warm or hotter. Works with Xbox-compatible controllers; " +
                "PlayStation controllers need Steam Input. Also respects the game's own vibration setting.");

            HudPosition = config.Bind("HUD", "Position", HudAnchor.TopCenter, "Where the radar panel appears on screen.");
            HudScale = config.Bind("HUD", "Scale", 1f,
                new ConfigDescription("Size of the radar panel.", new AcceptableValueRange<float>(0.5f, 2f)));
        }

        internal ConfigEntry<string> ToggleRadarKeyboard { get; }

        internal ConfigEntry<string> ToggleRadarGamepad { get; }

        internal ConfigEntry<string> PreviousTargetKeyboard { get; }

        internal ConfigEntry<string> PreviousTargetGamepad { get; }

        internal ConfigEntry<string> NextTargetKeyboard { get; }

        internal ConfigEntry<string> NextTargetGamepad { get; }

        internal ConfigEntry<bool> Sound { get; }

        internal ConfigEntry<bool> Rumble { get; }

        internal ConfigEntry<HudAnchor> HudPosition { get; }

        internal ConfigEntry<float> HudScale { get; }
    }
}
