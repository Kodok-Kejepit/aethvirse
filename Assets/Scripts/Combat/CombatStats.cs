using System;
using UnityEngine;

namespace Aethvirse.Combat
{
    /// <summary>Flat combat numbers used by <see cref="DamageCalculator"/>. Derived from StatsComponent in M3.</summary>
    [Serializable]
    public sealed class CombatStats
    {
        public float attack = 20f;
        public float defense = 5f;
        [Range(0f, 1f)] public float critChance = 0.1f;
        [Tooltip("Crit multiplier (1.5 = +50%).")]
        public float critDamage = 1.5f;
        [Tooltip("Affinity used for incoming element multipliers.")]
        public ElementType element = ElementType.Physical;

        public CombatStats Clone() => (CombatStats)MemberwiseClone();
    }
}
