// The bulk of the item catalogue: tiered drop bases for every world, eight named gear sets, gems in four grades,
// enchant scrolls, consumables, region materials and named uniques — each with its own name and text.
// Called by ItemDataBuilder.Build (writes into the same database).
using System.Collections.Generic;
using Aethvirse.Combat;
using Aethvirse.Items;
using UnityEditor;
using UnityEngine;

public static class ItemContent
{
    public static void Build()
    {
        Bases();
        Sets();
        Gems();
        Scrolls();
        Consumables();
        Materials();
        Uniques();
    }

    // --- Tiered drop bases ------------------------------------------------------------------------------

    /// <summary>(level, prefix, flavour, bonus stat): one band of hunter gear per stretch of the world.</summary>
    static readonly (int level, string prefix, string flavour, string stat)[] Bands =
    {
        (50, "Thornwarden", "Grown more than forged, in the quiet valley.", StatKeys.Vit),
        (100, "Sunscar", "Red dune-steel, still warm from a sun that never sets.", StatKeys.Str),
        (150, "Frostbound", "Ice-tempered. It never quite stops being cold.", StatKeys.Int),
        (200, "Tideglass", "Salvaged from the drowned archive; it hums with old words.", StatKeys.Sen),
        (250, "Riftsteel", "Forged across the crack in the world. It is heavier on one side.", StatKeys.Agi),
        (300, "Hollowcrown", "Court armour of a kingdom with an empty throne.", StatKeys.Str),
        (400, "Glasswright", "Seren-made. Light, clear, and harder than it looks.", StatKeys.Agi),
        (550, "Cinderforged", "Kethran war-gear, quenched in the last fires of their world.", StatKeys.Vit),
        (700, "Seraphic", "Taken from the hosts of the Seat. It does not want you to wear it.", StatKeys.Int),
    };

    static readonly (string suffix, EquipSlot slot, float atk, float def)[] Pieces =
    {
        ("Greatsword", EquipSlot.Weapon, 4.4f, 0f), ("Twinfangs", EquipSlot.Weapon, 4f, 0f), ("Staff", EquipSlot.Weapon, 4.2f, 0f),
        ("Longbow", EquipSlot.Weapon, 4.1f, 0f), ("Buckler", EquipSlot.OffHand, 0f, 0.9f), ("Helm", EquipSlot.Helm, 0f, 0.7f),
        ("Mail", EquipSlot.Armor, 0f, 1.35f), ("Gauntlets", EquipSlot.Gloves, 0f, 0.45f), ("Greaves", EquipSlot.Boots, 0f, 0.55f),
        ("Signet", EquipSlot.Ring, 0f, 0f), ("Pendant", EquipSlot.Necklace, 0f, 0f), ("Rune Core", EquipSlot.RuneCore, 0f, 0f),
    };

    static void Bases()
    {
        foreach (var b in Bands)
            foreach (var p in Pieces)
            {
                string id = $"{b.prefix.ToLowerInvariant()}_{p.suffix.ToLowerInvariant().Replace(' ', '_')}";
                ItemKind kind = p.slot == EquipSlot.Weapon ? ItemKind.Weapon : p.slot == EquipSlot.Ring || p.slot == EquipSlot.Necklace || p.slot == EquipSlot.RuneCore ? ItemKind.Accessory : ItemKind.Armor;
                ItemData i = ItemDataBuilder.Item(id, $"{b.prefix} {p.suffix}", kind, Rarity.Common, 1, 12 + b.level / 5, b.flavour);
                i.slot = p.slot;
                i.attackPerLevel = p.atk;
                i.defensePerLevel = p.def;
                i.isDropBase = true;
                i.minLevel = b.level;
                i.statBonuses.Add(new StatBonus(b.stat, 1f + b.level / 50f));
            }
    }

    // --- Gear sets ----------------------------------------------------------------------------------------

    struct SetDef
    {
        public string id, name, lore;
        public Rarity rarity;
        public int level;
        public string mainStat;
        public (int pieces, string stat, float value, string text)[] tiers;
        public string[] pieceNames;
    }

