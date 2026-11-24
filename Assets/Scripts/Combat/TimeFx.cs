using System.Collections.Generic;
using UnityEngine;

namespace Aethvirse.Combat
{
    /// <summary>
    /// Global time effects (hit-stop, slow motion) through Time.timeScale. The strongest (lowest) active
    /// scale wins; each effect ends on a real-time timer, so it restores itself even at scale 0.02.
    /// </summary>
    public sealed class TimeFx : MonoBehaviour
    {
        public const float HitStopScale = 0.02f;

        static TimeFx _instance;
        readonly List<(float scale, float endsAt)> _effects = new List<(float, float)>();

        public static void HitStop(float seconds) => Apply(HitStopScale, seconds);
        public static void SlowMotion(float scale, float seconds) => Apply(scale, seconds);

        public static void Apply(float scale, float seconds)
        {
            if (seconds <= 0f) return;
            if (_instance == null)
            {
                var go = new GameObject("TimeFx") { hideFlags = HideFlags.HideInHierarchy };
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<TimeFx>();
            }
            _instance._effects.Add((scale, Time.unscaledTime + seconds));
            _instance.Refresh();
        }

        public static void ResetAll()
        {
            if (_instance != null) _instance._effects.Clear();
            Time.timeScale = 1f;
        }

        void Update()
        {
            if (_effects.Count == 0) return;
            _effects.RemoveAll(e => Time.unscaledTime >= e.endsAt);
            Refresh();
        }

        void Refresh()
        {
            float scale = 1f;
            foreach (var e in _effects) scale = Mathf.Min(scale, e.scale);
            Time.timeScale = scale;
        }

        void OnDestroy() => Time.timeScale = 1f;
    }
}
