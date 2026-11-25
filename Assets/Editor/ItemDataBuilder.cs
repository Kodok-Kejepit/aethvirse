// Writes the item database (port of the Godot data/items/*.tres), loot rules, recipes and shop stock.
// Assets are updated in place, so references keep working.
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Aethvirse.Items;
using UnityEditor;
using UnityEngine;

public static class ItemDataBuilder
{
    const string Dir = "Assets/Data/Items";
    public const string DatabasePath = Dir + "/ItemDatabase.asset";
    public const string RulesPath = "Assets/Data/Balancing/LootRules.asset";

    internal static ItemDatabase _db;

    [MenuItem("AETHVIRSE/Build Item Data")]
    public static void Build()
    {
        Directory.CreateDirectory(Dir);
        Directory.CreateDirectory(Path.GetDirectoryName(RulesPath));
        LoadOrCreate<LootRules>(RulesPath);
        _db = LoadOrCreate<ItemDatabase>(DatabasePath);
        _db.items = new List<ItemData>();

        // Consumables & materials
        Item("potion_small", "Small Potion", ItemKind.Consumable, Rarity.Common, 20, 5, "Restores 150 HP.").healAmount = 150;
        Item("potion_large", "Large Potion", ItemKind.Consumable, Rarity.Uncommon, 20, 15, "Restores 400 HP.").healAmount = 400;
        Item("bread", "Bread", ItemKind.Food, Rarity.Common, 20, 1, "Still warm. Restores 30 HP.").healAmount = 30;
        Item("codex_identify_scroll", "Codex Identification Scroll", ItemKind.Consumable, Rarity.Uncommon, 10, 10, "Reveals the nature of one unidentified item.").identifiesItems = true;
        Item("mystery_box", "Mystery Box", ItemKind.Consumable, Rarity.Unknown, 20, 50, "Something rattles inside. It feels heavier when you look away.").opensMysteryBox = true;
        Item("herb", "Ashfall Herb", ItemKind.Material, Rarity.Common, 99, 2, "Grows where the ash is thickest.");
        Item("scrap_metal", "Scrap Metal", ItemKind.Material, Rarity.Uncommon, 99, 4, "Salvaged from broken gear.");
        Item("rusty_coin", "Rusty Coin", ItemKind.Currency, Rarity.Uncommon, 999, 0, "A coin from a city that no longer exists.");
        ItemData key = Item("gate_key", "Gate Key", ItemKind.Key, Rarity.Legendary, 9, 0, "Opens a sealed chest inside a Gate.");
        key.canSell = false;
        ItemData sigil = Item("sigil_first", "The First Sigil", ItemKind.Key, Rarity.Legendary, 1, 0, "Warm to the touch. It points somewhere.");
        sigil.canSell = false;

        // Issued (starter) weapons per class
        Weapon("issued_greatsword", "Issued Greatsword", Rarity.Common, 0f, 4.0f, 5, "Academy issue. Heavy, honest steel.");
        Weapon("issued_daggers", "Issued Twin Daggers", Rarity.Common, 0f, 3.6f, 5, "Academy issue. A matched pair.");
        Weapon("issued_staff", "Issued Focus Staff", Rarity.Common, 0f, 3.8f, 5, "Academy issue. Hums faintly.");
        Weapon("issued_bow", "Issued Hunting Bow", Rarity.Common, 0f, 3.9f, 5, "Academy issue. Well strung.");

        // Drop bases (random rarity + affixes)
        Base("hunter_greatsword", "Hunter Greatsword", EquipSlot.Weapon, 0f, 4.2f, 0f, null);
        Base("hunter_buckler", "Hunter Buckler", EquipSlot.OffHand, 0f, 0f, 0.8f, null);
        Base("hunter_helm", "Hunter Helm", EquipSlot.Helm, 0f, 0f, 0.6f, null);
        Base("hunter_coat", "Hunter Coat", EquipSlot.Armor, 0f, 0f, 1.2f, null);
        Base("hunter_gloves", "Hunter Gloves", EquipSlot.Gloves, 0f, 0f, 0.4f, new StatBonus(StatKeys.Str, 1f));
        Base("hunter_boots", "Hunter Boots", EquipSlot.Boots, 0f, 0f, 0.5f, new StatBonus(StatKeys.Agi, 1f));
        Base("plain_ring", "Plain Ring", EquipSlot.Ring, 0f, 0f, 0f, new StatBonus(StatKeys.Sen, 1f));
        Base("plain_amulet", "Plain Amulet", EquipSlot.Necklace, 0f, 0f, 0f, new StatBonus(StatKeys.Vit, 1f));
        Base("rune_shard", "Rune Shard Core", EquipSlot.RuneCore, 0f, 0f, 0f, new StatBonus(StatKeys.Int, 1f));

        // Named and secret items (their secrets hook in with P1-11)
        Gear("ember_charm", "Ember Charm", EquipSlot.Necklace, Rarity.Epic, 120, "Warm, like a hand that never lets go.", new StatBonus(StatKeys.Str, 4f), new StatBonus(StatKeys.Vit, 2f));
        Gear("silver_band", "Silver Band", EquipSlot.Ring, Rarity.Rare, 40, "Engraved on the inside: 'come home'.", new StatBonus(StatKeys.Crit, 0.03f), new StatBonus(StatKeys.Sen, 2f));
        Gear("warden_seal", "Warden's Seal", EquipSlot.Ring, Rarity.Legendary, 400, "The seal of an order that guarded something it was told never to open.", new StatBonus(StatKeys.Vit, 8f), new StatBonus(StatKeys.Sen, 4f));
        ItemData license = Gear("rusted_license", "Hunter License — Marrow Kessick, Rank S", EquipSlot.Necklace, Rarity.Rare, 0, "Rank S. The photo has been scratched out.", new StatBonus(StatKeys.Sen, 5f));
        license.canSell = false;
        license.unidentified = true;
        license.unidentifiedName = "Rusted Card";
        license.unidentifiedDescription = "A rusted card on a chain. Letters under the rust.";
        license.hiddenEffect = "old_eyes";
        ItemData pipe = Weapon("rusty_pipe", "Rusty Pipe", Rarity.Common, 3f, 0f, 1, "It has seen worse days. So have you.");
        pipe.evolveKills = 300;
        pipe.evolvesInto = "fanged_pipe";
        ItemData fanged = Weapon("fanged_pipe", "Fanged Pipe", Rarity.Rare, 0f, 3.5f, 0, "Teeth grew on it. Nobody saw it happen.");
        fanged.evolvesInto = "first_fang";
        fanged.hiddenEffect = "bleed_5";
        Weapon("first_fang", "First Fang", Rarity.Epic, 0f, 5.5f, 0, "It remembers every bite.").hiddenEffect = "higher_rank_bonus";
        ItemData grave = Weapon("gravedigger_fang", "Gravedigger's Fang", Rarity.Epic, 0f, 6f, 0, "It drinks. You pay.");
        grave.cursed = true;
        grave.curseHpFraction = 0.01f;
        grave.statBonuses.Add(new StatBonus(StatKeys.Lifesteal, 0.08f));
        ItemData venom = Weapon("dagger_venomfang", "Venomfang", Rarity.Legendary, 30f, 6f, 1200, "Green drips from the edge even when it is clean.");
        venom.statBonuses.Add(new StatBonus(StatKeys.Agi, 10f));
        venom.statBonuses.Add(new StatBonus(StatKeys.Crit, 0.06f));
        venom.onHitPoison = 0.2f;
        ItemData starfall = Weapon("sword_starfall", "Starfall, Eye of the Void", Rarity.Mythic, 40f, 7f, 0, "The blade shows a sky that is not this one.");
        starfall.canSell = false;
        starfall.statBonuses.AddRange(new[] { new StatBonus(StatKeys.Str, 12f), new StatBonus(StatKeys.Sen, 8f), new StatBonus(StatKeys.Crit, 0.08f) });

        // Costumes (cosmetic)
        Costume("costume_starcloak", "Starcloak", Rarity.Epic);
        Costume("costume_wings_of_dawn", "Wings of Dawn", Rarity.Legendary);
        Costume("costume_void_halo", "Halo of the Unwritten", Rarity.Mythic);

        ItemContent.Build();
        _db.recipes = Recipes();
        _db.stock = QuartermasterStock();
        EditorUtility.SetDirty(_db);
        AssetDatabase.SaveAssets();
        Debug.Log($"AETHVIRSE: item data built ({_db.items.Count} items)");
    }

