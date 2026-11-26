using System.Collections.Generic;
using Aethvirse.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aethvirse.Core
{
    /// <summary>
    /// Placeholder music (no assets): a slow pad per place, built from a short chord progression in a key and mode
    /// that suits it (minor and low for ash and the throne, bright for Verdant, strange for [ ??? ]), crossfading when
    /// the region changes. Real BGM replaces it in Phase 2; the volume follows the master volume.
    /// </summary>
    public sealed class Music : MonoBehaviour
    {
        const int Rate = 22050;
        const float BarSeconds = 4f;
        const float Level = 0.16f;

        /// <summary>(root MIDI note, minor?, progression in scale degrees) per place (pure; tested).</summary>
        public static (int root, bool minor, int[] chords) Theme(string place) => place switch
        {
            "r1_ashfall" => (45, true, new[] { 0, 5, 3, 4 }),
            "r2_verdant" => (50, false, new[] { 0, 4, 5, 3 }),
            "r3_dunes" => (47, true, new[] { 0, 6, 5, 4 }),
            "r4_frostveil" => (52, true, new[] { 0, 3, 5, 6 }),
            "r5_archive" => (48, true, new[] { 0, 5, 1, 4 }),
            "r6_fracture" => (46, true, new[] { 0, 1, 0, 6 }),
            "r7_throne" => (43, true, new[] { 0, 5, 0, 4 }),
            "s1_glasswood" => (55, false, new[] { 0, 2, 5, 3 }),
            "s2_mirrorsea" => (53, false, new[] { 0, 5, 3, 4 }),
            "s3_choir" => (50, true, new[] { 0, 3, 4, 0 }),
            "k1_cinder" => (44, true, new[] { 0, 6, 4, 5 }),
            "k2_hollow_moon" => (49, true, new[] { 0, 2, 0, 5 }),
            "k3_last_city" => (46, true, new[] { 0, 5, 6, 4 }),
            "z1_seat" => (51, false, new[] { 0, 3, 4, 0 }),
            "r0_unknown" => (60, true, new[] { 0, 1, 0, 1 }),
            "Gate" => (42, true, new[] { 0, 1, 5, 4 }),
            "PenaltyZone" => (40, true, new[] { 0, 1, 0, 1 }),
            "Title" => (48, true, new[] { 0, 5, 3, 6 }),
            _ => (52, false, new[] { 0, 3, 4, 0 }), // the academy and anything else
        };

        static Music _instance;
        AudioSource _a, _b;
        bool _aPlaying;
        string _place = "";
        float _check;
        readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (_instance != null || Application.isBatchMode) return;
            var go = new GameObject("Music");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<Music>();
        }

        void Awake()
        {
            _a = gameObject.AddComponent<AudioSource>();
            _b = gameObject.AddComponent<AudioSource>();
            foreach (AudioSource s in new[] { _a, _b }) { s.loop = true; s.volume = 0f; s.playOnAwake = false; s.spatialBlend = 0f; }
        }

        string Place => RegionController.Current != null ? RegionController.Current.id : SceneManager.GetActiveScene().name;

        void Update()
        {
            _check -= Time.unscaledDeltaTime;
            if (_check <= 0f)
            {
                _check = 0.5f;
                string p = Place;
                if (p != _place && p != "Boot")
                {
                    _place = p;
                    AudioSource next = _aPlaying ? _b : _a;
                    next.clip = Clip(p);
                    next.Play();
                    _aPlaying = !_aPlaying;
                }
            }
            // Crossfade over ~3 s.
            AudioSource on = _aPlaying ? _a : _b, off = _aPlaying ? _b : _a;
            float step = Time.unscaledDeltaTime / 3f;
            on.volume = Mathf.MoveTowards(on.volume, Level, step * Level);
            off.volume = Mathf.MoveTowards(off.volume, 0f, step * Level);
            if (off.volume <= 0f && off.isPlaying) off.Stop();
        }

        AudioClip Clip(string place)
        {
            if (_clips.TryGetValue(place, out AudioClip c)) return c;
            var (root, minor, chords) = Theme(place);
            c = Render(place, root, minor, chords);
            _clips[place] = c;
            return c;
        }

        static readonly int[] Major = { 0, 2, 4, 5, 7, 9, 11 };
        static readonly int[] Minor = { 0, 2, 3, 5, 7, 8, 10 };

        /// <summary>MIDI notes of the triad on a scale degree (pure; tested).</summary>
        public static int[] Triad(int root, bool minor, int degree)
        {
            int[] scale = minor ? Minor : Major;
            var notes = new int[3];
            for (int i = 0; i < 3; i++)
            {
                int d = degree + i * 2;
                notes[i] = root + scale[d % 7] + 12 * (d / 7);
            }
            return notes;
        }

        static float Hz(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

        /// <summary>One loop: each chord a bar of soft detuned sines with a slow swell, plus a quiet bass and a sparse high note.</summary>
        static AudioClip Render(string name, int root, bool minor, int[] chords)
        {
            int perBar = Mathf.RoundToInt(Rate * BarSeconds);
            int n = perBar * chords.Length;
            var data = new float[n];
            var rng = new System.Random(name.GetHashCode());
            for (int b = 0; b < chords.Length; b++)
            {
                int[] triad = Triad(root, minor, chords[b]);
                float bass = Hz(triad[0] - 12);
                int bell = triad[rng.Next(3)] + 24;
                float bellHz = Hz(bell), bellAt = 0.25f + (float)rng.NextDouble() * 0.5f;
                for (int i = 0; i < perBar; i++)
                {
                    float t = (float)i / Rate, u = (float)i / perBar;
                    // Swell in and out so bars join smoothly in the loop.
                    float env = Mathf.Sin(Mathf.PI * u);
                    float v = 0f;
                    foreach (int note in triad)
                    {
                        float f = Hz(note);
                        v += Mathf.Sin(2f * Mathf.PI * f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * f * 1.003f * t);
                    }
                    v = v / 4.5f * env;
                    v += 0.35f * Mathf.Sin(2f * Mathf.PI * bass * t) * env;
                    float since = u - bellAt;
                    if (since >= 0f) v += 0.18f * Mathf.Sin(2f * Mathf.PI * bellHz * t) * Mathf.Exp(-since * 9f);
                    data[b * perBar + i] = v * 0.5f;
                }
            }
            AudioClip clip = AudioClip.Create("music_" + name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
