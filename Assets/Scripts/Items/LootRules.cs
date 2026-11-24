using Aethvirse.Stats;
using UnityEngine;

namespace Aethvirse.Items
{
    /// <summary>Loot tuning (Balancing sheet "Loot"; Godot loot_rules). Index = rarity (Common … Mythic).</summary>
    [CreateAssetMenu(menuName = "AETHVIRSE/Loot Rules", fileName = "LootRules")]
    public sealed class LootRules : ScriptableObject
    {
        public float[] weightsNormal = { 700, 220, 65, 13, 2, 0 };
        public float[] weightsElite = { 300, 350, 250, 85, 14, 1 };
        public float[] weightsBoss = { 0, 100, 450, 330, 110, 10 };
        public float[] statMultiplier = { 1f, 1.15f, 1.35f, 1.6f, 2f, 2.6f };
        public int[] affixCount = { 0, 1, 2, 3, 4, 5 };
        [Tooltip("Chance of an equipment drop per tier (normal, elite, boss).")]
        public float[] equipDropChance = { 0.08f, 0.6f, 1f };

        [Header("Mystery box")]
        public int[] boxTiers = { 0, 2, 3, 4, 5, 6 };
        public float[] boxInitial = { 0.55f, 0.3f, 0.12f, 0.025f, 0.005f, 0f };
        public float[] boxUpgrade = { 0.4f, 0.25f, 0.15f, 0.1f, 0.05f, 0f };

        [Header("Upgrade +1 … +15")]
        public float upgradeBaseCost = 100f;
        public float upgradeCostGrowth = 1.35f;
        public float[] upgradeSuccess = { 1, 1, 1, 1, 1, 0.9f, 0.85f, 0.8f, 0.7f, 0.6f, 0.5f, 0.4f, 0.3f, 0.2f, 0.1f };
        [Tooltip("From this level a failed upgrade drops one level.")]
        public int upgradeRiskFrom = 11;
        public int upgradeMax = 15;
        public float upgradeBonusPerLevel = 0.04f;

        public float[] WeightsFor(EnemyTier tier) => tier switch
        {
            EnemyTier.Elite => weightsElite,
            EnemyTier.Boss => weightsBoss,
            _ => weightsNormal,
        };

        public int RollRarity(EnemyTier tier, System.Random rng) => WeightedIndex(WeightsFor(tier), rng);

        public static int WeightedIndex(float[] weights, System.Random rng)
        {
            float total = 0f;
            foreach (float w in weights) total += w;
            double pick = rng.NextDouble() * total;
            for (int i = 0; i < weights.Length; i++)
            {
                pick -= weights[i];
                if (pick < 0) return i;
            }
            return weights.Length - 1;
        }

        /// <summary>Tier steps of a mystery box: it may "upgrade" while opening (§9.3).</summary>
        public int[] RollMysteryBox(System.Random rng)
        {
            var steps = new System.Collections.Generic.List<int>();
            int index = WeightedIndex(boxInitial, rng);
            steps.Add(boxTiers[index]);
            while (index < boxTiers.Length - 1 && rng.NextDouble() < boxUpgrade[index])
            {
                index++;
                steps.Add(boxTiers[index]);
            }
            return steps.ToArray();
        }

        public int UpgradeCost(int targetLevel) => Mathf.RoundToInt(upgradeBaseCost * Mathf.Pow(upgradeCostGrowth, targetLevel - 1));
        public float UpgradeChance(int targetLevel) => upgradeSuccess[Mathf.Clamp(targetLevel, 1, upgradeMax) - 1];
        public float UpgradeBonus(int level) => level * upgradeBonusPerLevel + Mathf.Max(0, level - 10) * upgradeBonusPerLevel;
    }
}
