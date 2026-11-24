using Aethvirse.Combat;
using Aethvirse.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Aethvirse.Core
{
    /// <summary>
    /// Combo counter as a living flame (owner, 2026-11-24, instead of letter ranks): three layered
    /// flame tongues grow and change colour with the heat tier (Spark … Phoenix), flicker on their
    /// own, pulse on every hit and fade out when the combo ends. Built in code, no image assets.
    /// </summary>
    public sealed class ComboFlameHud : MonoBehaviour
    {
        [SerializeField] PlayerCombat combat;

        const float MinSize = 90f;
        const float MaxSize = 300f;
        const float GrowSpeed = 6f;
        const float FadeSpeed = 2.5f;
        const float HitPulse = 0.18f;

        RectTransform _root;
        Image _outer, _mid, _core;
        Text _count, _tier;
        CanvasGroup _group;
        int _hits;
        ComboRank _rank;
        float _size;
        float _pulse;
        float _alpha;

        void Awake() => Build();

        void OnEnable() { if (combat != null) combat.ComboChanged += OnCombo; }
        void OnDisable() { if (combat != null) combat.ComboChanged -= OnCombo; }

        void OnCombo(int hits, ComboRank rank)
        {
            if (hits > _hits) _pulse = HitPulse;
            _hits = hits;
            _rank = rank;
            if (hits > 0)
            {
                _count.text = hits.ToString();
                _tier.text = rank.ToString().ToUpperInvariant();
            }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float t = Time.unscaledTime;
            bool on = _hits > 0;
            _alpha = Mathf.MoveTowards(_alpha, on ? 1f : 0f, FadeSpeed * dt);
            _group.alpha = _alpha;
            if (_alpha <= 0f) return;

            float target = Mathf.Lerp(MinSize, MaxSize, ComboTracker.Heat(_rank));
            _size = Mathf.Lerp(_size, on ? target : _size, 1f - Mathf.Exp(-GrowSpeed * dt));
            _pulse = Mathf.Max(_pulse - dt, 0f);
            float pulse = 1f + _pulse / HitPulse * 0.25f;

            (Color outer, Color mid, Color core) = Palette(_rank);
            Flicker(_outer, outer, 1.00f, t, 0.0f, pulse);
            Flicker(_mid, mid, 0.72f, t, 1.7f, pulse);
            Flicker(_core, core, 0.42f, t, 3.1f, pulse);
            _count.fontSize = Mathf.RoundToInt(Mathf.Lerp(44f, 84f, ComboTracker.Heat(_rank)) * pulse);
        }

        /// <summary>Each layer breathes and sways with its own noise.</summary>
        void Flicker(Image layer, Color color, float share, float t, float seed, float pulse)
        {
            float n1 = Mathf.PerlinNoise(t * 3.1f, seed);
            float n2 = Mathf.PerlinNoise(seed, t * 4.3f);
            float w = _size * share * (0.9f + 0.15f * n1) * pulse;
            float h = _size * share * 1.45f * (0.85f + 0.3f * n2) * pulse;
            layer.rectTransform.sizeDelta = new Vector2(w, h);
            layer.rectTransform.localRotation = Quaternion.Euler(0f, 0f, (n1 - 0.5f) * 8f);
            layer.color = color;
        }

        static (Color outer, Color mid, Color core) Palette(ComboRank rank) => rank switch
        {
            ComboRank.Ember => (new Color(0.95f, 0.30f, 0.08f), new Color(1f, 0.60f, 0.15f), new Color(1f, 0.90f, 0.60f)),
            ComboRank.Flame => (new Color(1f, 0.40f, 0.05f), new Color(1f, 0.75f, 0.20f), new Color(1f, 1f, 0.80f)),
            ComboRank.Blaze => (new Color(1f, 0.55f, 0.05f), new Color(1f, 0.85f, 0.25f), new Color(1f, 1f, 0.90f)),
            ComboRank.Inferno => (new Color(0.85f, 0.10f, 0.05f), new Color(1f, 0.40f, 0.05f), new Color(1f, 0.85f, 0.50f)),
            ComboRank.Phoenix => (new Color(0.20f, 0.45f, 1f), new Color(0.45f, 0.80f, 1f), new Color(0.95f, 1f, 1f)),
            _ => (new Color(1f, 0.45f, 0.10f), new Color(1f, 0.70f, 0.20f), new Color(1f, 0.95f, 0.70f)),
        };

        // --- Build ------------------------------------------------------------------------------

        void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            _root = new GameObject("ComboFlame", typeof(RectTransform)).GetComponent<RectTransform>();
            _root.SetParent(transform, false);
            _root.anchorMin = _root.anchorMax = new Vector2(1f, 0.72f);
            _root.anchoredPosition = new Vector2(-210f, 0f);
            _root.sizeDelta = Vector2.zero;

            Sprite flame = FlameSprite();
            _outer = Layer("Outer", flame);
            _mid = Layer("Mid", flame);
            _core = Layer("Core", flame);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _count = Label("Count", font, new Vector2(0f, 55f), 64, FontStyle.Bold);
            _tier = Label("Tier", font, new Vector2(0f, -45f), 30, FontStyle.Bold);
            _size = MinSize;
        }

        Image Layer(string name, Sprite sprite)
        {
            var img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            img.rectTransform.SetParent(_root, false);
            // Pivot at the base so the flame grows upward.
            img.rectTransform.pivot = new Vector2(0.5f, 0.12f);
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        Text Label(string name, Font font, Vector2 position, int size, FontStyle style)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline)).GetComponent<Text>();
            text.rectTransform.SetParent(_root, false);
            text.rectTransform.anchoredPosition = position;
            text.rectTransform.sizeDelta = new Vector2(360f, 120f);
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            var outline = text.GetComponent<Outline>();
            outline.effectColor = new Color(0.25f, 0.05f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        /// <summary>Teardrop flame: round base, tongue tapering to a point, soft edges.</summary>
        static Sprite FlameSprite()
        {
            const int w = 128, h = 192;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float v = (y + 0.5f) / h;
                // Half-width: a round bottom up to 30% height, then a tapering tongue.
                float half = v < 0.3f
                    ? Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow((0.3f - v) / 0.3f, 2f)))
                    : Mathf.Pow(1f - (v - 0.3f) / 0.7f, 1.4f);
                for (int x = 0; x < w; x++)
                {
                    float u = Mathf.Abs((x + 0.5f) / w * 2f - 1f);
                    float edge = Mathf.Clamp01((half - u) / 0.18f);
                    float topFade = Mathf.Clamp01((1f - v) / 0.25f);
                    pixels[y * w + x] = new Color32(255, 255, 255, (byte)(255 * edge * topFade));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.12f), 100f);
        }
    }
}
