using UnityEngine;

namespace Aethvirse.Combat
{
    /// <summary>damage = (ATK × skill_mult − DEF × 0.5) × crit × element × reaction × variance, minimum 1 (§6).</summary>
    public static class DamageCalculator
    {
        public const float DefFactor = 0.5f;
        public const float VarianceMin = 0.95f;
        public const float VarianceMax = 1.05f;
        public const int MinDamage = 1;

        public static int Compute(float atk, float skillMult, float def, float critMult,
            float elementMult, float reactionMult, float variance)
        {
            float baseDamage = atk * skillMult - def * DefFactor;
            float total = baseDamage * critMult * elementMult * reactionMult * variance;
            return Mathf.Max(MinDamage, Mathf.RoundToInt(total));
        }

        public static float RollVariance(System.Random rng) =>
            VarianceMin + (float)rng.NextDouble() * (VarianceMax - VarianceMin);

        public static bool RollCrit(System.Random rng, float chance) => rng.NextDouble() < Mathf.Clamp01(chance);

        /// <summary>Full roll for an attacker/defender pair. <paramref name="bonusMult"/> covers counters and similar bonuses.</summary>
        public static DamageInfo Resolve(CombatStats attacker, CombatStats defender, float skillMult,
            ElementType attackElement, System.Random rng, bool forceCrit = false, float bonusMult = 1f, float reactionMult = 1f)
        {
            var info = new DamageInfo { element = attackElement };
            info.isCrit = forceCrit || RollCrit(rng, attacker.critChance);
            float critMult = info.isCrit ? attacker.critDamage : 1f;
            float elementMult = Element.Multiplier(attackElement, defender.element);
            info.amount = Compute(attacker.attack * bonusMult, skillMult, defender.defense,
                critMult, elementMult, reactionMult, RollVariance(rng));
            return info;
        }
    }
}