    /// <summary>Crafting recipes and Quartermaster stock for the player (wired by PlaygroundBuilder).</summary>
    public static List<Recipe> Recipes() => new List<Recipe>
    {
        new Recipe { id = "potion_small", inputIds = { "herb" }, inputCounts = { 2 }, output = "potion_small" },
        new Recipe { id = "potion_large", inputIds = { "potion_small", "herb" }, inputCounts = { 2, 3 }, output = "potion_large" },
        new Recipe { id = "mystery_box", inputIds = { "scrap_metal" }, inputCounts = { 12 }, output = "mystery_box", goldCost = 200 },
    }.Concat(ItemContent.Recipes()).ToList();

    public static List<ShopEntry> QuartermasterStock() => new List<ShopEntry>
    {
        new ShopEntry { itemId = "potion_small", price = 20 },
        new ShopEntry { itemId = "potion_large", price = 80 },
        new ShopEntry { itemId = "bread", price = 5 },
        new ShopEntry { itemId = "codex_identify_scroll", price = 60 },
        new ShopEntry { itemId = "mana_draught", price = 30 },
        new ShopEntry { itemId = "mana_draught_greater", price = 110 },
        new ShopEntry { itemId = "potion_royal", price = 400 },
        new ShopEntry { itemId = "canteen_stew", price = 12 },
        new ShopEntry { itemId = "gem_ruby_1", price = 150 },
        new ShopEntry { itemId = "gem_diamond_1", price = 150 },
        new ShopEntry { itemId = "scroll_warding", price = 900 },
        new ShopEntry { itemId = "scroll_kindling", price = 1200 },
    };

