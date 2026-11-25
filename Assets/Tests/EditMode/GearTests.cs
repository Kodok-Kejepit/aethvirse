using System.Collections.Generic;
using System.Linq;
using Aethvirse.Items;
using NUnit.Framework;
using UnityEditor;

namespace Aethvirse.Tests
{
    public sealed class GearTests
    {
        static ItemDatabase Db => AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/Data/Items/ItemDatabase.asset");

        [Test]
        public void Catalogue_IsLarge_AndEveryItemHasTextAndAUniqueId()
        {
            ItemDatabase db = Db;
            Assert.That(db.items.Count, Is.GreaterThanOrEqualTo(300), "owner: many items");
            Assert.That(db.items.Select(i => i.id).Distinct().Count(), Is.EqualTo(db.items.Count));
            foreach (ItemData i in db.items) Assert.That(i.description.Length, Is.GreaterThan(5), i.id);
            Assert.That(db.items.Count(i => i.kind == ItemKind.Gem), Is.EqualTo(28));
            Assert.That(db.items.Count(i => i.kind == ItemKind.EnchantScroll), Is.GreaterThanOrEqualTo(14));
            Assert.That(db.items.Count(i => i.id.StartsWith("u_")), Is.GreaterThanOrEqualTo(20));
        }

        [Test]
        public void EverySet_HasSixPiecesInDifferentSlots_AndThreeTiers()
        {
            ItemDatabase db = Db;
            var sets = AssetDatabase.LoadAssetAtPath<GearSetDatabase>("Assets/Resources/GearSetDatabase.asset");
            Assert.That(sets.sets.Count, Is.GreaterThanOrEqualTo(8));
            foreach (GearSet s in sets.sets)
            {
                var pieces = db.items.Where(i => i.setId == s.id).ToList();
                Assert.That(pieces.Count, Is.EqualTo(6), s.id);
                Assert.That(pieces.Select(p => p.slot).Distinct().Count(), Is.EqualTo(6), s.id);
                Assert.That(s.tiers.Select(t => t.pieces), Is.EquivalentTo(new[] { 2, 4, 6 }), s.id);
            }
        }

        [Test]
        public void SetBonus_CountsOnlyReachedTiers()
        {
            var db = UnityEngine.ScriptableObject.CreateInstance<GearSetDatabase>();
            var set = new GearSet { id = "x" };
            set.tiers.Add(new SetTier { pieces = 2, stat = StatKeys.AtkPct, value = 0.05f });
            set.tiers.Add(new SetTier { pieces = 4, stat = StatKeys.AtkPct, value = 0.1f });
            db.sets.Add(set);
            Assert.That(db.Bonus(StatKeys.AtkPct, new Dictionary<string, int> { { "x", 1 } }), Is.EqualTo(0f));
            Assert.That(db.Bonus(StatKeys.AtkPct, new Dictionary<string, int> { { "x", 3 } }), Is.EqualTo(0.05f).Within(1e-5));
            Assert.That(db.Bonus(StatKeys.AtkPct, new Dictionary<string, int> { { "x", 6 } }), Is.EqualTo(0.15f).Within(1e-5));
        }

        [Test]
        public void Gems_SocketAndAddTheirStat_FuseUpward()
        {
            ItemDatabase db = Db;
            var bag = new Inventory(db);
            var gear = new ItemStack { itemId = "hunter_helm", rarity = 3, itemLevel = 10, sockets = 1 };
            bag.Add("gem_ruby_1", 2);
            Assert.That(ItemActions.SocketGem(bag, gear, "gem_ruby_1"), Is.True);
            Assert.That(ItemActions.SocketGem(bag, gear, "gem_ruby_1"), Is.False, "only one socket");
            float str = ItemStats.Bonus(gear, db.Get("hunter_helm"), StatKeys.Str, AssetDatabase.LoadAssetAtPath<LootRules>("Assets/Data/Balancing/LootRules.asset"), db);
            Assert.That(str, Is.GreaterThanOrEqualTo(db.Get("gem_ruby_1").gemValue));
            Assert.That(db.recipes.Any(r => r.output == "gem_ruby_2" && r.inputIds[0] == "gem_ruby_1" && r.inputCounts[0] == 3), Is.True);
            Assert.That(db.recipes.Any(r => r.output == "gem_bloodstone_4"), Is.True);
        }

        [Test]
        public void Reforge_KeepsRarityLevelAndSockets()
        {
            ItemDatabase db = Db;
            var rules = AssetDatabase.LoadAssetAtPath<LootRules>("Assets/Data/Balancing/LootRules.asset");
            var rng = new System.Random(5);
            ItemStack gear = new EquipmentGenerator(db, rules, rng).Generate(120, 4);
            int sockets = gear.sockets, level = gear.itemLevel;
            new EquipmentGenerator(db, rules, rng).RollAffixes(gear, gear.rarity);
            Assert.That(gear.rarity, Is.EqualTo(4));
            Assert.That(gear.itemLevel, Is.EqualTo(level));
            Assert.That(gear.sockets, Is.EqualTo(sockets));
            Assert.That(gear.affixes.Count, Is.EqualTo(rules.affixCount[4]));
            Assert.That(EquipmentGenerator.RollSockets(0, rng), Is.EqualTo(0), "commons have no sockets");
            for (int i = 0; i < 50; i++) Assert.That(EquipmentGenerator.RollSockets(5, rng), Is.InRange(2, 3));
        }

        [Test]
        public void Chest_MimicIsStableForTheDay()
        {
            Assert.That(Chest.IsMimic("r1_ashfall_0", 3), Is.EqualTo(Chest.IsMimic("r1_ashfall_0", 3)));
            int mimics = Enumerable.Range(0, 400).Count(d => Chest.IsMimic("r2_verdant_1", d));
            Assert.That(mimics, Is.InRange(40, 130), "about one in five");
        }

        [Test]
        public void Stash_HoldsTwoHundred()
        {
            var stash = new Inventory(Db) { capacity = Inventory.StashCapacity };
            for (int i = 0; i < 205; i++) stash.AddStack(new ItemStack { itemId = "hunter_helm", rarity = 1 });
            Assert.That(stash.stacks.Count, Is.EqualTo(200));
        }
    }
}
