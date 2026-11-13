using UnityEngine;

namespace TinykinHotCold.Radar
{
    /// <summary>A small anti-aliased sprite drawn in code: a rounded box for the panel and the bar segments.</summary>
    internal static class UiSprites
    {
        private const int RoundedSize = 64;
        private const int CornerRadius = 20;

        private static Sprite rounded;

        /// <summary>White rounded rectangle, 9-sliced so it stretches to any size.</summary>
        internal static Sprite Rounded => rounded != null ? rounded : rounded = CreateRounded();

        private static Sprite CreateRounded()
        {
            Texture2D texture = NewTexture(RoundedSize);
            float half = RoundedSize / 2f;
            float inner = half - CornerRadius;
            for (int y = 0; y < RoundedSize; y++)
            {
                for (int x = 0; x < RoundedSize; x++)
                {
                    // Signed distance to a rounded box, measured from the pixel centre.
                    float dx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - inner, 0f);
                    float dy = Mathf.Max(Mathf.Abs(y + 0.5f - half) - inner, 0f);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy) - CornerRadius;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - distance)));
                }
            }

            texture.Apply();
            var border = new Vector4(CornerRadius, CornerRadius, CornerRadius, CornerRadius);
            return Finish(Sprite.Create(texture, new Rect(0, 0, RoundedSize, RoundedSize), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border));
        }

        private static Texture2D NewTexture(int size)
        {
            return new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
        }

        private static Sprite Finish(Sprite sprite)
        {
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