    internal static ItemData Item(string id, string name, ItemKind kind, Rarity rarity, int maxStack, int price, string desc)
    {
        var i = LoadOrCreate<ItemData>($"{Dir}/{id}.asset");
        // Reset to defaults so a rebuild never keeps stale fields.
        EditorUtility.CopySerialized(ScriptableObject.CreateInstance<ItemData>(), i);
        i.name = id;
        i.id = id;
        i.displayName = name;
        i.kind = kind;
        i.rarity = rarity;
        i.maxStack = maxStack;
        i.sellPrice = price;
        i.description = desc;
        EditorUtility.SetDirty(i);
        _db.items.Add(i);
        return i;
    }

    internal static ItemData Weapon(string id, string name, Rarity rarity, float atk, float atkPerLevel, int price, string desc)
    {
        ItemData i = Item(id, name, ItemKind.Weapon, rarity, 1, price, desc);
        i.slot = EquipSlot.Weapon;
        i.attack = atk;
        i.attackPerLevel = atkPerLevel;
        return i;
    }

    static void Base(string id, string name, EquipSlot slot, float atk, float atkPerLevel, float defPerLevel, StatBonus? bonus)
    {
        ItemKind kind = slot == EquipSlot.Weapon ? ItemKind.Weapon : slot == EquipSlot.Ring || slot == EquipSlot.Necklace || slot == EquipSlot.RuneCore ? ItemKind.Accessory : ItemKind.Armor;
        ItemData i = Item(id, name, kind, Rarity.Common, 1, 12, "Hunter-standard gear.");
        i.slot = slot;
        i.attack = atk;
        i.attackPerLevel = atkPerLevel;
        i.defensePerLevel = defPerLevel;
        i.isDropBase = true;
        if (bonus.HasValue) i.statBonuses.Add(bonus.Value);
    }

    internal static ItemData Gear(string id, string name, EquipSlot slot, Rarity rarity, int price, string desc, params StatBonus[] bonuses)
    {
        ItemData i = Item(id, name, ItemKind.Accessory, rarity, 1, price, desc);
        i.slot = slot;
        i.statBonuses.AddRange(bonuses);
        return i;
    }

    static void Costume(string id, string name, Rarity rarity)
    {
        ItemData i = Item(id, name, ItemKind.Costume, rarity, 1, 0, "A look, nothing more. Or so it says.");
        i.slot = EquipSlot.Costume;
        i.canSell = false;
    }

    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }
}
