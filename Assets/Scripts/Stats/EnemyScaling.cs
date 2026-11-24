using UnityEngine;

namespace Aethvirse.Stats
{
    public enum EnemyTier { Normal, Elite, Boss }

    /// <summary>
    /// Enemy numbers per level, derived from a reference hero of the same level (Balancing sheet "Musuh"):
    /// a Normal dies in ~hitsToKillNormal hero hits and kills the hero in ~normalHitsToKillHero hits.
    /// </summary>
    public static class EnemyScaling
    {
        public struct Result
        {
            public int maxHealth;
            public float attack;
            public float defense;
            public int xp;
            public int gold;
        }

        public static Result Compute(int level, EnemyTier tier, ClassData reference, StatTuning t)
        {
            StatBlock heroStats = reference.baseStats.Plus(reference.growth, level - 1)
                .Plus(reference.recommendedSplit, t.freePointsPerLevel * (level - 1)).Rounded();
            float weapon = level * t.weaponAtkPerLevel;
            DerivedStats hero = DerivedStats.Compute(level, heroStats, t, weapon);
            float heroAtk = reference.magicDamage ? hero.magicAttack : hero.attack;

            var r = new Result { defense = Mathf.Round(level * t.enemyDefPerLevel) };
            float avgHit = Mathf.Max(1f, heroAtk - r.defense * t.defFactor) * (1f + hero.critChance * (hero.critDamage - 1f));
            float normalHp = avgHit * t.hitsToKillNormal;
            float normalAtk = hero.maxHp / t.normalHitsToKillHero + hero.defense * t.defFactor;
            float kills = Mathf.Round(t.killsPerLevelBase + t.killsPerLevelGrowth * level);
            float normalXp = Progression.XpNeeded(level, t) / Mathf.Max(kills, 1f);
            float normalGold = t.goldBase * Mathf.Pow(t.goldGrowth, level);
            (float hp, float atk, float xp) mult = tier switch
            {
                EnemyTier.Elite => (t.eliteHpMult, t.eliteAtkMult, t.eliteXpMult),
                EnemyTier.Boss => (t.bossHpMult, t.bossAtkMult, t.bossXpMult),
                _ => (1f, 1f, 1f),
            };
            r.maxHealth = Mathf.RoundToInt(normalHp * mult.hp);
            r.attack = Mathf.Round(normalAtk * mult.atk);
            r.xp = Mathf.RoundToInt(normalXp * mult.xp);
            r.gold = Mathf.RoundToInt(normalGold * mult.xp);
            return r;
        }
    }
}
