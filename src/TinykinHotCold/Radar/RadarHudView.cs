using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TinykinHotCold.Radar
{
    /// <summary>What the radar panel shows.</summary>
    internal sealed class HudState
    {
        public TargetType Type;
        public string Count = string.Empty;
        public bool HasHeat;
        public HeatLevel Level;
        public string Status = string.Empty;
        public string Hint;
    }

    /// <summary>
    /// The radar panel, built with Unity UI and TextMeshPro in the game's own font and icons:
    /// arrows and the target's icon with how many are left, the six-step hot/cold bar and its level.
    /// </summary>
    internal sealed class RadarHudView
    {
        private const float Width = 320f;
        private const float Margin = 24f;
        private const float PulseDuration = 0.2f;

        private static readonly Color[] LevelColors =
        {
            new Color(0.36f, 0.62f, 1.00f), // Freezing
            new Color(0.47f, 0.80f, 1.00f), // Cold
            new Color(0.56f, 0.93f, 0.78f), // Cool
            new Color(1.00f, 0.84f, 0.32f), // Warm
            new Color(1.00f, 0.58f, 0.22f), // Hot
            new Color(1.00f, 0.30f, 0.25f), // Burning
        };

        private static readonly string[] LevelNames = { "FREEZING", "COLD", "COOL", "WARM", "HOT", "BURNING" };

        private static readonly Color PanelColor = new Color(0.05f, 0.04f, 0.10f, 0.62f);
        private static readonly Color UnlitSegment = new Color(1f, 1f, 1f, 0.16f);
        private static readonly Color SoftWhite = new Color(1f, 1f, 1f, 0.9f);

        private readonly Image[] segments = new Image[LevelNames.Length];

        private GameObject root;
        private RectTransform panel;
        private Image typeIcon;
        private TextMeshProUGUI typeName;
        private TextMeshProUGUI countText;
        private RectTransform bar;
        private TextMeshProUGUI levelText;
        private TextMeshProUGUI hintText;
        private TextMeshProUGUI statusText;

        private GameUiAssets assets;
        private HeatLevel shownLevel;
        private float pulseAt = -10f;

        /// <summary>Uses the game's font and icons. Called whenever a level's HUD is ready.</summary>
        internal void UseAssets(GameUiAssets gameAssets)
        {
            assets = gameAssets;
            if (root != null)
            {
                Object.Destroy(root);
                root = null;
            }
        }

        internal void Pulse() => pulseAt = Time.unscaledTime;

        internal void Hide()
        {
            if (root != null && root.activeSelf)
            {
                root.SetActive(false);
            }
        }

        internal void Show(HudState state, HudAnchor anchor, float scale)
        {
            if (assets == null || assets.Font == null)
            {
                return;
            }

            if (root == null)
            {
                Build();
            }

            Sprite icon = state.Type == TargetType.Pollen ? assets.PollenIcon : assets.TinykinIcon;
            typeIcon.sprite = icon;
            typeIcon.gameObject.SetActive(icon != null);
            typeName.text = state.Type == TargetType.Pollen ? "Pollen" : "Tinykin";
            typeName.gameObject.SetActive(icon == null);
            countText.text = state.Count;

            bar.gameObject.SetActive(state.HasHeat);
            levelText.gameObject.SetActive(state.HasHeat);
            hintText.gameObject.SetActive(state.HasHeat && state.Hint != null);
            statusText.gameObject.SetActive(!state.HasHeat);

            float height;
            if (state.HasHeat)
            {
                shownLevel = state.Level;
                levelText.text = LevelNames[(int)state.Level];
                levelText.color = LevelColors[(int)state.Level];
                hintText.text = state.Hint ?? string.Empty;
                height = state.Hint != null ? 146f : 120f;
            }
            else
            {
                // Status messages are one to three short lines; size the panel to fit.
                int lines = state.Status.Split('\n').Length;
                statusText.text = state.Status;
                statusText.rectTransform.sizeDelta = new Vector2(300f, lines * 24f);
                statusText.rectTransform.anchoredPosition = new Vector2(0f, -58f - lines * 12f);
                height = 68f + lines * 24f;
            }

            Place(anchor, height, scale);
            AnimateBar();
            if (!root.activeSelf)
            {
                root.SetActive(true);
            }
        }

        private void AnimateBar()
        {
            float pulse = Mathf.Clamp01(1f - (Time.unscaledTime - pulseAt) / PulseDuration);
            int level = (int)shownLevel;
            for (int i = 0; i < segments.Length; i++)
            {
                Color color = i <= level ? LevelColors[i] : UnlitSegment;
                float size = 1f;
                if (i == level)
                {
                    color = Color.Lerp(color, Color.white, 0.55f * pulse);
                    size = 1f + 0.22f * pulse;
                }

                segments[i].color = color;
                segments[i].rectTransform.localScale = new Vector3(size, size, 1f);
            }
        }

        private void Place(HudAnchor anchor, float height, float scale)
        {
            float x = 0.5f;
            if (anchor == HudAnchor.TopLeft || anchor == HudAnchor.BottomLeft)
            {
                x = 0f;
            }
            else if (anchor == HudAnchor.TopRight || anchor == HudAnchor.BottomRight)
            {
                x = 1f;
            }

            bool top = anchor == HudAnchor.TopLeft || anchor == HudAnchor.TopCenter || anchor == HudAnchor.TopRight;
            var corner = new Vector2(x, top ? 1f : 0f);
            panel.anchorMin = corner;
            panel.anchorMax = corner;
            panel.pivot = corner;
            panel.sizeDelta = new Vector2(Width, height);
            panel.localScale = new Vector3(scale, scale, 1f);
            panel.anchoredPosition = new Vector2(x == 0f ? Margin : x == 1f ? -Margin : 0f, top ? -Margin : Margin);
        }

        private void Build()
        {
            root = new GameObject("TinykinHotCold HUD");
            root.SetActive(false);
            Object.DontDestroyOnLoad(root);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            Image background = NewImage("Panel", root.transform, UiSprites.Rounded, PanelColor);
            panel = background.rectTransform;

            // Title row: arrows on the edges, the target's icon (or name) and how many are left in the middle.
            NewArrow("Previous", flip: true, anchorX: 0f, x: 26f);
            NewArrow("Next", flip: false, anchorX: 1f, x: -26f);

            var title = new GameObject("Title", typeof(RectTransform));
            title.transform.SetParent(panel, false);
            var titleRect = (RectTransform)title.transform;
            Pin(titleRect, new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(220f, 44f));
            var row = title.AddComponent<HorizontalLayoutGroup>();
            row.childAlignment = TextAnchor.MiddleCenter;
            row.spacing = 10f;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            typeIcon = NewImage("Icon", title.transform, null, Color.white);
            typeIcon.preserveAspect = true;
            var iconLayout = typeIcon.gameObject.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 38f;
            iconLayout.preferredHeight = 38f;
            typeName = NewText("Name", title.transform, 26f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            countText = NewText("Count", title.transform, 26f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

            // The six-step hot/cold bar.
            var barObject = new GameObject("Bar", typeof(RectTransform));
            barObject.transform.SetParent(panel, false);
            bar = (RectTransform)barObject.transform;
            Pin(bar, new Vector2(0.5f, 1f), new Vector2(0f, -73f), new Vector2(276f, 16f));
            const float segmentWidth = 40f;
            const float gap = 7.2f;
            for (int i = 0; i < segments.Length; i++)
            {
                Image segment = NewImage("Segment" + i, bar, UiSprites.Rounded, UnlitSegment);
                segment.pixelsPerUnitMultiplier = 2.6f;
                float centre = -bar.sizeDelta.x / 2f + segmentWidth / 2f + i * (segmentWidth + gap);
                Pin(segment.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(centre, 0f), new Vector2(segmentWidth, 16f));
                segments[i] = segment;
            }

            levelText = NewText("Level", panel, 22f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            Pin(levelText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -101f), new Vector2(290f, 28f));
            hintText = NewText("Hint", panel, 17f, FontStyles.Normal, TextAlignmentOptions.Center, SoftWhite);
            Pin(hintText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -127f), new Vector2(300f, 24f));
            statusText = NewText("Status", panel, 19f, FontStyles.Normal, TextAlignmentOptions.Center, SoftWhite);
            Pin(statusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(300f, 52f));
        }

        private void NewArrow(string name, bool flip, float anchorX, float x)
        {
            Image arrow = NewImage(name, panel, UiSprites.Arrow, SoftWhite);
            Pin(arrow.rectTransform, new Vector2(anchorX, 1f), new Vector2(x, -34f), new Vector2(18f, 22f));
            if (flip)
            {
                arrow.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            }
        }

        private static Image NewImage(string name, Transform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private TextMeshProUGUI NewText(string name, Transform parent, float size, FontStyles style, TextAlignmentOptions alignment, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.font = assets.Font;
            if (assets.FontMaterial != null)
            {
                text.fontSharedMaterial = assets.FontMaterial;
            }
            else
            {
                // No game text style found: add an outline so the text stays readable over the level.
                text.outlineWidth = 0.18f;
                text.outlineColor = new Color32(0, 0, 0, 200);
            }

            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.enableWordWrapping = false;
            text.richText = false;
            text.raycastTarget = false;
            return text;
        }

        private static void Pin(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
