using System.Collections.Generic;

namespace Aethvirse.Stats
{
    public enum BuffStat { Attack, Defense, Crit, AttackSpeed, MoveSpeed }

    /// <summary>Timed buffs: one per stat (a new one replaces the old). Pure; time is passed in.</summary>
    public sealed class BuffSet
    {
        readonly List<(BuffStat stat, float amount, float until)> _buffs = new List<(BuffStat, float, float)>();

        public int Count => _buffs.Count;

        public void Add(BuffStat stat, float amount, float duration, float now)
        {
            _buffs.RemoveAll(b => b.stat == stat);
            _buffs.Add((stat, amount, now + duration));
        }

        /// <summary>Drops expired buffs; returns true if any expired.</summary>
        public bool Expire(float now) => _buffs.RemoveAll(b => b.until <= now) > 0;

        public float Total(BuffStat stat)
        {
            float total = 0f;
            foreach (var b in _buffs) if (b.stat == stat) total += b.amount;
            return total;
        }

        public float Remaining(BuffStat stat, float now)
        {
            foreach (var b in _buffs) if (b.stat == stat) return System.Math.Max(0f, b.until - now);
            return 0f;
        }
    }
}
