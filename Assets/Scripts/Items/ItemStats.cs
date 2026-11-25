using UnityEngine;

namespace Aethvirse.Items
{
    /// <summary>Numbers of one item copy: rarity roll × upgrade on top of the base item.</summary>
    public static class ItemStats
    {
        public static Rarity RarityOf(ItemStack s, ItemData item) => s.rarity >= 0 ? (Rarity)s.rarity : item.rarity;

        public static float Multiplier(ItemStack s, LootRules rules)
        {
            float rarity = s.rarity >= 0 && s.rarity < rules.statMultiplier.Length ? rules.statMultiplier[s.rarity] : 1f;
            return rarity * (1f + rules.UpgradeBonus(s.upgrade));
        }

        public static float Attack(ItemStack s, ItemData item, int playerLevel, LootRules rules)
        {
            int level = s.itemLevel > 0 ? s.itemLevel : playerLevel;
            return item.AttackAt(level) * Multiplier(s, rules);
        }

        public static float Defense(ItemStack s, ItemData item, LootRules rules) =>
            (item.defense + item.defensePerLevel * Mathf.Max(s.itemLevel, 1)) * Multiplier(s, rules);

        public static float Bonus(ItemStack s, ItemData item, string stat, LootRules rules, ItemDatabase db = null)
        {
            float total = item.Bonus(stat) * Multiplier(s, rules);
            foreach (Affix a in s.affixes)
                if (a.stat == stat) total += a.value * (1f + rules.UpgradeBonus(s.upgrade));
            if (db == null) return total;
            foreach (string g in s.gems)
            {
                ItemData gem = db.Get(g);
                if (gem != null && gem.gemStat == stat) total += gem.gemValue;
            }
            ItemData ench = db.Get(s.enchant);
            if (ench != null && ench.gemStat == stat) total += ench.gemValue;
            return total;
        }

        /// <summary>The element a weapon enchant gives normal attacks (Physical = none).</summary>
        public static Combat.ElementType EnchantElement(ItemStack s, ItemDatabase db)
        {
            ItemData ench = s != null && db != null ? db.Get(s.enchant) : null;
            return ench != null ? ench.enchantElement : Combat.ElementType.Physical;
        }
    }
}
