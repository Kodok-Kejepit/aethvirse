using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Aethvirse.UI
{
    public enum CodexTone { Info, Reward, Quest, Warning, Anomaly, System }

    /// <summary>
    /// The Codex speaks: slide-in notification cards (top right) with the signature "ding" (§4, §10).
    /// Any system calls <see cref="Notify"/>. Graybox look; the hologram style comes in Phase 2.
    /// </summary>
    public sealed class Codex : MonoBehaviour
    {
        const int MaxCards = 4;
        const float CardLife = 4f;
        const float SlideTime = 0.25f;

        sealed class Card
        {
            public RectTransform rect;
            public CanvasGroup group;
            public float age;
        }

        static Codex _instance;
        readonly List<Card> _cards = new List<Card>();
        readonly Queue<(string text, CodexTone tone)> _queue = new Queue<(string, CodexTone)>();
        RectTransform _stack;
        Font _font;
        AudioSource _audio;
        AudioClip _ding, _alert;

        /// <summary>Shows a Codex message.</summary>
        public static void Notify(string text, CodexTone tone = CodexTone.Info)
        {
            if (_instance == null)
            {
                var go = new GameObject("Codex");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<Codex>();
            }
            _instance._queue.Enqueue((text, tone));
        }

        void Awake()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            _stack = new GameObject("Stack", typeof(RectTransform)).GetComponent<RectTransform>();
            _stack.SetParent(transform, false);
            _stack.anchorMin = _stack.anchorMax = new Vector2(1f, 1f);
            _stack.pivot = new Vector2(1f, 1f);
            _stack.anchoredPosition = new Vector2(-24f, -150f);
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _ding = Tone("CodexDing", new[] { 1318.5f, 1975.5f }, 0.35f);
            _alert = Tone("CodexAlert", new[] { 659.3f, 523.3f }, 0.4f);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            while (_queue.Count > 0 && _cards.Count < MaxCards)
            {
                var (text, tone) = _queue.Dequeue();
                Show(text, tone);
            }
            for (int i = _cards.Count - 1; i >= 0; i--)
            {
                Card c = _cards[i];
                c.age += dt;
                float slideIn = Mathf.Clamp01(c.age / SlideTime);
                float fadeOut = Mathf.Clamp01((CardLife - c.age) / SlideTime);
                c.group.alpha = Mathf.Min(slideIn, fadeOut);
                float x = (1f - EaseOut(slideIn)) * 480f;
                c.rect.anchoredPosition = new Vector2(x, -i * 96f);
                if (c.age >= CardLife)
                {
                    Destroy(c.rect.gameObject);
                    _cards.RemoveAt(i);
                }
            }
        }

        void Show(string text, CodexTone tone)
        {
            var go = new GameObject("Card", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(_stack, false);
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(460f, 84f);
            go.GetComponent<Image>().color = new Color(0.03f, 0.1f, 0.2f, 0.88f);

            var bar = new GameObject("Accent", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            bar.SetParent(rect, false);
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(0f, 1f);
            bar.sizeDelta = new Vector2(6f, 0f);
            bar.pivot = new Vector2(0f, 0.5f);
            bar.GetComponent<Image>().color = ToneColor(tone);

            var label = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.rectTransform.SetParent(rect, false);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(20f, 6f);
            label.rectTransform.offsetMax = new Vector2(-12f, -6f);
            label.font = _font;
            label.fontSize = 24;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = new Color(0.85f, 0.95f, 1f);
            label.text = text;

            _cards.Insert(0, new Card { rect = rect, group = go.GetComponent<CanvasGroup>() });
            _audio.PlayOneShot(tone == CodexTone.Warning || tone == CodexTone.Anomaly ? _alert : _ding, 0.6f);
        }

        static Color ToneColor(CodexTone tone) => tone switch
        {
            CodexTone.Reward => new Color(1f, 0.82f, 0.3f),
            CodexTone.Quest => new Color(0.5f, 1f, 0.6f),
            CodexTone.Warning => new Color(1f, 0.35f, 0.3f),
            CodexTone.Anomaly => new Color(0.75f, 0.4f, 1f),
            CodexTone.System => new Color(0.4f, 0.85f, 1f),
            _ => new Color(0.4f, 0.7f, 1f),
        };

        static float EaseOut(float t) => 1f - (1f - t) * (1f - t);

        /// <summary>A short two-note chime (procedural, no audio asset).</summary>
        static AudioClip Tone(string name, float[] notes, float seconds)
        {
            const int rate = 44100;
            int samples = Mathf.RoundToInt(rate * seconds);
            var data = new float[samples];
            int per = samples / notes.Length;
            for (int i = 0; i < samples; i++)
            {
                int n = Mathf.Min(i / per, notes.Length - 1);
                float t = (float)i / rate;
                float local = (float)(i - n * per) / per;
                float env = Mathf.Exp(-local * 4f) * Mathf.Clamp01(local * 60f);
                data[i] = (Mathf.Sin(2f * Mathf.PI * notes[n] * t) * 0.6f + Mathf.Sin(4f * Mathf.PI * notes[n] * t) * 0.15f) * env * 0.5f;
            }
            AudioClip clip = AudioClip.Create(name, samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