    static readonly SetDef[] SetDefs =
    {
        new SetDef { id = "ashwalker", name = "Ashwalker", rarity = Rarity.Rare, level = 15, mainStat = StatKeys.Str,
            lore = "Worn by the scouts who went into the dead city first and came back last.",
            tiers = new[] { (2, StatKeys.AtkPct, 0.05f, "ATK +5%"), (4, StatKeys.Lifesteal, 0.03f, "Heal 3% of damage dealt"), (6, StatKeys.AtkPct, 0.12f, "ATK +12% more") },
            pieceNames = new[] { "Ashwalker's Cleaver", "Ashwalker's Hood", "Ashwalker's Cloak", "Ashwalker's Wraps", "Ashwalker's Treads", "Ashwalker's Lantern" } },
        new SetDef { id = "verdant_oath", name = "Verdant Oath", rarity = Rarity.Rare, level = 60, mainStat = StatKeys.Vit,
            lore = "Sworn to a valley that never asked for protecting. It was grateful anyway.",
            tiers = new[] { (2, StatKeys.HpPct, 0.08f, "HP +8%"), (4, StatKeys.DefPct, 0.1f, "DEF +10%"), (6, StatKeys.HpPct, 0.2f, "HP +20% more") },
            pieceNames = new[] { "Oathroot Maul", "Oathbark Helm", "Oathleaf Hauberk", "Oathvine Gloves", "Oathmoss Boots", "Seed of the Oath" } },
        new SetDef { id = "dunestrider", name = "Dunestrider", rarity = Rarity.Epic, level = 110, mainStat = StatKeys.Agi,
            lore = "Couriers of the Crimson Dunes ran these routes for three hundred years without being caught.",
            tiers = new[] { (2, StatKeys.Mspd, 0.08f, "Move speed +8%"), (4, StatKeys.Aspd, 0.1f, "Attack speed +10%"), (6, StatKeys.Crit, 0.08f, "Critical chance +8%") },
            pieceNames = new[] { "Dunestrider Khopesh", "Dunestrider Veil", "Dunestrider Robes", "Dunestrider Grips", "Dunestrider Sandals", "Dunestrider Compass" } },
        new SetDef { id = "frostveil_regalia", name = "Frostveil Regalia", rarity = Rarity.Epic, level = 160, mainStat = StatKeys.Int,
            lore = "Court dress of the ice-mages, who froze their own hall rather than surrender it.",
            tiers = new[] { (2, StatKeys.Cdr, 0.05f, "Cooldowns −5%"), (4, StatKeys.AtkPct, 0.1f, "ATK +10%"), (6, StatKeys.Cdr, 0.12f, "Cooldowns −12% more") },
            pieceNames = new[] { "Rimecrown Sceptre", "Rimecrown Circlet", "Rimecrown Mantle", "Rimecrown Mitts", "Rimecrown Slippers", "Rimecrown Snowflake" } },
        new SetDef { id = "drowned_archivist", name = "Drowned Archivist", rarity = Rarity.Epic, level = 210, mainStat = StatKeys.Sen,
            lore = "The archivists stayed with their books when the tide came. Their gear still smells of ink and salt.",
            tiers = new[] { (2, StatKeys.Crit, 0.05f, "Critical chance +5%"), (4, StatKeys.CritDmg, 0.25f, "Critical damage +25%"), (6, StatKeys.Crit, 0.1f, "Critical chance +10% more") },
            pieceNames = new[] { "Archivist's Quill-Blade", "Archivist's Hood", "Archivist's Coat", "Archivist's Inkstained Gloves", "Archivist's Waders", "Archivist's Index Key" } },
        new SetDef { id = "fracture_knight", name = "Fracture Knight", rarity = Rarity.Legendary, level = 260, mainStat = StatKeys.Vit,
            lore = "Knights who stood on the edge when the world split, and did not step back.",
            tiers = new[] { (2, StatKeys.DefPct, 0.12f, "DEF +12%"), (4, StatKeys.AtkPct, 0.12f, "ATK +12%"), (6, StatKeys.HpPct, 0.25f, "HP +25%") },
            pieceNames = new[] { "Faultline Blade", "Faultline Greathelm", "Faultline Plate", "Faultline Gauntlets", "Faultline Sabatons", "Faultline Shard" } },
        new SetDef { id = "hollow_regent", name = "Hollow Regent", rarity = Rarity.Legendary, level = 310, mainStat = StatKeys.Str,
            lore = "Regalia made for a ruler who never came. It fits you suspiciously well.",
            tiers = new[] { (2, StatKeys.AtkPct, 0.1f, "ATK +10%"), (4, StatKeys.Lifesteal, 0.05f, "Heal 5% of damage dealt"), (6, StatKeys.AtkPct, 0.25f, "ATK +25% more") },
            pieceNames = new[] { "Regent's Unused Sword", "Regent's Empty Crown", "Regent's Coronation Robe", "Regent's Signet Gloves", "Regent's Throne-Steps", "Regent's Unsigned Seal" } },
        new SetDef { id = "first_vanguard", name = "Echo of the First Vanguard", rarity = Rarity.Mythic, level = 600, mainStat = StatKeys.Sen,
            lore = "Someone carried the Codex before you. This is what is left of them. It remembers how to fight.",
            tiers = new[] { (2, StatKeys.AtkPct, 0.15f, "ATK +15%"), (4, StatKeys.Cdr, 0.15f, "Cooldowns −15%"), (6, StatKeys.CritDmg, 0.6f, "Critical damage +60%") },
            pieceNames = new[] { "First Vanguard's Blade", "First Vanguard's Visor", "First Vanguard's Coat", "First Vanguard's Grips", "First Vanguard's Boots", "First Vanguard's Codex Clasp" } },
    };

