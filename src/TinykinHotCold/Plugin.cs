using BepInEx;
using BepInEx.Logging;

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
#if DEBUG
            gameObject.AddComponent<Dev.DevDiagnostics>();
            Log.LogInfo("Development diagnostics enabled (Debug build).");
#endif
        }

        private void OnDestroy()
        {
            // If this shows up mid-game, the game destroyed our manager object and we need
            // BepInEx's HideManagerGameObject option (or our own persistent object).
            Log.LogWarning("Plugin object destroyed.");
        }
    }
}
