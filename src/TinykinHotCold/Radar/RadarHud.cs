using UnityEngine;

namespace TinykinHotCold.Radar
{
    /// <summary>What the radar panel shows this frame.</summary>
    internal sealed class HudState
    {
        public bool Visible;
        public string Title = string.Empty;
        public string Count = string.Empty;
        public bool HasHeat;
        public HeatLevel Level;
        public Trend Trend;
        public string Status = string.Empty;
        public string Hint;
    }

    /// <summary>Draws the radar panel with Unity's immediate-mode GUI.</summary>
    internal sealed class RadarHud
    {
        private const float ReferenceHeight = 1080f;
        private const float PulseDuration = 0.18f;

        private static readonly Color[] LevelColors =
        {
            new Color(0.30f, 0.55f, 1.00f), // Freezing
            new Color(0.45f, 0.78f, 1.00f), // Cold
            new Color(0.55f, 0.92f, 0.80f), // Cool
            new Color(1.00f, 0.82f, 0.30f), // Warm
            new Color(1.00f, 0.55f, 0.20f), // Hot
            new Color(1.00f, 0.27f, 0.22f), // Burning
        };

        private static readonly string[] LevelNames = { "FREEZING", "COLD", "COOL", "WARM", "HOT", "BURNING" };

        private static readonly Color DimText = new Color(1f, 1f, 1f, 0.85f);
        private static readonly Color UnlitSegment = new Color(1f, 1f, 1f, 0.12f);

        private Texture2D pixel;
        private GUIStyle titleStyle;
        private GUIStyle countStyle;
        private GUIStyle levelStyle;
        private GUIStyle smallStyle;
        private GUIStyle smallRightStyle;
        private int stylesBuiltFor = -1;
        private float pulseAt = -10f;

        /// <summary>Flashes the current segment, in time with the tick.</summary>
        internal void Pulse() => pulseAt = Time.unscaledTime;

        internal void Draw(HudState state, HudAnchor anchor, float userScale)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            float s = Mathf.Max(0.3f, Screen.height / ReferenceHeight * userScale);
            EnsureResources(s);

            float pad = 12f * s;
            float width = 300f * s;
            float height = (state.HasHeat ? (state.Hint != null ? 104f : 84f) : 64f) * s;
            Rect panel = Place(anchor, width, height, 24f * s);
            float pulse = Mathf.Clamp01(1f - (Time.unscaledTime - pulseAt) / PulseDuration);

            Fill(panel, new Color(0f, 0f, 0f, 0.55f + 0.15f * pulse));
            float inner = width - 2f * pad;
            float x = panel.x + pad;
            float y = panel.y + 8f * s;

            Label(new Rect(x, y, inner, 22f * s), state.Title, titleStyle);
            Label(new Rect(x, y, inner, 22f * s), state.Count, countStyle);
            y += 28f * s;

            if (!state.HasHeat)
            {
                Label(new Rect(x, y, inner, 22f * s), state.Status, smallStyle);
                return;
            }

            int level = (int)state.Level;
            float gap = 4f * s;
            float segment = (inner - 5f * gap) / 6f;
            float barHeight = 12f * s;
            for (int i = 0; i < LevelNames.Length; i++)
            {
                Color color = i <= level ? LevelColors[i] : UnlitSegment;
                if (i == level)
                {
                    color = Color.Lerp(color, Color.white, 0.6f * pulse);
                }

                Fill(new Rect(x + i * (segment + gap), y, segment, barHeight), color);
            }

            y += barHeight + 6f * s;
            levelStyle.normal.textColor = LevelColors[level];
            Label(new Rect(x, y, inner, 22f * s), LevelNames[level], levelStyle);
            Label(new Rect(x, y, inner, 22f * s), TrendText(state.Trend), smallRightStyle);
            y += 24f * s;

            if (state.Hint != null)
            {
                Label(new Rect(x, y, inner, 20f * s), state.Hint, smallStyle);
            }
        }

        private static string TrendText(Trend trend)
        {
            switch (trend)
            {
                case Trend.Warmer:
                    return "↑ warmer";
                case Trend.Colder:
                    return "↓ colder";
                default:
                    return string.Empty;
            }
        }

        private static Rect Place(HudAnchor anchor, float width, float height, float margin)
        {
            float x;
            switch (anchor)
            {
                case HudAnchor.TopLeft:
                case HudAnchor.BottomLeft:
                    x = margin;
                    break;
                case HudAnchor.TopRight:
                case HudAnchor.BottomRight:
                    x = Screen.width - width - margin;
                    break;
                default:
                    x = (Screen.width - width) * 0.5f;
                    break;
            }

            bool top = anchor == HudAnchor.TopLeft || anchor == HudAnchor.TopCenter || anchor == HudAnchor.TopRight;
            float y = top ? margin : Screen.height - height - margin;
            return new Rect(x, y, width, height);
        }

        private void EnsureResources(float scale)
        {
            if (pixel == null)
            {
                pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                pixel.SetPixel(0, 0, Color.white);
                pixel.Apply();
            }

            int key = Mathf.RoundToInt(scale * 100f);
            if (key == stylesBuiltFor && titleStyle != null)
            {
                return;
            }

            stylesBuiltFor = key;
            titleStyle = MakeStyle(17, scale, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            countStyle = MakeStyle(15, scale, FontStyle.Normal, TextAnchor.MiddleRight, DimText);
            levelStyle = MakeStyle(15, scale, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            smallStyle = MakeStyle(13, scale, FontStyle.Normal, TextAnchor.MiddleLeft, DimText);
            smallRightStyle = MakeStyle(13, scale, FontStyle.Normal, TextAnchor.MiddleRight, DimText);
        }

        private static GUIStyle MakeStyle(int baseSize, float scale, FontStyle fontStyle, TextAnchor alignment, Color color)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Max(8, Mathf.RoundToInt(baseSize * scale)),
                fontStyle = fontStyle,
                alignment = alignment,
                clipping = TextClipping.Overflow,
                wordWrap = false,
            };
            style.normal.textColor = color;
            return style;
        }

        private static void Label(Rect rect, string text, GUIStyle style)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            // Drop shadow keeps the text readable over bright scenery.
            Color color = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, style);
            style.normal.textColor = color;
            GUI.Label(rect, text, style);
        }

        private void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, pixel);
            GUI.color = previous;
        }
    }
}
