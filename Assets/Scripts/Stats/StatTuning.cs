using UnityEngine;

namespace Aethvirse.Stats
{
    /// <summary>Global balancing constants (mirror aethvirse-dev AETHVIRSE_Balancing.xlsx, sheet Parameter; Godot stat_tuning).</summary>
    [CreateAssetMenu(menuName = "AETHVIRSE/Stat Tuning", fileName = "StatTuning")]
    public sealed class StatTuning : ScriptableObject
    {
        [Header("Leveling")]
        public float xpBase = 100f;
        public float xpExponent = 1.8f;
        public int levelCap = 1000;
        public int freePointsPerLevel = 5;
        public int skillPointsPerLevel = 1;

        [Header("Derived stats")]
        public float hpBase = 100f;
        public float hpPerVit = 20f;
        public float hpPerLevel = 10f;
        public float mpBase = 50f;
        public float mpPerInt = 10f;
        public float mpPerLevel = 3f;
        public float atkPerStr = 2f;
        public float atkPerAgi = 0.5f;
        public float matkPerInt = 2.5f;
        public float defPerVit = 1f;
        public float defPerStr = 0.3f;
        public float critBase = 0.05f;
        public float critPerSen = 0.002f;
        public float critPerAgi = 0.001f;
        public float critCap = 0.75f;
        public float critDmgBase = 1.5f;
        public float critDmgPerSen = 0.005f;
        public float aspdBase = 1f;
        public float aspdPerAgi = 0.002f;
        [Tooltip("Not in the sheet yet (engine defaults, tune later).")]
        public float mspdPerAgi = 0.001f;
        public float mspdCap = 1.3f;
        public float cdrPerInt = 0.001f;
        public float cdrCap = 0.4f;
        public float mpRegenBase = 2f;
        public float mpRegenPerInt = 0.05f;

        [Header("Enemy scaling")]
        public float defFactor = 0.5f;
        public float hitsToKillNormal = 5f;
        public float eliteHpMult = 4f;
        public float bossHpMult = 40f;
        public float normalHitsToKillHero = 12f;
        public float eliteAtkMult = 1.6f;
        public float bossAtkMult = 2.5f;
        public float enemyDefPerLevel = 1.5f;
        public float killsPerLevelBase = 15f;
        public float killsPerLevelGrowth = 0.5f;
        public float eliteXpMult = 5f;
        public float bossXpMult = 50f;
        public float goldBase = 5f;
        public float goldGrowth = 1.06f;
        [Tooltip("Average (Rare) weapon ATK per level: enemy scaling and the starter weapon.")]
        public float weaponAtkPerLevel = 4f;
    }
}
