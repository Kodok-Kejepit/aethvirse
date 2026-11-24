using System;
using UnityEngine;

namespace Aethvirse.Combat
{
    /// <summary>
    /// Anything that can be hit: holds the health pool and defensive stats (the Godot Hurtbox + Health).
    /// Hits are found by <see cref="HitQuery"/> through any collider on this object or its children.
    /// </summary>
    public sealed class Damageable : MonoBehaviour
    {
        [SerializeField] HealthPool health = new HealthPool();
        [SerializeField] CombatStats stats = new CombatStats();

        public HealthPool Health => health;
        public CombatStats Stats => stats;
        public bool IsDead => health.IsDead;

        /// <summary>Raised for every hit that reaches this object (applied or dodged).</summary>
        public event Action<DamageInfo, bool> HitReceived;

        void Awake() => health.Reset();

        void Update() => health.Tick(Time.deltaTime);

        public void Configure(int maxHealth, CombatStats newStats, bool immortal = false)
        {
            health.maxHealth = maxHealth;
            health.immortal = immortal;
            if (newStats != null) stats = newStats;
            health.Reset();
        }

        /// <summary>Returns true if the damage was applied.</summary>
        public bool Receive(DamageInfo info)
        {
            bool applied = health.TakeDamage(info);
            HitReceived?.Invoke(info, applied);
            return applied;
        }
    }
}
