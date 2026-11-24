using System.Collections.Generic;
using Aethvirse.Stats;
using UnityEngine;

namespace Aethvirse.Items
{
    public enum UpgradeResult { Success, Failed, FailedDowngraded, Maxed, NoGold, NotEquipment }

    /// <summary>A crafting recipe: inputs + gold → output.</summary>
    [System.Serializable]
    public sealed class Recipe
    {
        public string id = "";
        public List<string> inputIds = new List<string>();
        public List<int> inputCounts = new List<int>();
        public string output = "";
        public int outputCount = 1;
        public int goldCost;
    }

    /// <summary>
    /// Item operations (port of the Godot ItemActions / LootDropper): drops, mystery boxes, selling, salvage,
    /// upgrades and crafting. Gold goes through the <paramref name="gold"/> wallet so tests need no scene.
    /// </summary>
    public static class ItemActions
    {
        public const float HerbChance = 0.3f;
        public const float ScrapChance = 0.2f;
        public const float PotionChance = 0.08f;
        static readonly float[] BoxChance = { 0.02f, 0.2f, 0.5f };
        static readonly int[] SalvageScrap = { 1, 2, 3, 5, 8, 12 };
        const float GateKeyChance = 0.3f;

        public interface IWallet
        {
            int Gold { get; }
            void AddGold(int amount);
            bool SpendGold(int amount);
        }

        /// <summary>What an enemy drops (§11): maybe gear by tier, herbs, scrap, a potion, a mystery box.</summary>
        public static List<ItemStack> RollDrops(int level, EnemyTier tier, ItemDatabase db, LootRules rules, System.Random rng)
        {
            var drops = new List<ItemStack>();
            if (rng.NextDouble() < rules.equipDropChance[(int)tier])
            {
                ItemStack gear = new EquipmentGenerator(db, rules, rng).Generate(level, rules.RollRarity(tier, rng));
                if (gear != null) drops.Add(gear);
            }
            if (rng.NextDouble() < HerbChance) Add(drops, db, "herb", 1 + rng.Next(2));
            if (rng.NextDouble() < ScrapChance) Add(drops, db, "scrap_metal", 1);
            if (rng.NextDouble() < PotionChance) Add(drops, db, "potion_small", 1);
            if (rng.NextDouble() < BoxChance[(int)tier]) Add(drops, db, "mystery_box", 1);
            return drops;
        }

        static void Add(List<ItemStack> list, ItemDatabase db, string id, int count)
        {
            ItemData item = db.Get(id);
            if (item != null) list.Add(ItemStack.Make(id, count, item));
        }

        /// <summary>Opens a mystery box: tier steps (it can upgrade while opening), the item, maybe a Gate Key.</summary>
        public static (int[] steps, ItemStack item, bool key) OpenMysteryBox(Inventory inv, ItemStack box, int level, LootRules rules, System.Random rng)
        {
            int[] steps = rules.RollMysteryBox(rng);
            int tier = Mathf.Min(steps[steps.Length - 1], (int)Rarity.Mythic);
            ItemStack item = new EquipmentGenerator(inv.Db, rules, rng).Generate(level, tier);
            inv.TakeOne(box);
            if (item != null) inv.AddStack(item);
            bool key = rng.NextDouble() < GateKeyChance && inv.Db.Get("gate_key") != null;
            if (key) inv.Add("gate_key");
            return (steps, item, key);
        }

        public static int SellPrice(Inventory inv, ItemStack s, LootRules rules)
        {
            ItemData item = inv.Db.Get(s.itemId);
            if (item == null || !item.canSell) return 0;
            return Mathf.RoundToInt(item.sellPrice * ItemStats.Multiplier(s, rules) * Mathf.Max(s.itemLevel, 1) / 2f + item.sellPrice);
        }

        public static int Sell(Inventory inv, ItemStack s, LootRules rules, IWallet wallet)
        {
            int price = SellPrice(inv, s, rules);
            if (price <= 0 || !inv.stacks.Contains(s)) return 0;
            inv.TakeOne(s);
            wallet.AddGold(price);
            return price;
        }

        /// <summary>Breaks gear into scrap metal by rarity.</summary>
        public static int Salvage(Inventory inv, ItemStack s)
        {
            ItemData item = inv.Db.Get(s.itemId);
            if (item == null || !item.IsEquipment || !inv.stacks.Contains(s) || !item.canSell) return 0;
            int scrap = SalvageScrap[Mathf.Clamp((int)ItemStats.RarityOf(s, item), 0, SalvageScrap.Length - 1)];
            inv.stacks.Remove(s);
            inv.Add("scrap_metal", scrap);
            return scrap;
        }

        public static UpgradeResult Upgrade(ItemStack s, ItemData item, LootRules rules, IWallet wallet, System.Random rng)
        {
            if (item == null || !item.IsEquipment) return UpgradeResult.NotEquipment;
            if (s.upgrade >= rules.upgradeMax) return UpgradeResult.Maxed;
            int target = s.upgrade + 1;
            if (!wallet.SpendGold(rules.UpgradeCost(target))) return UpgradeResult.NoGold;
            if (rng.NextDouble() < rules.UpgradeChance(target))
            {
                s.upgrade = target;
                return UpgradeResult.Success;
            }
            if (target >= rules.upgradeRiskFrom)
            {
                s.upgrade = Mathf.Max(s.upgrade - 1, 0);
                return UpgradeResult.FailedDowngraded;
            }
            return UpgradeResult.Failed;
        }

        public static bool CanCraft(Inventory inv, Recipe r, IWallet wallet)
        {
            if (wallet.Gold < r.goldCost) return false;
            for (int i = 0; i < r.inputIds.Count; i++) if (inv.CountOf(r.inputIds[i]) < r.inputCounts[i]) return false;
            return true;
        }

        public static bool Craft(Inventory inv, Recipe r, IWallet wallet)
        {
            if (!CanCraft(inv, r, wallet)) return false;
            for (int i = 0; i < r.inputIds.Count; i++) inv.Remove(r.inputIds[i], r.inputCounts[i]);
            wallet.SpendGold(r.goldCost);
            inv.Add(r.output, r.outputCount);
            return true;
        }
    }
}
