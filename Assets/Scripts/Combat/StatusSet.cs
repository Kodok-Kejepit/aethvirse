using System;
using System.Collections.Generic;

namespace Aethvirse.Combat
{
    public enum StatusType { Burn, Chill, Freeze, Shock, Bleed, Poison, Stun, Slow, Fear, Blind, Petrify, Corrode, Root, Exposed }

    /// <summary>
    /// Status effects on one body (§6.1, pure and tested): timed, some stacking. Damage over time comes out of
    /// <see cref="Tick"/>; three Chill stacks become a Freeze. Control effects (Freeze, Stun, Petrify, Root) disable.
    /// </summary>
    [Serializable]
    public sealed class StatusSet
    {
        public const int ChillToFreeze = 3;
        public const float FreezeTime = 2f;

        sealed class Entry { public float time; public int stacks; }

        readonly Dictionary<StatusType, Entry> _on = new Dictionary<StatusType, Entry>();
        float _dotClock;

        public static int MaxStacks(StatusType t) => t switch { StatusType.Burn => 3, StatusType.Bleed => 5, StatusType.Poison => 5, StatusType.Chill => ChillToFreeze, _ => 1 };

        public bool Has(StatusType t) => _on.TryGetValue(t, out Entry e) && e.time > 0f;
        public int Stacks(StatusType t) => Has(t) ? _on[t].stacks : 0;

        public void Apply(StatusType t, float duration, int stacks = 1)
        {
            if (!_on.TryGetValue(t, out Entry e)) _on[t] = e = new Entry();
            e.stacks = e.time > 0f ? Math.Min(e.stacks + stacks, MaxStacks(t)) : Math.Min(stacks, MaxStacks(t));
            e.time = Math.Max(e.time, duration);
            if (t == StatusType.Chill && e.stacks >= ChillToFreeze)
            {
                Clear(StatusType.Chill);
                Apply(StatusType.Freeze, FreezeTime);
            }
        }

        public void Clear(StatusType t) => _on.Remove(t);

        /// <summary>Advances timers; returns the damage-over-time to deal now (ticks twice a second).</summary>
        public int Tick(float dt, int maxHealth)
        {
            foreach (Entry e in _on.Values) e.time -= dt;
            _dotClock += dt;
            if (_dotClock < 0.5f) return 0;
            _dotClock -= 0.5f;
            float perTick = 0f;
            perTick += Stacks(StatusType.Burn) * 0.006f;
            perTick += Stacks(StatusType.Bleed) * 0.004f;
            perTick += Stacks(StatusType.Poison) * 0.003f;
            perTick += Has(StatusType.Shock) ? 0.004f : 0f;
            return perTick > 0f ? Math.Max(1, (int)Math.Round(maxHealth * perTick)) : 0;
        }

        /// <summary>Cannot act (frozen, stunned, petrified, rooted).</summary>
        public bool Disabled => Has(StatusType.Freeze) || Has(StatusType.Stun) || Has(StatusType.Petrify) || Has(StatusType.Root);

        public float SpeedFactor => Disabled ? 0f : (Has(StatusType.Slow) ? 0.5f : 1f) * (1f - 0.15f * Stacks(StatusType.Chill));

        /// <summary>More damage taken (Corrode −25 % DEF, Superconduct's Exposed −40 %).</summary>
        public float DamageTakenFactor => (Has(StatusType.Corrode) ? 1.2f : 1f) * (Has(StatusType.Exposed) ? 1.35f : 1f);

        public void ClearAll() => _on.Clear();

        /// <summary>What an element leaves behind on a hit.</summary>
        public static (StatusType type, float time)? FromElement(ElementType e) => e switch
        {
            ElementType.Fire => (StatusType.Burn, 3f),
            ElementType.Ice => (StatusType.Chill, 4f),
            ElementType.Lightning => (StatusType.Shock, 2f),
            ElementType.Earth => (StatusType.Slow, 2f),
            ElementType.Shadow => (StatusType.Corrode, 4f),
            ElementType.Light => (StatusType.Blind, 3f),
            _ => null,
        };
    }
}
