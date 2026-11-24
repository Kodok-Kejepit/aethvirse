using System.Collections.Generic;
using Aethvirse.Items;
using Aethvirse.Save;
using Aethvirse.Stats;
using Aethvirse.World;
using NUnit.Framework;
using UnityEngine;

namespace Aethvirse.Tests
{
    public sealed class ItemTests
    {
        ItemDatabase _db;
        LootRules _rules;
        readonly List<Object> _made = new List<Object>();

        sealed class Wallet : ItemActions.IWallet
        {
            public int Gold { get; set; }
            public void AddGold(int amount) => Gold += amount;
            public bool SpendGold(int amount) { if (amount > Gold) return false; Gold -= amount; return true; }
        }

        ItemData Make(string id, ItemKind kind, int maxStack = 1, EquipSlot slot = EquipSlot.None, bool dropBase = false)
        {
            var i = ScriptableObject.CreateInstance<ItemData>();
            i.id = id; i.displayName = id; i.kind = kind; i.maxStack = maxStack; i.slot = slot; i.isDropBase = dropBase; i.sellPrice = 10;
            _db.items.Add(i);
            _made.Add(i);
            return i;
        }

        [SetUp]
        public void SetUp()
        {
            _db = ScriptableObject.CreateInstance<ItemDatabase>();
            _rules = ScriptableObject.CreateInstance<LootRules>();
            _made.Add(_db);
            _made.Add(_rules);
            Make("herb", ItemKind.Material, 99);
            Make("scrap_metal", ItemKind.Material, 99);
            Make("potion_small", ItemKind.Consumable, 20).healAmount = 150;
            Make("mystery_box", ItemKind.Consumable, 20).opensMysteryBox = true;
            Make("gate_key", ItemKind.Key, 9);
            Make("sword", ItemKind.Weapon, 1, EquipSlot.Weapon, true).attackPerLevel = 4f;
            Make("ring", ItemKind.Accessory, 1, EquipSlot.Ring, true).statBonuses.Add(new StatBonus(StatKeys.Sen, 1f));
            Make("helm", ItemKind.Armor, 1, EquipSlot.Helm, true).defensePerLevel = 0.6f;
            Make("cursed_blade", ItemKind.Weapon, 1, EquipSlot.Weapon).cursed = true;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _made) Object.DestroyImmediate(o);
            _made.Clear();
        }

        [Test]
        public void Add_StacksAndOverflows()
        {
            var inv = new Inventory(_db);
            Assert.That(inv.Add("herb", 150), Is.EqualTo(0));
            Assert.That(inv.stacks.Count, Is.EqualTo(2), "99 + 51");
            Assert.That(inv.CountOf("herb"), Is.EqualTo(150));
            Assert.That(inv.Add("nope"), Is.EqualTo(1), "unknown items do not fit");
            for (int i = 0; i < Inventory.Capacity; i++) inv.Add("sword");
            Assert.That(inv.stacks.Count, Is.EqualTo(Inventory.Capacity));
            Assert.That(inv.Add("sword"), Is.EqualTo(1), "bag full");
        }

        [Test]
        public void Remove_TakesFromStacksAndRefusesShortfall()
        {
            var inv = new Inventory(_db);
            inv.Add("herb", 10);
            Assert.That(inv.Remove("herb", 11), Is.False);
            Assert.That(inv.Remove("herb", 10), Is.True);
            Assert.That(inv.stacks.Count, Is.EqualTo(0));
        }

        [Test]
        public void Equip_UsesSecondRingAndSwaps()
        {
            var inv = new Inventory(_db);
            inv.Add("ring", 2);
            inv.Equip(inv.stacks[0]);
            inv.Equip(inv.stacks[0]);
            Assert.That(inv.GetEquipped(EquipSlot.Ring), Is.Not.Null);
            Assert.That(inv.GetEquipped(EquipSlot.Ring2), Is.Not.Null);
            inv.Add("sword", 2);
            inv.Equip(inv.stacks[0]);
            inv.Equip(inv.stacks[0]);
            Assert.That(inv.stacks.Count, Is.EqualTo(1), "the first sword went back to the bag");
        }

        [Test]
        public void Cursed_CannotBeTakenOff()
        {
            var inv = new Inventory(_db);
            inv.Add("cursed_blade");
            Assert.That(inv.Equip(inv.stacks[0]), Is.True);
            Assert.That(inv.Unequip(EquipSlot.Weapon), Is.False);
        }

        [Test]
        public void Generator_RollsRarityLevelAndAffixes()
        {
            var gen = new EquipmentGenerator(_db, _rules, new System.Random(5));
            for (int rarity = 0; rarity <= (int)Rarity.Mythic; rarity++)
            {
                ItemStack s = gen.Generate(10, rarity);
                Assert.That(s.rarity, Is.EqualTo(rarity));
                Assert.That(s.itemLevel, Is.EqualTo(10));
                Assert.That(s.affixes.Count, Is.EqualTo(_rules.affixCount[rarity]));
                Assert.That(_db.Get(s.itemId).isDropBase, Is.True);
            }
        }

        [Test]
        public void Stats_ScaleWithRarityAndUpgrade()
        {
            ItemData sword = _db.Get("sword");
            var common = new ItemStack { itemId = "sword", rarity = 0, itemLevel = 10 };
            var mythic = new ItemStack { itemId = "sword", rarity = (int)Rarity.Mythic, itemLevel = 10 };
            Assert.That(ItemStats.Attack(common, sword, 1, _rules), Is.EqualTo(40f).Within(0.01f));
            Assert.That(ItemStats.Attack(mythic, sword, 1, _rules), Is.EqualTo(40f * 2.6f).Within(0.01f));
            common.upgrade = 5;
            Assert.That(ItemStats.Attack(common, sword, 1, _rules), Is.EqualTo(40f * 1.2f).Within(0.01f));
        }