    static readonly (EquipSlot slot, ItemKind kind, float atk, float def)[] SetSlots =
    {
        (EquipSlot.Weapon, ItemKind.Weapon, 5f, 0f), (EquipSlot.Helm, ItemKind.Armor, 0f, 0.8f), (EquipSlot.Armor, ItemKind.Armor, 0f, 1.5f),
        (EquipSlot.Gloves, ItemKind.Armor, 0f, 0.5f), (EquipSlot.Boots, ItemKind.Armor, 0f, 0.6f), (EquipSlot.Necklace, ItemKind.Accessory, 0f, 0f),
    };

    static void Sets()
    {
        var db = LoadOrCreate<GearSetDatabase>("Assets/Resources/GearSetDatabase.asset");
        db.sets = new List<GearSet>();
        foreach (SetDef d in SetDefs)
        {
            var set = new GearSet { id = d.id, displayName = d.name, lore = d.lore };
            foreach (var t in d.tiers) set.tiers.Add(new SetTier { pieces = t.pieces, stat = t.stat, value = t.value, text = t.text });
            db.sets.Add(set);
            for (int k = 0; k < SetSlots.Length; k++)
            {
                var sl = SetSlots[k];
                ItemData i = ItemDataBuilder.Item($"set_{d.id}_{k}", d.pieceNames[k], sl.kind, d.rarity, 1, 80 * (1 + (int)d.rarity * (int)d.rarity),
                    $"Part of the {d.name} set. {d.lore}");
                i.slot = sl.slot;
                i.attackPerLevel = sl.atk * (1f + 0.08f * (int)d.rarity);
                i.defensePerLevel = sl.def * (1f + 0.08f * (int)d.rarity);
                i.setId = d.id;
                i.minLevel = d.level;
                i.statBonuses.Add(new StatBonus(d.mainStat, 3f + d.level / 25f));
            }
        }
        EditorUtility.SetDirty(db);
    }

    // --- Gems -----------------------------------------------------------------------------------------

    static readonly (string id, string name, string stat, float v1, string text)[] GemTypes =
    {
        ("ruby", "Ruby", StatKeys.Str, 3f, "Red as a held breath."),
        ("emerald", "Emerald", StatKeys.Agi, 3f, "Green, and quick to catch the light."),
        ("sapphire", "Sapphire", StatKeys.Int, 3f, "Blue that goes deeper than the stone."),
        ("diamond", "Diamond", StatKeys.Vit, 3f, "Nothing scratches it. Nothing."),
        ("topaz", "Topaz", StatKeys.Sen, 3f, "It glints a moment before danger does."),
        ("onyx", "Onyx", StatKeys.Crit, 0.01f, "Black, and sharp at every edge."),
        ("bloodstone", "Bloodstone", StatKeys.Lifesteal, 0.006f, "Warm. It has a pulse."),
    };
    static readonly string[] GemGrades = { "Chipped", "Flawed", "Flawless", "Royal" };
    static readonly float[] GemScale = { 1f, 2.3f, 4.8f, 9.5f };
    static readonly Rarity[] GemRarity = { Rarity.Uncommon, Rarity.Rare, Rarity.Epic, Rarity.Legendary };

