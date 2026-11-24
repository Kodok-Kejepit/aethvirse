using Aethvirse.Combat;
using UnityEngine;

namespace Aethvirse.Stats
{
    /// <summary>Secondary stats computed from level + primary stats (§8, Balancing sheet "Hero").</summary>
    public sealed class DerivedStats
    {
        public int maxHp;
        public int maxMp;
        public float attack;
        public float magicAttack;
        public float defense;
        public float critChance;
        public float critDamage = 1.5f;
        public float attackSpeed = 1f;
        public float moveSpeed = 1f;
        public float cooldownReduction;
        public float mpRegen;

        public static DerivedStats Compute(int level, StatBlock s, StatTuning t, float weaponAttack)
        {
            return new DerivedStats
            {
                maxHp = Mathf.RoundToInt(t.hpBase + s.vitality * t.hpPerVit + level * t.hpPerLevel),
                maxMp = Mathf.RoundToInt(t.mpBase + s.intelligence * t.mpPerInt + level * t.mpPerLevel),
                attack = s.strength * t.atkPerStr + s.agility * t.atkPerAgi + weaponAttack,
                magicAttack = s.intelligence * t.matkPerInt + weaponAttack,
                defense = s.vitality * t.defPerVit + s.strength * t.defPerStr,
                critChance = Mathf.Min(t.critCap, t.critBase + s.sense * t.critPerSen + s.agility * t.critPerAgi),
                critDamage = t.critDmgBase + s.sense * t.critDmgPerSen,
                attackSpeed = t.aspdBase + s.agility * t.aspdPerAgi,
                moveSpeed = Mathf.Min(t.mspdCap, 1f + s.agility * t.mspdPerAgi),
                cooldownReduction = Mathf.Min(t.cdrCap, s.intelligence * t.cdrPerInt),
                mpRegen = t.mpRegenBase + s.intelligence * t.mpRegenPerInt,
            };
        }

        /// <summary>CombatStats for the damage formula. Magic classes use M.ATK as their main attack.</summary>
        public CombatStats ToCombatStats(bool magic, ElementType element = ElementType.Physical) => new CombatStats
        {
            attack = magic ? magicAttack : attack,
            defense = defense,
            critChance = critChance,
            critDamage = critDamage,
            element = element,
        };
    }
}
