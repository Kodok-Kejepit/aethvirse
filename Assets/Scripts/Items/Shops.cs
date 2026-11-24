using System.Collections.Generic;
using Aethvirse.World;
using UnityEngine;

namespace Aethvirse.Items
{
    /// <summary>
    /// The Codex shop (§9.3 secret vendor flavour): 8 gear offers (3 Rare, 2 Epic, 2 Legendary, 1 Mythic) that
    /// change every game day, seeded by day + level so they stay stable within a day. Bought offers are flagged.
    /// </summary>
    public static class CodexShop
    {
        static readonly Rarity[] Offers = { Rarity.Rare, Rarity.Rare, Rarity.Rare, Rarity.Epic, Rarity.Epic, Rarity.Legendary, Rarity.Legendary, Rarity.Mythic };
        static readonly int[] PricePerLevel = { 8, 15, 70, 200, 650, 2200 };
        public const int MinLevel = 5;

        public static int Price(Rarity rarity, int itemLevel) => PricePerLevel[Mathf.Clamp((int)rarity, 0, PricePerLevel.Length - 1)] * Mathf.Max(itemLevel, 1);

        public struct Offer
        {
            public ItemStack stack;
            public int price;
            public bool sold;
        }

        public static List<Offer> List(int day, int playerLevel, ItemDatabase db, LootRules rules, WorldFlags flags)
        {
            int level = Mathf.Max(playerLevel, MinLevel);
            var gen = new EquipmentGenerator(db, rules, new System.Random(day * 7919 + level * 31));
            var list = new List<Offer>();
            for (int i = 0; i < Offers.Length; i++)
                list.Add(new Offer { stack = gen.Generate(level, (int)Offers[i]), price = Price(Offers[i], level), sold = flags.Has(SoldFlag(day, i)) });
            return list;
        }

        public static string SoldFlag(int day, int index) => $"codex_shop_{day}_{index}";
    }

    /// <summary>A plain shop's stock line (Quartermaster): unlimited items at a fixed price.</summary>
    [System.Serializable]
    public struct ShopEntry
    {
        public string itemId;
        public int price;
    }
}
