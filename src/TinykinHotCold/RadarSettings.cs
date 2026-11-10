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
        internal RadarSettings(ConfigFile config)
        {
            ToggleRadar = config.Bind(
                "Controls",
                "ToggleRadar",
                "<Keyboard>/f6;<Gamepad>/dpad/up",
                "Turns the radar on and off. Unity Input System control paths, separated by ';'.");

            SwitchTarget = config.Bind(
                "Controls",
                "SwitchTarget",
                "<Keyboard>/f7;<Gamepad>/dpad/left;<Gamepad>/dpad/right",
                "Switches the radar between pollen and tinykin. Unity Input System control paths, separated by ';'.");

            Sound = config.Bind(
                "Feedback",
                "Sound",
                true,
                "Plays a tick that speeds up as you get closer.");

            Rumble = config.Bind(
                "Feedback",
                "Rumble",
                true,
                "Short controller rumble pulses when you're Warm or hotter. Works with Xbox-compatible controllers; " +
                "PlayStation controllers need Steam Input. Also respects the game's own vibration setting.");

            HudPosition = config.Bind(
                "HUD",
                "Position",
                HudAnchor.TopCenter,
                "Where the radar panel appears on screen.");

            HudScale = config.Bind(
                "HUD",
                "Scale",
                1f,
                new ConfigDescription("Size of the radar panel.", new AcceptableValueRange<float>(0.5f, 2f)));
        }

        internal ConfigEntry<string> ToggleRadar { get; }

        internal ConfigEntry<string> SwitchTarget { get; }

        internal ConfigEntry<bool> Sound { get; }

        internal ConfigEntry<bool> Rumble { get; }

        internal ConfigEntry<HudAnchor> HudPosition { get; }

        internal ConfigEntry<float> HudScale { get; }
    }
}
