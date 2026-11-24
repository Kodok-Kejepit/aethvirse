using System;
using UnityEngine;

namespace Aethvirse.Combat
{
    /// <summary>Hit points (plain class, unit tested). Invulnerable hits are reported as dodged, not applied.</summary>
    [Serializable]
    public sealed class HealthPool
    {
        public int maxHealth = 100;
        [Tooltip("Never drops below 1 and refills after a pause (training dummies).")]
        public bool immortal;
        public float immortalRefillDelay = 3f;

        public int Current { get; private set; }
        public bool Invulnerable { get; set; }
        public bool IsDead => Current <= 0;
        public float Ratio => maxHealth > 0 ? (float)Current / maxHealth : 0f;

        public event Action<DamageInfo> Damaged;
        public event Action<DamageInfo> Dodged;
        public event Action Died;
        public event Action<int, int> Changed;

        float _sinceLastHit;

        public void Reset()
        {
            Current = maxHealth;
            _sinceLastHit = 0f;
            Changed?.Invoke(Current, maxHealth);
        }

        /// <summary>Returns true if the damage was applied.</summary>
        public bool TakeDamage(DamageInfo info)
        {
            if (IsDead) return false;
            if (Invulnerable)
            {
                Dodged?.Invoke(info);
                return false;
            }
            int floor = immortal ? 1 : 0;
            Current = Mathf.Max(Current - info.amount, floor);
            _sinceLastHit = 0f;
            Damaged?.Invoke(info);
            Changed?.Invoke(Current, maxHealth);
            if (Current <= 0) Died?.Invoke();
            return true;
        }

        /// <summary>Changes max HP (level up, gear). Refill fills it; otherwise the current HP is kept (clamped).</summary>
        public void SetMax(int value, bool refill)
        {
            maxHealth = Mathf.Max(value, 1);
            Current = refill ? maxHealth : Mathf.Min(Current, maxHealth);
            Changed?.Invoke(Current, maxHealth);
        }

        public void Heal(int amount)
        {
            if (IsDead || amount <= 0) return;
            int before = Current;
            Current = Mathf.Min(Current + amount, maxHealth);
            if (Current != before) Changed?.Invoke(Current, maxHealth);
        }

        public void Tick(float deltaTime)
        {
            if (!immortal) return;
            _sinceLastHit += deltaTime;
            if (_sinceLastHit >= immortalRefillDelay && Current < maxHealth) Reset();
        }
    }
}
