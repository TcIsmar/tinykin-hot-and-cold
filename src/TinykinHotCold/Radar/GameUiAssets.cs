using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace TinykinHotCold.Radar
{
    /// <summary>
    /// Borrows the look of the game's own HUD: the font and text style of its pollen counter, and the
    /// pollen and tinykin icons. Anything not found falls back to plain text.
    /// </summary>
    internal sealed class GameUiAssets
    {
        // Letters the panel always shows; a font without them is a numbers-only or symbol font.
        private const string RequiredCharacters = "PollenTinyk";

        internal TMP_FontAsset Font { get; private set; }

        /// <summary>The game's text material (outline and shadow), or null to use the font's own.</summary>
        internal Material FontMaterial { get; private set; }

        internal Sprite PollenIcon { get; private set; }

        internal Sprite TinykinIcon { get; private set; }

        internal static GameUiAssets Find()
        {
            var assets = new GameUiAssets();
            try
            {
                assets.FindInGameHud();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Couldn't read the game's HUD style: {ex.Message}");
            }

            if (assets.Font == null)
            {
                TMP_FontAsset[] loaded = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                assets.Font = loaded.FirstOrDefault(HasCharacters) ?? loaded.FirstOrDefault();
            }

            Plugin.Log.LogInfo(
                $"Radar panel style: font '{(assets.Font != null ? assets.Font.name : "none")}', " +
                $"pollen icon: {assets.PollenIcon != null}, tinykin icon: {assets.TinykinIcon != null}");
            return assets;
        }

        private void FindInGameHud()
        {
            GameGUI gui = Game.UI;
            if (gui == null)
            {
                return;
            }

            ProgressUI pollen = ProgressOf(gui.GetComponentInChildren<BasicCollectibleUI>(true));
            ProgressUI tinykin = ProgressOf(gui.GetComponentInChildren<SbirzCollectibleUI>(true));
            PollenIcon = IconOf(pollen);
            TinykinIcon = IconOf(tinykin);

            // Prefer the pollen counter's text style; otherwise the font the game's UI uses most.
            TextMeshProUGUI sample = pollen != null ? pollen.valueDisplay : null;
            if (sample != null && HasCharacters(sample.font))
            {
                Font = sample.font;
                FontMaterial = sample.fontSharedMaterial;
                return;
            }

            var counts = new Dictionary<TMP_FontAsset, int>();
            foreach (TextMeshProUGUI text in gui.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (HasCharacters(text.font))
                {
                    counts.TryGetValue(text.font, out int count);
                    counts[text.font] = count + 1;
                }
            }

            Font = counts.OrderByDescending(c => c.Value).Select(c => c.Key).FirstOrDefault();
            if (Font == null && sample != null && sample.font != null)
            {
                Font = sample.font;
                FontMaterial = sample.fontSharedMaterial;
            }
        }

        private static ProgressUI ProgressOf(CollectibleUI ui) => ui != null ? ui.progressUI : null;

        private static Sprite IconOf(ProgressUI progress) =>
            progress != null && progress.icon != null ? progress.icon.sprite : null;

        private static bool HasCharacters(TMP_FontAsset font) => font != null && font.HasCharacters(RequiredCharacters);
    }
}
