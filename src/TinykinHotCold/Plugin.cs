using BepInEx;
using BepInEx.Logging;
using UnityEngine.SceneManagement;

namespace TinykinHotCold
{
    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    [BepInProcess("Tinykin.exe")]
    public sealed class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log { get; private set; }

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"{PluginInfo.Name} v{PluginInfo.Version} loaded.");
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            // If this shows up mid-game, the game destroyed our manager object and we need
            // BepInEx's HideManagerGameObject option (or our own persistent object).
            Log.LogWarning("Plugin object destroyed.");
        }

        // Phase 0 smoke test: proves the plugin loads, survives scene changes,
        // and can call into the game's own code (Game.dll).
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Log.LogInfo($"Scene loaded: '{scene.name}' ({mode}), full game: {Game.IsFullGame}, game started: {Game.IsGameStarted}");
        }
    }
}
