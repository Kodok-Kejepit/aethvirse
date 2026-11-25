using System;
using System.Collections.Generic;
using UnityEngine;

namespace Aethvirse.Items
{
    public enum ItemKind { Consumable, Food, Weapon, Accessory, Armor, Key, Material, Currency, SkillBook, Costume, Rune }
    public enum Rarity { Common, Uncommon, Rare, Epic, Legendary, Mythic, Unknown }
    /// <summary>Equipment slots (§11). Append only: saves store the number.</summary>
    public enum EquipSlot { None, Weapon, OffHand, Helm, Armor, Gloves, Boots, Ring, Necklace, RuneCore, Ring2, Costume }

    /// <summary>Stat keys used by bonuses and affixes.</summary>
    public static class StatKeys
    {
        public const string Str = "str", Agi = "agi", Int = "int", Vit = "vit", Sen = "sen", Crit = "crit", Lifesteal = "lifesteal";
    }

    [Serializable]
    public struct StatBonus
    {
        public string stat;
        public float value;
        public StatBonus(string stat, float value) { this.stat = stat; this.value = value; }
    }

    /// <summary>An item definition (port of the Godot ItemData). Instances in bags are <see cref="ItemStack"/>s.</summary>
    [CreateAssetMenu(menuName = "AETHVIRSE/Item", fileName = "Item")]
    public sealed class ItemData : ScriptableObject
    {
        public string id = "";
        public string displayName = "";
        [TextArea] public string description = "";
        public ItemKind kind = ItemKind.Material;
        public Rarity rarity = Rarity.Common;
        public int maxStack = 1;
        public int sellPrice;
        public bool canSell = true;

        [Header("Equipment")]
        public EquipSlot slot = EquipSlot.None;
        public float attack;
        public float attackPerLevel;
        public float defense;
        public float defensePerLevel;
        public List<StatBonus> statBonuses = new List<StatBonus>();
        [Tooltip("Generated drops (random rarity + affixes) are built from these bases.")]
        public bool isDropBase;
        [Tooltip("Chance per hit to poison (venom weapons).")]
        public float onHitPoison;

        [Header("Use")]
        public int healAmount;
        public int manaAmount;
        public bool identifiesItems;
        public bool opensMysteryBox;

        [Header("Secrets")]
        public bool unidentified;
        public string unidentifiedName = "???";
        [TextArea] public string unidentifiedDescription = "";
        public bool cursed;
        public float curseHpFraction;
        public float curseInterval = 5f;
        public int evolveKills;
        public string evolvesInto = "";
        public string hiddenEffect = "";

        [Header("Skill books and runes")]
        [Tooltip("Skill book: the secret skill it teaches (SecretSkillDatabase id).")]
        public string teachesSkill = "";
        [Tooltip("Rune item: the rune it is (SecretSkillDatabase id).")]
        public string runeId = "";

        public bool IsStackable => maxStack > 1;
        public bool IsEquipment => slot != EquipSlot.None;
        public bool IsUsable => kind == ItemKind.Consumable || kind == ItemKind.Food || identifiesItems || opensMysteryBox
            || (kind == ItemKind.SkillBook && !string.IsNullOrEmpty(teachesSkill));
        public float AttackAt(int level) => attack + attackPerLevel * level;

        public float Bonus(string stat)
        {
            foreach (StatBonus b in statBonuses) if (b.stat == stat) return b.value;
            return 0f;
        }

        public static Color RarityColor(Rarity r) => r switch
        {
            Rarity.Common => new Color32(0xB0, 0xB0, 0xB0, 0xFF),
            Rarity.Uncommon => new Color32(0x4C, 0xAF, 0x50, 0xFF),
            Rarity.Rare => new Color32(0x2F, 0x80, 0xED, 0xFF),
            Rarity.Epic => new Color32(0x9B, 0x51, 0xE0, 0xFF),
            Rarity.Legendary => new Color32(0xF2, 0x99, 0x4A, 0xFF),
            Rarity.Mythic => new Color32(0xEB, 0x57, 0x57, 0xFF),
            _ => Color.white,
        };
    }
}
