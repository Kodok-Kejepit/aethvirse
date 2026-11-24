using System.Collections.Generic;
using UnityEngine;

namespace Aethvirse.Items
{
    /// <summary>Random gear from drop bases: rarity, item level and prefix / suffix affixes (§11).</summary>
    public sealed class EquipmentGenerator
    {
        // stat, name, base value, value per level, suffix?
        static readonly (string stat, string name, float baseValue, float perLevel, bool suffix)[] Affixes =
        {
            (StatKeys.Crit, "Keen", 0.01f, 0.0006f, false),
            (StatKeys.Str, "Brutal", 1f, 0.35f, false),
            (StatKeys.Agi, "Swift", 1f, 0.35f, false),
            (StatKeys.Int, "Wise", 1f, 0.35f, false),
            (StatKeys.Vit, "Sturdy", 1f, 0.35f, false),
            (StatKeys.Sen, "Watchful", 1f, 0.3f, false),
            (StatKeys.Lifesteal, "of Thirst", 0.01f, 0.0004f, true),
            (StatKeys.Vit, "of the Bear", 1.5f, 0.4f, true),
            (StatKeys.Sen, "of the Hawk", 1.5f, 0.35f, true),
            (StatKeys.Str, "of Ruin", 1.5f, 0.4f, true),
            (StatKeys.Agi, "of Echoes", 1.5f, 0.4f, true),
        };
        const float RollMin = 0.8f, RollMax = 1.2f;

        readonly ItemDatabase _db;
        readonly LootRules _rules;
        readonly System.Random _rng;

        public EquipmentGenerator(ItemDatabase db, LootRules rules, System.Random rng)
        {
            _db = db;
            _rules = rules;
            _rng = rng;
        }

        public ItemStack Generate(int level, int rarity, EquipSlot onlySlot = EquipSlot.None)
        {
            var bases = new List<ItemData>();
            foreach (ItemData b in _db.DropBases()) if (onlySlot == EquipSlot.None || b.slot == onlySlot) bases.Add(b);
            if (bases.Count == 0) return null;
            ItemData pick = bases[_rng.Next(bases.Count)];
            rarity = Mathf.Clamp(rarity, 0, (int)Rarity.Mythic);
            var stack = ItemStack.Make(pick.id, 1, pick);
            stack.rarity = rarity;
            stack.itemLevel = Mathf.Max(1, level);
            int count = _rules.affixCount[rarity];
            bool prefix = false, suffix = false;
            var used = new HashSet<int>();
            for (int i = 0; i < count && used.Count < Affixes.Length; i++)
            {
                int index = _rng.Next(Affixes.Length);
                if (!used.Add(index)) { i--; continue; }
                var a = Affixes[index];
                float roll = RollMin + (float)_rng.NextDouble() * (RollMax - RollMin);
                // Only the first prefix and the first suffix show in the name.
                bool named = a.suffix ? !suffix : !prefix;
                if (a.suffix) suffix = true; else prefix = true;
                stack.affixes.Add(new Affix
                {
                    stat = a.stat,
                    value = (a.baseValue + a.perLevel * stack.itemLevel) * roll,
                    name = named ? a.name : "",
                    suffix = a.suffix,
                });
            }
            return stack;
        }
    }
}
