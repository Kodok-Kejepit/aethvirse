using System;
using System.Collections.Generic;

namespace Aethvirse.Items
{
    [Serializable]
    public struct Affix
    {
        public string stat;
        public float value;
        public string name;
        public bool suffix;
    }

    /// <summary>One bag entry: an item id plus everything unique to this copy (rarity roll, affixes, upgrade, kills …).</summary>
    [Serializable]
    public sealed class ItemStack
    {
        public string itemId = "";
        public int count = 1;
        public bool identified = true;
        public string identifiedBy = "";
        public int kills;
        /// <summary>-1 = the item's own rarity; otherwise a rolled rarity (generated drops).</summary>
        public int rarity = -1;
        public int itemLevel;
        public List<Affix> affixes = new List<Affix>();
        public int upgrade;
        /// <summary>Gem sockets this copy rolled, and the gem item ids in them.</summary>
        public int sockets;
        public List<string> gems = new List<string>();
        /// <summary>Enchant scroll item id applied to this copy ("" = none).</summary>
        public string enchant = "";

        public static ItemStack Make(string id, int amount, ItemData item) =>
            new ItemStack { itemId = id, count = amount, identified = item == null || !item.unidentified };

        /// <summary>Unique copies never merge into other stacks.</summary>
        public bool IsUniqueInstance => rarity >= 0 || affixes.Count > 0 || upgrade > 0 || sockets > 0 || !string.IsNullOrEmpty(enchant);
    }
}
