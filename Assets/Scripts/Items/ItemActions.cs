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
            if (rng.NextDouble() < RuneChance[(int)tier]) AddRune(drops, db, rng);
            if (rng.NextDouble() < GemChance[(int)tier])
            {
                // Grade by tier: normal enemies chip, bosses can drop royal gems.
                int grade = Mathf.Min(4, 1 + (int)tier + (rng.NextDouble() < 0.15 ? 1 : 0));
                string[] types = { "ruby", "emerald", "sapphire", "diamond", "topaz", "onyx", "bloodstone" };
                Add(drops, db, $"gem_{types[rng.Next(types.Length)]}_{grade}", 1);
            }
            if (rng.NextDouble() < ScrollChance[(int)tier]) AddRandom(drops, db, rng, i => i.kind == ItemKind.EnchantScroll && i.rarity <= Rarity.Epic);
            if (rng.NextDouble() < SetChance[(int)tier]) AddRandom(drops, db, rng, i => !string.IsNullOrEmpty(i.setId) && i.minLevel <= level + 10);
            if (rng.NextDouble() < UniqueChance[(int)tier]) AddRandom(drops, db, rng, i => i.id.StartsWith("u_") && i.minLevel <= level + 10);
            return drops;
        }

        // Chances by tier (normal, elite, boss).
        public static readonly float[] GemChance = { 0.025f, 0.15f, 0.6f };
        public static readonly float[] ScrollChance = { 0.003f, 0.04f, 0.2f };
        public static readonly float[] SetChance = { 0.002f, 0.03f, 0.25f };
        public static readonly float[] UniqueChance = { 0.0005f, 0.008f, 0.08f };

        static void AddRandom(List<ItemStack> list, ItemDatabase db, System.Random rng, System.Predicate<ItemData> match)
        {
            var pool = new List<ItemData>();
            foreach (ItemData i in db.items) if (i != null && match(i)) pool.Add(i);
            if (pool.Count == 0) return;
            ItemData pick = pool[rng.Next(pool.Count)];
            list.Add(ItemStack.Make(pick.id, 1, pick));
        }

        /// <summary>Runes are rare drops: elites and bosses far more often (normal, elite, boss).</summary>
        public static readonly float[] RuneChance = { 0.004f, 0.06f, 0.35f };

        /// <summary>A random rune, weighted toward the common ones (rarer runes are 3× rarer per tier).</summary>
        static void AddRune(List<ItemStack> list, ItemDatabase db, System.Random rng)
        {
            var runes = new List<ItemData>();
            float total = 0f;
            foreach (ItemData i in db.items)
                if (i != null && i.kind == ItemKind.Rune) { runes.Add(i); total += RuneWeight(i.rarity); }
            if (runes.Count == 0) return;
            double roll = rng.NextDouble() * total;
            foreach (ItemData i in runes)
            {
                roll -= RuneWeight(i.rarity);
                if (roll <= 0) { list.Add(ItemStack.Make(i.id, 1, i)); return; }
            }
        }

        static float RuneWeight(Rarity r) => Mathf.Pow(3f, (int)Rarity.Mythic - Mathf.Min((int)r, (int)Rarity.Mythic));

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
            Core.GameEvents.RaiseCrafted(r.output);
            return true;
        }

        // --- Gems, enchants, reforging ------------------------------------------------------------

        /// <summary>Puts a gem from the bag into a free socket of a gear copy.</summary>
        public static bool SocketGem(Inventory bag, ItemStack gear, string gemId)
        {
            ItemData gem = bag.Db?.Get(gemId);
            if (gem == null || gem.kind != ItemKind.Gem || gear.gems.Count >= gear.sockets) return false;
            if (!bag.Remove(gemId, 1)) return false;
            gear.gems.Add(gemId);
            bag.NotifyChanged();
            return true;
        }

        /// <summary>Pulling gems out costs gold; the gems come back.</summary>
        public static int UnsocketCost(ItemStack gear) => 100 * gear.gems.Count * (1 + Mathf.Max(0, gear.rarity));

        public static bool UnsocketAll(Inventory bag, ItemStack gear, IWallet wallet)
        {
            if (gear.gems.Count == 0 || !wallet.SpendGold(UnsocketCost(gear))) return false;
            foreach (string g in gear.gems) bag.Add(g);
            gear.gems.Clear();
            bag.NotifyChanged();
            return true;
        }

        /// <summary>Applies an enchant scroll (weapon scrolls on weapons, armour scrolls on the rest). Replaces the old enchant.</summary>
        public static bool Enchant(Inventory bag, ItemStack gear, ItemData gearItem, string scrollId)
        {
            ItemData scroll = bag.Db?.Get(scrollId);
            if (scroll == null || scroll.kind != ItemKind.EnchantScroll || !gearItem.IsEquipment) return false;
            if (scroll.enchantWeapon != (gearItem.slot == EquipSlot.Weapon)) return false;
            if (!bag.Remove(scrollId, 1)) return false;
            gear.enchant = scrollId;
            bag.NotifyChanged();
            return true;
        }

        public static int ReforgeCost(ItemStack gear) => 150 * (1 + Mathf.Max(0, gear.rarity)) * (1 + Mathf.Max(0, gear.rarity));

        /// <summary>Rerolls the affixes of a generated copy for gold and 2 scrap (rarity, level, sockets stay).</summary>
        public static bool Reforge(Inventory bag, ItemStack gear, LootRules rules, IWallet wallet, System.Random rng)
        {
            if (gear.rarity < 0 || bag.CountOf("scrap_metal") < 2 || !wallet.SpendGold(ReforgeCost(gear))) return false;
            bag.Remove("scrap_metal", 2);
            new EquipmentGenerator(bag.Db, rules, rng).RollAffixes(gear, gear.rarity);
            bag.NotifyChanged();
            return true;
        }
    }
}