    static void Gems()
    {
        foreach (var g in GemTypes)
            for (int k = 0; k < 4; k++)
            {
                ItemData i = ItemDataBuilder.Item($"gem_{g.id}_{k + 1}", $"{GemGrades[k]} {g.name}", ItemKind.Gem, GemRarity[k], 20, 40 * (k + 1) * (k + 1),
                    $"{g.text} Socket it into gear with a free socket. Three of a grade fuse into the next.");
                i.gemStat = g.stat;
                i.gemValue = g.v1 * GemScale[k];
            }
    }

    // --- Enchant scrolls --------------------------------------------------------------------------------

    static readonly (string id, string name, bool weapon, ElementType element, string stat, float value, Rarity rarity, string mat, string text)[] ScrollDefs =
    {
        ("scroll_kindling", "Scroll of Kindling Steel", true, ElementType.Fire, StatKeys.AtkPct, 0.04f, Rarity.Rare, "mat_r1_ashfall", "Your weapon burns. Normal attacks become Fire."),
        ("scroll_rime", "Scroll of Rime Edge", true, ElementType.Ice, StatKeys.AtkPct, 0.04f, Rarity.Rare, "mat_r4_frostveil", "Frost grows on the edge. Normal attacks become Ice."),
        ("scroll_gale", "Scroll of the Gale Edge", true, ElementType.Wind, StatKeys.Aspd, 0.05f, Rarity.Rare, "mat_r3_dunes", "The blade whistles. Normal attacks become Wind."),
        ("scroll_bedrock", "Scroll of Bedrock", true, ElementType.Earth, StatKeys.DefPct, 0.05f, Rarity.Rare, "mat_r2_verdant", "Heavy as the hills. Normal attacks become Earth."),
        ("scroll_storm", "Scroll of the Storm Brand", true, ElementType.Lightning, StatKeys.AtkPct, 0.04f, Rarity.Rare, "mat_r6_fracture", "It crackles in your grip. Normal attacks become Lightning."),
        ("scroll_umbra", "Scroll of Umbra", true, ElementType.Shadow, StatKeys.Crit, 0.03f, Rarity.Epic, "mat_r5_archive", "The edge drinks light. Normal attacks become Shadow."),
        ("scroll_dawn", "Scroll of First Dawn", true, ElementType.Light, StatKeys.AtkPct, 0.05f, Rarity.Epic, "mat_s3_choir", "A small sunrise lives in the steel. Normal attacks become Light."),
        ("scroll_unwritten", "Scroll That Was Never Written", true, ElementType.Void, StatKeys.AtkPct, 0.08f, Rarity.Mythic, "mat_z1_seat", "The words fall off the page as you read. Normal attacks become Void."),
        ("scroll_warding", "Scroll of Warding", false, ElementType.Physical, StatKeys.DefPct, 0.06f, Rarity.Uncommon, "mat_r2_verdant", "Armour enchant: DEF +6%."),
        ("scroll_vigor", "Scroll of Vigor", false, ElementType.Physical, StatKeys.HpPct, 0.06f, Rarity.Uncommon, "mat_r1_ashfall", "Armour enchant: HP +6%."),
        ("scroll_swiftness", "Scroll of Swiftness", false, ElementType.Physical, StatKeys.Mspd, 0.05f, Rarity.Rare, "mat_r3_dunes", "Armour enchant: move speed +5%."),
        ("scroll_focus", "Scroll of Focus", false, ElementType.Physical, StatKeys.Cdr, 0.04f, Rarity.Rare, "mat_r4_frostveil", "Armour enchant: cooldowns −4%."),
        ("scroll_fury", "Scroll of Fury", false, ElementType.Physical, StatKeys.Aspd, 0.05f, Rarity.Rare, "mat_k1_cinder", "Armour enchant: attack speed +5%."),
        ("scroll_keen_eye", "Scroll of the Keen Eye", false, ElementType.Physical, StatKeys.Crit, 0.03f, Rarity.Epic, "mat_k2_hollow_moon", "Armour enchant: critical chance +3%."),
    };

