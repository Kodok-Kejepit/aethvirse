using System;
using UnityEngine;

namespace Aethvirse.Stats
{
    /// <summary>Level / XP / free stat points / skill points. Pure and serializable (saves come in M5).</summary>
    [Serializable]
    public sealed class Progression
    {
        public int level = 1;
        public int xp;
        public int freePoints;
        public int skillPoints;
        public StatBlock allocated;

        [NonSerialized] StatTuning _tuning;

        public Progression() { }
        public Progression(StatTuning tuning) => _tuning = tuning;

        public StatTuning Tuning { get => _tuning; set => _tuning = value; }

        /// <summary>xp_needed(level) = round(xpBase × level^xpExponent) (§8); 0 at the cap.</summary>
        public static int XpNeeded(int level, StatTuning t) =>
            level >= t.levelCap ? 0 : Mathf.RoundToInt(t.xpBase * Mathf.Pow(level, t.xpExponent));

        public int XpToNext => XpNeeded(level, _tuning);

        /// <summary>Adds XP and levels up as often as needed. Returns the number of levels gained.</summary>
        public int AddXp(int amount)
        {
            if (amount <= 0 || level >= _tuning.levelCap) return 0;
            xp += amount;
            int gained = 0;
            while (level < _tuning.levelCap && xp >= XpToNext)
            {
                xp -= XpToNext;
                level++;
                gained++;
                freePoints += _tuning.freePointsPerLevel;
                skillPoints += _tuning.skillPointsPerLevel;
            }
            if (level >= _tuning.levelCap) xp = 0;
            return gained;
        }

        public bool Allocate(Stat stat, int points = 1)
        {
            if (points <= 0 || points > freePoints) return false;
            freePoints -= points;
            allocated[stat] += points;
            return true;
        }

        /// <summary>Primary stats = class base + class growth × (level − 1) + allocated points.</summary>
        public StatBlock PrimaryStats(StatBlock baseStats, StatBlock growth) =>
            baseStats.Plus(growth, level - 1).Plus(allocated);
    }
}