        [Test]
        public void Upgrade_CostsGoldAndRisksDowngradeLate()
        {
            ItemData sword = _db.Get("sword");
            var wallet = new Wallet { Gold = 100000 };
            var s = new ItemStack { itemId = "sword", rarity = 2, itemLevel = 5 };
            for (int i = 0; i < 5; i++) Assert.That(ItemActions.Upgrade(s, sword, _rules, wallet, new System.Random(i)), Is.EqualTo(UpgradeResult.Success), "+1…+5 always succeed");
            Assert.That(s.upgrade, Is.EqualTo(5));
            s.upgrade = 14;
            _rules.upgradeSuccess[14] = 0f;
            Assert.That(ItemActions.Upgrade(s, sword, _rules, wallet, new System.Random(1)), Is.EqualTo(UpgradeResult.FailedDowngraded));
            Assert.That(s.upgrade, Is.EqualTo(13));
            var broke = new Wallet { Gold = 0 };
            Assert.That(ItemActions.Upgrade(s, sword, _rules, broke, new System.Random(1)), Is.EqualTo(UpgradeResult.NoGold));
        }

        [Test]
        public void MysteryBox_StepsOnlyGoUp()
        {
            var rng = new System.Random(11);
            for (int i = 0; i < 200; i++)
            {
                int[] steps = _rules.RollMysteryBox(rng);
                for (int k = 1; k < steps.Length; k++) Assert.That(steps[k], Is.GreaterThan(steps[k - 1]));
            }
        }

        [Test]
        public void MysteryBox_OpensIntoGear()
        {
            var inv = new Inventory(_db);
            inv.Add("mystery_box");
            var (steps, item, _) = ItemActions.OpenMysteryBox(inv, inv.stacks[0], 7, _rules, new System.Random(3));
            Assert.That(steps.Length, Is.GreaterThan(0));
            Assert.That(item, Is.Not.Null);
            Assert.That(inv.CountOf("mystery_box"), Is.EqualTo(0));
            Assert.That(inv.stacks.Contains(item), Is.True);
        }

        [Test]
        public void Craft_ConsumesInputsAndGold()
        {
            var inv = new Inventory(_db);
            var wallet = new Wallet { Gold = 50 };
            var r = new Recipe { inputIds = { "herb" }, inputCounts = { 2 }, output = "potion_small", goldCost = 30 };
            inv.Add("herb", 3);
            Assert.That(ItemActions.Craft(inv, r, wallet), Is.True);
            Assert.That(inv.CountOf("herb"), Is.EqualTo(1));
            Assert.That(inv.CountOf("potion_small"), Is.EqualTo(1));
            Assert.That(wallet.Gold, Is.EqualTo(20));
            Assert.That(ItemActions.CanCraft(inv, r, wallet), Is.False);
        }

        [Test]
        public void SellAndSalvage()
        {
            var inv = new Inventory(_db);
            var wallet = new Wallet();
            inv.Add("herb", 2);
            Assert.That(ItemActions.Sell(inv, inv.stacks[0], _rules, wallet), Is.GreaterThan(0));
            Assert.That(inv.CountOf("herb"), Is.EqualTo(1));
            var gear = new ItemStack { itemId = "helm", rarity = (int)Rarity.Epic, itemLevel = 3 };
            inv.AddStack(gear);
            Assert.That(ItemActions.Salvage(inv, gear), Is.EqualTo(5));
            Assert.That(inv.CountOf("scrap_metal"), Is.EqualTo(5));
        }

        [Test]
        public void BossesAlwaysDropGear()
        {
            var rng = new System.Random(2);
            for (int i = 0; i < 50; i++)
            {
                List<ItemStack> drops = ItemActions.RollDrops(10, EnemyTier.Boss, _db, _rules, rng);
                Assert.That(drops.Exists(d => d.rarity >= (int)Rarity.Uncommon), Is.True);
            }
        }

        [Test]
        public void CodexShop_StableWithinADay()
        {
            var flags = new WorldFlags();
            var a = CodexShop.List(4, 12, _db, _rules, flags);
            var b = CodexShop.List(4, 12, _db, _rules, flags);
            Assert.That(a.Count, Is.EqualTo(8));
            for (int i = 0; i < a.Count; i++) Assert.That(a[i].stack.itemId, Is.EqualTo(b[i].stack.itemId));
            flags.SetFlag(CodexShop.SoldFlag(4, 2));
            Assert.That(CodexShop.List(4, 12, _db, _rules, flags)[2].sold, Is.True);
        }

        [Test]
        public void Inventory_SurvivesASave()
        {
            var inv = new Inventory(_db);
            inv.Add("herb", 5);
            inv.AddStack(new ItemStack { itemId = "sword", rarity = 4, itemLevel = 9, affixes = { new Affix { stat = StatKeys.Str, value = 3f, name = "Brutal" } } });
            inv.Equip(inv.stacks[1]);
            var data = new SaveData();
            data.player.inventory = inv;
            data.player.hasInventory = true;
            SaveData back = SaveData.FromJson(data.ToJson());
            Assert.That(back.player.hasInventory, Is.True);
            Inventory loaded = back.player.inventory;
            loaded.Db = _db;
            Assert.That(loaded.CountOf("herb"), Is.EqualTo(5));
            ItemStack sword = loaded.GetEquipped(EquipSlot.Weapon);
            Assert.That(sword.rarity, Is.EqualTo(4));
            Assert.That(sword.affixes[0].name, Is.EqualTo("Brutal"));
            Assert.That(loaded.DisplayName(sword), Is.EqualTo("Brutal sword"));
        }
    }
}