    static void Scrolls()
    {
        foreach (var d in ScrollDefs)
        {
            ItemData i = ItemDataBuilder.Item(d.id, d.name, ItemKind.EnchantScroll, d.rarity, 10, 60 * (1 + (int)d.rarity * (int)d.rarity), d.text);
            i.enchantWeapon = d.weapon;
            i.enchantElement = d.element;
            i.gemStat = d.stat;
            i.gemValue = d.value;
        }
    }

    // --- Consumables and materials ---------------------------------------------------------------------

    static void Consumables()
    {
        ItemDataBuilder.Item("mana_draught", "Mana Draught", ItemKind.Consumable, Rarity.Common, 20, 8, "Tastes like cold metal. Restores 80 MP.").manaAmount = 80;
        ItemDataBuilder.Item("mana_draught_greater", "Greater Mana Draught", ItemKind.Consumable, Rarity.Uncommon, 20, 25, "Restores 220 MP.").manaAmount = 220;
        ItemDataBuilder.Item("potion_royal", "Royal Potion", ItemKind.Consumable, Rarity.Rare, 20, 90, "Brewed for academy champions. Restores 1200 HP.").healAmount = 1200;
        ItemDataBuilder.Item("phoenix_tonic", "Phoenix Tonic", ItemKind.Consumable, Rarity.Epic, 10, 300, "One sip and you feel reborn. Restores 4000 HP.").healAmount = 4000;
        ItemDataBuilder.Item("canteen_stew", "Academy Canteen Stew", ItemKind.Food, Rarity.Common, 20, 2, "Nobody knows what is in it. Restores 60 HP.").healAmount = 60;
        ItemDataBuilder.Item("dune_lizard", "Grilled Dune Lizard", ItemKind.Food, Rarity.Common, 20, 6, "Crunchy. Restores 150 HP.").healAmount = 150;
        ItemDataBuilder.Item("frostberry_tart", "Frostberry Tart", ItemKind.Food, Rarity.Uncommon, 20, 12, "Sweet, and cold enough to hurt your teeth. Restores 300 HP.").healAmount = 300;
        ItemDataBuilder.Item("starbread", "Kethran Starbread", ItemKind.Food, Rarity.Rare, 20, 40, "The last recipe of a dead world. Restores 900 HP.").healAmount = 900;
        ItemData both = ItemDataBuilder.Item("elixir_twofold", "Twofold Elixir", ItemKind.Consumable, Rarity.Epic, 10, 200, "Half red, half blue, never mixing. Restores 1500 HP and 300 MP.");
        both.healAmount = 1500;
        both.manaAmount = 300;
    }

    /// <summary>(region id, material name, text): one crafting material per region (enemies there drop it).</summary>
    public static readonly (string region, string name, string text)[] MaterialDefs =
    {
        ("r1_ashfall", "Ember Ash", "Ash that never cools. It glows when you breathe on it."),
        ("r2_verdant", "Heartwood Splinter", "From a tree that grew back three times."),
        ("r3_dunes", "Red Glass Sand", "Sand that melted into glass and remembered being sand."),
        ("r4_frostveil", "Everfrost Shard", "Ice that will not melt in your hand."),
        ("r5_archive", "Drowned Ink", "A vial of ink that writes by itself if you leave it open."),
        ("r6_fracture", "Fault Crystal", "A crystal from the crack in the world. It hums in two notes."),
        ("r7_throne", "Throne Dust", "Dust from an empty seat. It is heavier than it should be."),
        ("s1_glasswood", "Singing Glass", "Glass from a tree. It rings when the wind moves."),
        ("s2_mirrorsea", "Mirror Pearl", "It shows you from behind."),
        ("s3_choir", "Silent Bell Shard", "Part of a bell that rang its last note."),
        ("k1_cinder", "Kethran Cinder", "An ember from another world's last fire."),
        ("k2_hollow_moon", "Moonhollow Stone", "Light, and empty inside."),
        ("k3_last_city", "Last City Brick", "A name is scratched into it."),
        ("z1_seat", "Seraph Feather", "It does not fall. You have to put it down."),
    };

