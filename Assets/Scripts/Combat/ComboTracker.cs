using UnityEngine;

namespace Aethvirse.Combat
{
    public enum ComboRank { None, D, C, B, A, S, SSS }

    /// <summary>Hit counter + style rank D → C → B → A → S → SSS (§6). Pure: time is passed in by the caller.</summary>
    public sealed class ComboTracker
    {
        static readonly (ComboRank rank, int hits)[] Thresholds =
        {
            (ComboRank.D, 1), (ComboRank.C, 5), (ComboRank.B, 10), (ComboRank.A, 20), (ComboRank.S, 35), (ComboRank.SSS, 60),
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
        public static float Bonus(ComboRank rank) => rank switch
        {
            ComboRank.C => 1.05f, ComboRank.B => 1.1f, ComboRank.A => 1.2f, ComboRank.S => 1.35f, ComboRank.SSS => 1.5f, _ => 1f,
        };
    }
}
