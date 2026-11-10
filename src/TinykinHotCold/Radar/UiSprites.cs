using UnityEngine;

namespace TinykinHotCold.Radar
{
    /// <summary>Small anti-aliased sprites drawn in code: a rounded box for panels and bars, and an arrow.</summary>
    internal static class UiSprites
    {
        private const int RoundedSize = 64;
        private const int CornerRadius = 20;
        private const int ArrowSize = 32;

        private static Sprite rounded;
        private static Sprite arrow;

        /// <summary>White rounded rectangle, 9-sliced so it stretches to any size.</summary>
        internal static Sprite Rounded => rounded != null ? rounded : rounded = CreateRounded();

        /// <summary>White triangle pointing right.</summary>
        internal static Sprite Arrow => arrow != null ? arrow : arrow = CreateArrow();

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

        private static Sprite CreateArrow()
        {
            Texture2D texture = NewTexture(ArrowSize);
            var a = new Vector2(6f, 3f);
            var b = new Vector2(ArrowSize - 5f, ArrowSize / 2f);
            var c = new Vector2(6f, ArrowSize - 3f);
            for (int y = 0; y < ArrowSize; y++)
            {
                for (int x = 0; x < ArrowSize; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float distance = Mathf.Max(EdgeDistance(p, a, b), Mathf.Max(EdgeDistance(p, b, c), EdgeDistance(p, c, a)));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - distance)));
                }
            }

            texture.Apply();
            return Finish(Sprite.Create(texture, new Rect(0, 0, ArrowSize, ArrowSize), new Vector2(0.5f, 0.5f), 100f));
        }

        /// <summary>Signed distance from p to the edge a-b of a counter-clockwise triangle (negative inside).</summary>
        private static float EdgeDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 edge = (b - a).normalized;
            var outward = new Vector2(edge.y, -edge.x);
            return Vector2.Dot(p - a, outward);
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