    static void Materials()
    {
        int k = 0;
        foreach (var m in MaterialDefs)
            ItemDataBuilder.Item("mat_" + m.region, m.name, ItemKind.Material, k < 7 ? Rarity.Uncommon : Rarity.Rare, 99, 6 + 3 * k++, m.text);
    }

    // --- Uniques ------------------------------------------------------------------------------------------

    static void Uniques()
    {
        U("blade_of_the_cinder_seat", "Blade of the Cinder Seat", EquipSlot.Weapon, Rarity.Legendary, 40, "Every student who sat in the Cinder Seat carved a line on this blade. There is room for one more.",
            (StatKeys.Str, 10f), (StatKeys.AtkPct, 0.08f));
        U("kaelens_spare_sword", "Kaelen's Spare Sword", EquipSlot.Weapon, Rarity.Epic, 30, "He would never admit he left it for you.", (StatKeys.Agi, 8f), (StatKeys.Crit, 0.04f));
        U("nyras_reading_lamp", "Nyra's Reading Lamp", EquipSlot.Necklace, Rarity.Legendary, 60, "It only lights pages that someone tried to burn.", (StatKeys.Int, 12f), (StatKeys.Sen, 8f));
        U("horne_whistle", "Instructor's Whistle", EquipSlot.Necklace, Rarity.Epic, 35, "Blow it and everyone stands straighter. Including you.", (StatKeys.Vit, 8f), (StatKeys.DefPct, 0.05f));
        U("verdant_heart", "Heart of the Valley", EquipSlot.RuneCore, Rarity.Legendary, 70, "It beats slowly. The valley around it grows a little greener each time.", (StatKeys.HpPct, 0.12f), (StatKeys.Vit, 12f));
        U("sandglass_dagger", "Sandglass Dagger", EquipSlot.Weapon, Rarity.Legendary, 120, "Its blade is an hourglass. Cuts arrive a moment early.", (StatKeys.Aspd, 0.12f), (StatKeys.Agi, 14f));
        U("frozen_tear", "The Frozen Tear", EquipSlot.Ring, Rarity.Legendary, 160, "A queen's last tear, frozen before it fell.", (StatKeys.Int, 16f), (StatKeys.Cdr, 0.06f));
        U("drowned_crown", "Drowned Crown", EquipSlot.Helm, Rarity.Legendary, 210, "Water still drips from it. It is never wet.", (StatKeys.Sen, 18f), (StatKeys.CritDmg, 0.3f));
        U("fault_hammer", "Hammer That Split the World", EquipSlot.Weapon, Rarity.Mythic, 260, "Nobody says who swung it first.", (StatKeys.Str, 24f), (StatKeys.AtkPct, 0.15f), (StatKeys.CritDmg, 0.25f));
        U("unwritten_page_ring", "Ring of the Unwritten Page", EquipSlot.Ring, Rarity.Mythic, 330, "There is a word engraved inside. It changes when you are not looking.", (StatKeys.AtkPct, 0.1f), (StatKeys.Cdr, 0.08f), (StatKeys.Sen, 12f));
        U("glass_tree_bow", "Bow of the Glass Tree", EquipSlot.Weapon, Rarity.Legendary, 410, "Cut from a branch that sang. The string still hums.", (StatKeys.Agi, 22f), (StatKeys.Crit, 0.06f));
        U("mirror_shield", "Shield of the Other Sky", EquipSlot.OffHand, Rarity.Legendary, 460, "Its face reflects a sky with two moons.", (StatKeys.DefPct, 0.14f), (StatKeys.Vit, 20f));
        U("conductors_baton", "The Conductor's Baton", EquipSlot.Weapon, Rarity.Mythic, 520, "Lift it and the world waits for the first note.", (StatKeys.Int, 30f), (StatKeys.Cdr, 0.1f), (StatKeys.AtkPct, 0.12f));
        U("cinder_kings_mantle", "Mantle of the Cinder King", EquipSlot.Armor, Rarity.Legendary, 560, "He lost his world in it. He did not lose it easily.", (StatKeys.HpPct, 0.18f), (StatKeys.Str, 22f));
        U("hollow_moon_boots", "Hollow Moon Boots", EquipSlot.Boots, Rarity.Legendary, 610, "Every step is a little lighter than it should be.", (StatKeys.Mspd, 0.15f), (StatKeys.Agi, 20f));
        U("last_city_banner", "Banner of the Last City", EquipSlot.Necklace, Rarity.Mythic, 660, "They raised it on the final morning. It is still raised.", (StatKeys.AtkPct, 0.12f), (StatKeys.DefPct, 0.12f), (StatKeys.HpPct, 0.12f));
        U("seraph_halo", "Fallen Halo", EquipSlot.Helm, Rarity.Mythic, 720, "It slipped off a host of the Seat. It does not want to go back.", (StatKeys.Int, 34f), (StatKeys.CritDmg, 0.4f));
        U("first_line_quill", "The First Line's Quill", EquipSlot.Weapon, Rarity.Mythic, 760, "Whatever it writes, happens. Write carefully.", (StatKeys.Sen, 30f), (StatKeys.AtkPct, 0.2f), (StatKeys.Crit, 0.08f));
        U("codex_bookmark", "Codex Bookmark", EquipSlot.RuneCore, Rarity.Mythic, 350, "It keeps your place. Which place, it will not say.", (StatKeys.Cdr, 0.12f), (StatKeys.Sen, 16f));
        U("empty_name_tag", "Name Tag With No Name", EquipSlot.Necklace, Rarity.Epic, 5, "Found on the floor of the academy archive. The pin is still warm.", (StatKeys.Sen, 6f), (StatKeys.Crit, 0.03f));
    }

