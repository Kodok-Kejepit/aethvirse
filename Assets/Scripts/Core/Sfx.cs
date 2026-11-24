using UnityEngine;

namespace Aethvirse.Core
{
    /// <summary>Placeholder sound (procedural, no assets): swings, hits, dodges, level-ups, pickups. Real audio in Phase 2.</summary>
    public sealed class Sfx : MonoBehaviour
    {
        static Sfx _instance;
        AudioSource _src;
        AudioClip _swing, _hit, _dodge, _level, _pickup;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (_instance != null) return;
            var go = new GameObject("Sfx");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<Sfx>();
        }

        void Awake()
        {
            _src = gameObject.AddComponent<AudioSource>();
            _swing = Noise("swing", 0.12f, 900f, 300f, 0.25f);
            _hit = Noise("hit", 0.09f, 220f, 90f, 0.5f);
            _dodge = Noise("dodge", 0.15f, 1400f, 600f, 0.18f);
            _level = Tone("level", new[] { 523f, 659f, 784f, 1047f }, 0.6f);
            _pickup = Tone("pickup", new[] { 988f, 1319f }, 0.18f);
            GameEvents.Attacked += () => Play(_swing, 0.5f);
            GameEvents.Dodged += () => Play(_dodge, 0.5f);
            GameEvents.EnemyKilled += _ => Play(_hit, 0.7f);
            GameEvents.PickedUp += (_, __) => Play(_pickup, 0.35f);
        }

        void Play(AudioClip c, float v) { if (c != null) _src.PlayOneShot(c, v); }

        public static void LevelUp() { if (_instance != null) _instance.Play(_instance._level, 0.6f); }
        public static void Hit() { if (_instance != null) _instance.Play(_instance._hit, 0.45f); }

        static AudioClip Noise(string name, float seconds, float fromHz, float toHz, float gain)
        {
            const int rate = 44100;
            int n = Mathf.RoundToInt(rate * seconds);
            var data = new float[n];
            var rng = new System.Random(name.GetHashCode());
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                phase += 2f * Mathf.PI * Mathf.Lerp(fromHz, toHz, t) / rate;
                float env = (1f - t) * Mathf.Clamp01(t * 40f);
                data[i] = (Mathf.Sin(phase) * 0.5f + ((float)rng.NextDouble() * 2f - 1f) * 0.5f) * env * gain;
            }
            AudioClip clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Tone(string name, float[] notes, float seconds)
        {
            const int rate = 44100;
            int n = Mathf.RoundToInt(rate * seconds), per = n / notes.Length;
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                int k = Mathf.Min(i / per, notes.Length - 1);
                float local = (float)(i - k * per) / per;
                data[i] = Mathf.Sin(2f * Mathf.PI * notes[k] * i / rate) * Mathf.Exp(-local * 3f) * Mathf.Clamp01(local * 50f) * 0.35f;
            }
            AudioClip clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
