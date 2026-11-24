using System;
using UnityEngine;

namespace Aethvirse.Stats
{
    /// <summary>MP pool with constant regeneration (plain class, unit tested).</summary>
    [Serializable]
    public sealed class ManaPool
    {
        public float maxMana = 100f;
        public float regenPerSecond = 2f;
        public float Current { get; private set; }
        public float Ratio => maxMana > 0f ? Current / maxMana : 0f;

        public void SetMax(float value, bool fill)
        {
            maxMana = Mathf.Max(value, 1f);
            Current = fill ? maxMana : Mathf.Min(Current, maxMana);
        }

        public void Refill() => Current = maxMana;
        public void SetRatio(float ratio) => Current = maxMana * Mathf.Clamp01(ratio);
        public void Restore(float amount) => Current = Mathf.Min(Current + amount, maxMana);
        public bool CanPay(float cost) => Current >= cost;

        public bool TryPay(float cost)
        {
            if (!CanPay(cost)) return false;
            Current -= cost;
            return true;
        }

        public void Tick(float deltaTime) => Current = Mathf.Min(Current + regenPerSecond * deltaTime, maxMana);
    }
}