    static void U(string id, string name, EquipSlot slot, Rarity rarity, int level, string desc, params (string stat, float value)[] bonuses)
    {
        ItemKind kind = slot == EquipSlot.Weapon ? ItemKind.Weapon : slot == EquipSlot.Ring || slot == EquipSlot.Necklace || slot == EquipSlot.RuneCore ? ItemKind.Accessory : ItemKind.Armor;
        ItemData i = ItemDataBuilder.Item("u_" + id, name, kind, rarity, 1, 100 * (1 + (int)rarity * (int)rarity), desc);
        i.slot = slot;
        i.attackPerLevel = slot == EquipSlot.Weapon ? 5f + (int)rarity * 0.4f : 0f;
        i.defensePerLevel = slot == EquipSlot.Armor ? 1.6f : slot == EquipSlot.Helm || slot == EquipSlot.OffHand ? 0.9f : slot == EquipSlot.Boots ? 0.65f : 0f;
        i.minLevel = level;
        foreach (var b in bonuses) i.statBonuses.Add(new StatBonus(b.stat, b.value));
    }

    // --- Recipes ------------------------------------------------------------------------------------------

    public static List<Recipe> Recipes()
    {
        var list = new List<Recipe>();
        foreach (var g in GemTypes)
            for (int k = 1; k < 4; k++)
                list.Add(new Recipe { id = $"gem_{g.id}_{k + 1}", inputIds = { $"gem_{g.id}_{k}" }, inputCounts = { 3 }, output = $"gem_{g.id}_{k + 1}", goldCost = 100 * k * k });
        foreach (var d in ScrollDefs)
            list.Add(new Recipe { id = d.id, inputIds = { d.mat, "scrap_metal" }, inputCounts = { d.rarity >= Rarity.Mythic ? 20 : 6, 4 }, output = d.id, goldCost = 150 * (1 + (int)d.rarity) });
        list.Add(new Recipe { id = "phoenix_tonic", inputIds = { "potion_royal", "mat_r1_ashfall" }, inputCounts = { 2, 3 }, output = "phoenix_tonic", goldCost = 300 });
        list.Add(new Recipe { id = "elixir_twofold", inputIds = { "potion_royal", "mana_draught_greater" }, inputCounts = { 1, 2 }, output = "elixir_twofold", goldCost = 120 });
        return list;
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
