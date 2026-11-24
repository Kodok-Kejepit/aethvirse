using System;
using UnityEngine;

namespace Aethvirse.Player
{
    /// <summary>Stamina with delayed regeneration and an exhausted state after depletion (plain class, unit tested).</summary>
    [Serializable]
    public sealed class StaminaPool
    {
        public float maxStamina = 100f;
        public float regenPerSecond = 28f;
        public float regenDelay = 0.8f;
        [Tooltip("After hitting 0, stamina must refill to this ratio before it can be spent again.")]
        [Range(0f, 1f)] public float exhaustedRecoverRatio = 0.3f;

        public float Current { get; private set; }
        public bool Exhausted { get; private set; }
        public float Ratio => maxStamina > 0f ? Current / maxStamina : 0f;

        /// <summary>Raised with (current, max) whenever the value changes.</summary>
        public event Action<float, float> Changed;
        public event Action Depleted;

        float _regenCooldown;

        public void Reset()
        {
            Current = maxStamina;
            Exhausted = false;
            _regenCooldown = 0f;
            Changed?.Invoke(Current, maxStamina);
        }

        public bool CanSpend => !Exhausted && Current > 0f;

        /// <summary>Spends a fixed amount (e.g. a dodge). Allowed while any stamina is left; clamps at 0.</summary>
        public bool TrySpend(float amount)
        {
            if (!CanSpend) return false;
            Consume(amount);
            return true;
        }

        /// <summary>Continuous drain. Returns false once stamina can no longer be spent.</summary>
        public bool Drain(float amountPerSecond, float deltaTime)
        {
            if (!CanSpend) return false;
            Consume(amountPerSecond * deltaTime);
            return CanSpend;
        }

        public void Tick(float deltaTime)
        {
            if (_regenCooldown > 0f)
            {
                _regenCooldown = Mathf.Max(_regenCooldown - deltaTime, 0f);
                return;
            }
            if (Current >= maxStamina) return;
            Current = Mathf.Min(Current + regenPerSecond * deltaTime, maxStamina);
            if (Exhausted && Current >= maxStamina * exhaustedRecoverRatio) Exhausted = false;
            Changed?.Invoke(Current, maxStamina);
        }

        void Consume(float amount)
        {
            Current = Mathf.Max(Current - amount, 0f);
            _regenCooldown = regenDelay;
            if (Current <= 0f)
            {
                Exhausted = true;
                Depleted?.Invoke();
            }
            Changed?.Invoke(Current, maxStamina);
        }
    }
}
