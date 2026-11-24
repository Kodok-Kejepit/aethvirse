using UnityEngine;

namespace Aethvirse.Combat
{
    /// <summary>
    /// Combo heat (owner, 2026-11-24: fire tiers instead of letter ranks D…SSS): the flame grows
    /// Spark → Ember → Flame → Blaze → Inferno → Phoenix with the hit count.
    /// </summary>
    public enum ComboRank { None, Spark, Ember, Flame, Blaze, Inferno, Phoenix }

    /// <summary>Hit counter + combo heat tier (§6). Pure: time is passed in by the caller.</summary>
    public sealed class ComboTracker
    {
        static readonly (ComboRank rank, int hits)[] Thresholds =
        {
            (ComboRank.Spark, 1), (ComboRank.Ember, 5), (ComboRank.Flame, 10), (ComboRank.Blaze, 20), (ComboRank.Inferno, 35), (ComboRank.Phoenix, 60),
        };

        public readonly float timeout;
        public int Count { get; private set; }
        public int Best { get; private set; }
        float _lastHitTime = float.NegativeInfinity;

        public ComboTracker(float timeoutSeconds = 3f) => timeout = timeoutSeconds;

        public void RegisterHit(float now)
        {
            if (now - _lastHitTime > timeout) Count = 0;
            Count++;
            Best = Mathf.Max(Best, Count);
            _lastHitTime = now;
        }

        /// <summary>Taking damage breaks the combo.</summary>
        public void Break()
        {
            Count = 0;
            _lastHitTime = float.NegativeInfinity;
        }

        public bool IsActive(float now) => Count > 0 && now - _lastHitTime <= timeout;

        public ComboRank Rank(float now) => IsActive(now) ? RankForCount(Count) : ComboRank.None;

        public static ComboRank RankForCount(int hits)
        {
            var result = ComboRank.None;
            foreach (var (rank, needed) in Thresholds)
                if (hits >= needed) result = rank;
            return result;
        }

        /// <summary>XP/drop bonus per rank, applied by reward systems later.</summary>
        /// <summary>Size of the HUD flame for a tier: 0 (none) … 1 (Phoenix).</summary>
        public static float Heat(ComboRank rank) => (int)rank / (float)(int)ComboRank.Phoenix;

        public static float Bonus(ComboRank rank) => rank switch
        {
            ComboRank.Ember => 1.05f, ComboRank.Flame => 1.1f, ComboRank.Blaze => 1.2f, ComboRank.Inferno => 1.35f, ComboRank.Phoenix => 1.5f, _ => 1f,
        };
    }
}
