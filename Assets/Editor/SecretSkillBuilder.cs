// Writes the secret skills (taught by skill books, any class), the skill runes, and their items (books and runes)
// into Resources/SecretSkillDatabase and the item database. Run after ItemDataBuilder (it appends its items).
using System.Collections.Generic;
using System.IO;
using Aethvirse.Combat;
using Aethvirse.Items;
using Aethvirse.Skills;
using Aethvirse.Stats;
using UnityEditor;
using UnityEngine;

public static class SecretSkillBuilder
{
    const string SkillDir = "Assets/Data/Skills/Secret";
    const string RuneDir = "Assets/Data/Skills/Runes";
    const string ItemDir = "Assets/Data/Items/Secret";

    static readonly Vector3 Around = new Vector3(7f, 2.2f, 7f), Centre = new Vector3(0, 1f, 0f);

    [MenuItem("AETHVIRSE/Build Secret Skills and Runes")]
    public static void Build()
    {
        Directory.CreateDirectory(SkillDir);
        Directory.CreateDirectory(RuneDir);
        Directory.CreateDirectory(ItemDir);
        ClassDataBuilder._dir = SkillDir;
        var db = LoadOrCreate<SecretSkillDatabase>("Assets/Resources/SecretSkillDatabase.asset");
        db.skills = Skills();
        db.runes = Runes();
        EditorUtility.SetDirty(db);

        var items = AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDataBuilder.DatabasePath);
        items.items.RemoveAll(i => i == null || i.kind == ItemKind.Rune || (i.kind == ItemKind.SkillBook && !string.IsNullOrEmpty(i.teachesSkill)));
        foreach (SkillData s in db.skills) items.items.Add(Book(s));
        foreach (SkillRuneData r in db.runes) items.items.Add(RuneItem(r));
        EditorUtility.SetDirty(items);
        AssetDatabase.SaveAssets();
        Debug.Log($"AETHVIRSE: secret skills built ({db.skills.Count} skills, {db.runes.Count} runes)");
    }

    // --- Secret skills ----------------------------------------------------------------------------------

    /// <summary>(skill, book rarity, book title, book text). The book text is the hint of what it is.</summary>
    static readonly Dictionary<string, (Rarity rarity, string book, string lore)> Books = new Dictionary<string, (Rarity, string, string)>();

    static List<SkillData> Skills()
    {
        Books.Clear();
        var list = new List<SkillData>
        {
            S(ClassDataBuilder.Active("sx_ashen_requiem", "Ashen Requiem", "Embers that never cooled fall on everything around you, and the ground remembers the fire.",
                SkillKind.Aoe, 1, 45, 22f, 3.6f, 0.25f, new Color(1f, 0.45f, 0.2f), SkillFxKind.SkyFall,
                ClassDataBuilder.M("sx_ashen_requiem", ElementType.Fire, 0.45f, 0.35f, 0.5f, new Vector3(11f, 3f, 11f), Centre, 6f, 60f, 0.12f, 0f, SwingKind.Slam)),
                Rarity.Epic, "Ash-Bound Hymnal", "Its pages are grey and warm. Someone sang from it while the city burned."),
            S(ClassDataBuilder.Heal("sx_verdant_bloom", "Verdant Bloom", "A ring of impossible flowers opens at your feet and knits you whole.", 1, 40, 35f, 0.45f, new Color(0.5f, 1f, 0.55f)),
                Rarity.Rare, "Pressed-Flower Almanac", "Every page holds a flower that should not grow in ash."),
            S(ClassDataBuilder.Active("sx_mirage_step", "Mirage Step", "You leave your shape behind. The wind carries the rest of you through your foes.",
                SkillKind.Dash, 1, 18, 6f, 2.2f, 0.15f, new Color(0.6f, 1f, 0.8f), SkillFxKind.Blink,
                ClassDataBuilder.M("sx_mirage_step", ElementType.Wind, 0.04f, 0.3f, 0.2f, new Vector3(2.2f, 1.8f, 3f), new Vector3(0, 1f, 0.8f), 5f, 30f, 0.05f, 30f, SwingKind.Thrust)),
                Rarity.Rare, "Notes of a Desert Courier", "'Run until the heat makes two of you. Then let the other one arrive.'"),
            S(ClassDataBuilder.Active("sx_glacier_coffin", "Glacier Coffin", "The air around you freezes solid. What stands inside it stops.",
                SkillKind.Aoe, 1, 40, 18f, 2.8f, 0.2f, new Color(0.55f, 0.85f, 1f), SkillFxKind.Frost,
                ClassDataBuilder.M("sx_glacier_coffin", ElementType.Ice, 0.3f, 0.2f, 0.45f, new Vector3(9f, 3f, 9f), Centre, 2f, 70f, 0.12f, 0f, SwingKind.Vertical)),
                Rarity.Epic, "Frostveil Ledger", "A list of names, each crossed out in ice. The last line is blank."),
            S(ClassDataBuilder.Active("sx_archivists_ink", "Archivist's Ink", "Five lines of black ink leave your hand, each one hunting a different word.",
                SkillKind.Projectile, 1, 30, 9f, 2f, 0.15f, new Color(0.2f, 0.2f, 0.35f), SkillFxKind.Volley,
                ClassDataBuilder.P("sx_archivists_ink", ElementType.Shadow, 0.12f, 5, 30f, 30f, 32f, 0.35f, 0, new Color(0.15f, 0.12f, 0.3f), true)),
                Rarity.Epic, "Drowned Index", "A catalogue of books that were never written. The ink is still wet."),
            S(ClassDataBuilder.Active("sx_fracture_line", "Fracture Line", "You draw a line on the world. The world agrees to break along it.",
                SkillKind.Melee, 1, 55, 20f, 4.5f, 0.3f, new Color(0.9f, 0.9f, 0.95f), SkillFxKind.GroundSplit,
                ClassDataBuilder.M("sx_fracture_line", ElementType.Void, 0.4f, 0.2f, 0.5f, new Vector3(2.5f, 2f, 12f), new Vector3(0, 1f, 6f), 8f, 80f, 0.16f, 0f, SwingKind.Vertical)),
                Rarity.Mythic, "Page Without Edges", "It has no margin. When you read it, the floor creaks."),
            S(ClassDataBuilder.Buff("sx_hollow_crown", "Hollow Crown", "Wear the weight of an empty throne. Your blows land like decrees.", 1, 35, 45f, BuffStat.Attack, 0.4f, 0.03f, 15f,
                new Color(0.8f, 0.8f, 0.85f), SkillFxKind.Barrier),
                Rarity.Legendary, "The Regent's Unsigned Order", "Sealed with a crown that has no jewels. The order inside is just: 'Rule.'"),
            S(ClassDataBuilder.Active("sx_bellsong", "Bellsong", "One clear note. Everything near you hears it in its bones.",
                SkillKind.Aoe, 1, 30, 12f, 2.4f, 0.18f, new Color(1f, 0.9f, 0.5f), SkillFxKind.Nova,
                ClassDataBuilder.M("sx_bellsong", ElementType.Light, 0.2f, 0.15f, 0.35f, Around, Centre, 10f, 50f, 0.1f, 0f, SwingKind.Rising)),
                Rarity.Rare, "Choir Practice Sheet", "Three notes are circled: low, high, and the one between."),
            S(ClassDataBuilder.Active("sx_glass_rain", "Glass Rain", "A dozen shards of singing glass, thrown in a fan that shreds the air.",
                SkillKind.Projectile, 1, 28, 8f, 1.4f, 0.12f, new Color(0.8f, 0.95f, 1f), SkillFxKind.Volley,
                ClassDataBuilder.P("sx_glass_rain", ElementType.Physical, 0.1f, 12, 55f, 36f, 22f, 0.25f, 1, new Color(0.85f, 0.95f, 1f), false)),
                Rarity.Epic, "Glasswright's Final Commission", "The client paid in advance and never came back for it."),
            S(ClassDataBuilder.Active("sx_tidebreaker", "Tidebreaker", "Charge like a wave that has waited a thousand years to land.",
                SkillKind.Dash, 1, 32, 10f, 3f, 0.2f, new Color(0.4f, 0.7f, 1f), SkillFxKind.Charge,
                ClassDataBuilder.M("sx_tidebreaker", ElementType.Ice, 0.1f, 0.4f, 0.3f, new Vector3(3f, 2f, 3f), new Vector3(0, 1f, 1f), 12f, 50f, 0.1f, 28f, SwingKind.Thrust)),
                Rarity.Epic, "Logbook of the Mirror Shallows", "'The sea pulled back. We all knew what that meant.'"),
            S(ClassDataBuilder.Active("sx_cinder_sovereign", "Cinder Sovereign", "Raise a column of living flame that bows to no one but you.",
                SkillKind.Aoe, 1, 60, 40f, 5.5f, 0.35f, new Color(1f, 0.35f, 0.1f), SkillFxKind.Pillar,
                ClassDataBuilder.M("sx_cinder_sovereign", ElementType.Fire, 0.5f, 0.4f, 0.5f, new Vector3(6f, 6f, 6f), new Vector3(0, 1f, 3.5f), 8f, 90f, 0.16f, 0f, SwingKind.Rising)),
                Rarity.Legendary, "Crown of the Cinder Reach", "A king of a burned world wrote this. He did not win, but he made them pay."),
            S(ClassDataBuilder.Stealth("sx_moonless_night", "Moonless Night", "Pull the dark over yourself like a cloak. Longer than any shadow should last.", 1, 30, 25f, 9f, new Color(0.15f, 0.15f, 0.25f)),
                Rarity.Rare, "Hollow Moon Survey", "'We measured the dark inside the moon. It measured us back.'"),
            S(ClassDataBuilder.Active("sx_star_archers_vow", "Star Archer's Vow", "One arrow of starlight that does not stop until it finds its mark.",
                SkillKind.Projectile, 1, 40, 14f, 5f, 0.3f, new Color(1f, 1f, 0.7f), SkillFxKind.Bolt,
                ClassDataBuilder.P("sx_star_archers_vow", ElementType.Light, 0.35f, 1, 0f, 55f, 60f, 0.6f, 6, new Color(1f, 1f, 0.75f), true, 60f)),
                Rarity.Legendary, "Vow Carved in a Bowstave", "'I will miss nothing. Not even you.'"),
            S(ClassDataBuilder.Buff("sx_last_citys_wall", "Last City's Wall", "Stand like the walls that held when everything else fell.", 1, 30, 40f, BuffStat.Defense, 0.5f, 0.04f, 15f,
                new Color(0.6f, 0.6f, 0.75f), SkillFxKind.Barrier),
                Rarity.Epic, "Brick From the Last City", "Someone scratched a name into it. It is yours."),
            S(SummonSkill("sx_requiem_of_echoes", "Requiem of Echoes", "Two echoes of yourself step out of the air and fight until they fade.", "hollow_echo", 2),
                Rarity.Legendary, "Score for Two Voices", "Both parts are written in your handwriting."),
            S(ClassDataBuilder.Active("sx_thunder_hymn", "Thunder Hymn", "Raise your hand and the sky answers, again and again.",
                SkillKind.Aoe, 1, 42, 16f, 3.2f, 0.2f, new Color(1f, 0.95f, 0.35f), SkillFxKind.Thunder,
                ClassDataBuilder.M("sx_thunder_hymn", ElementType.Lightning, 0.3f, 0.3f, 0.4f, new Vector3(10f, 3f, 10f), Centre, 6f, 55f, 0.12f, 0f, SwingKind.Slam)),
                Rarity.Epic, "Storm-Stained Psalter", "The lightning marks on the cover form a word if you squint."),
            S(ClassDataBuilder.Trap("sx_sandglass_halt", "Sandglass Halt", "Bury an hourglass. When it breaks, time stops for whoever stepped on it.", 1, 25, 14f, 2.4f, 0.2f, 3.5f,
                new Color(0.9f, 0.75f, 0.45f),
                ClassDataBuilder.M("sx_sandglass_halt", ElementType.Earth, 0.05f, 0.05f, 0.2f, new Vector3(7f, 2f, 7f), Centre, 1f, 80f, 0.08f, 0f, SwingKind.Thrust)),
                Rarity.Rare, "Timekeeper's Spare Glass", "The sand inside runs upward."),
            S(ClassDataBuilder.Active("sx_wind_scar", "Wind Scar", "A cut so fast the wind keeps bleeding after you sheathe.",
                SkillKind.Melee, 1, 22, 6f, 2.8f, 0.18f, new Color(0.65f, 1f, 0.75f), SkillFxKind.Cleave,
                ClassDataBuilder.M("sx_wind_scar", ElementType.Wind, 0.08f, 0.12f, 0.25f, new Vector3(5f, 1.6f, 3.5f), new Vector3(0, 1f, 2f), 6f, 30f, 0.07f, 8f, SwingKind.Horizontal)),
                Rarity.Rare, "Torn Sword Manual", "Only one page survived. It is the important one."),
            S(ClassDataBuilder.Heal("sx_phoenix_feather", "Phoenix Feather", "Burn bright, then rise again — most of your wounds go with the ash.", 1, 60, 90f, 0.75f, new Color(1f, 0.6f, 0.2f)),
                Rarity.Legendary, "A Single Burning Feather", "It has been burning for a very long time. It is not hot."),
            S(ClassDataBuilder.Active("sx_codex_glitch", "C0dex Gl1tch", "Something in the Codex skips. For a moment, the world around you is not rendered.",
                SkillKind.Aoe, 1, 66, 33f, 6.6f, 0.33f, new Color(0.1f, 0.1f, 0.1f), SkillFxKind.Storm,
                ClassDataBuilder.M("sx_codex_glitch", ElementType.Void, 0.33f, 0.33f, 0.33f, new Vector3(9f, 3f, 9f), Centre, 6f, 66f, 0.2f, 0f, SwingKind.DoubleSpin)),
                Rarity.Mythic, "[ ERR: page not found ]", "The cover reads differently every time you look at it."),
            S(ClassDataBuilder.Buff("sx_name_unspoken", "Name Unspoken", "Say nothing. Every weakness in front of you shows itself.", 1, 30, 40f, BuffStat.Crit, 0.3f, 0.02f, 14f,
                new Color(0.4f, 0.3f, 0.55f), SkillFxKind.Shadow),
                Rarity.Epic, "Blank Nameplate", "Taken from a door in the academy that nobody remembers."),
            S(ClassDataBuilder.Active("sx_echoing_blade", "Echoing Blade", "Every cut you make is made twice — once now, once a moment later.",
                SkillKind.Aoe, 1, 30, 9f, 2.6f, 0.18f, new Color(0.85f, 0.85f, 0.95f), SkillFxKind.Spin,
                ClassDataBuilder.M("sx_echoing_blade", ElementType.Physical, 0.08f, 0.5f, 0.3f, new Vector3(5.5f, 1.8f, 5.5f), Centre, 5f, 35f, 0.06f, 2f, SwingKind.DoubleSpin)),
                Rarity.Epic, "Duelist's Mirror Notes", "The handwriting on the left page mirrors the right."),
            S(ClassDataBuilder.Active("sx_frostbound_arrow", "Frostbound Arrow", "An arrow of old ice that passes through three foes and freezes the fourth.",
                SkillKind.Projectile, 1, 26, 7f, 2.8f, 0.18f, new Color(0.6f, 0.9f, 1f), SkillFxKind.Frost,
                ClassDataBuilder.P("sx_frostbound_arrow", ElementType.Ice, 0.2f, 1, 0f, 45f, 45f, 0.45f, 3, new Color(0.65f, 0.9f, 1f), false, 40f)),
                Rarity.Rare, "Quiver Tag, Frostveil Patrol", "'Return to sender if found.' There is no sender."),
            S(ClassDataBuilder.Active("sx_the_first_line", "The First Line", "Write the first line of a new story. Everything near you must read it.",
                SkillKind.Aoe, 1, 80, 60f, 8f, 0.4f, new Color(1f, 0.97f, 0.85f), SkillFxKind.Smite,
                ClassDataBuilder.M("sx_the_first_line", ElementType.Light, 0.6f, 0.3f, 0.6f, new Vector3(14f, 5f, 14f), Centre, 10f, 120f, 0.2f, 0f, SwingKind.Slam), ultimate: false),
                Rarity.Mythic, "The First Page", "It starts with your name. It does not end."),
        };
        return list;
    }

    static SkillData S(SkillData s, Rarity rarity, string book, string lore)
    {
        s.unlockLevel = 1;
        EditorUtility.SetDirty(s);
        Books[s.id] = (rarity, book, lore);
        return s;
    }

    static SkillData SummonSkill(string id, string name, string desc, string body, int count)
    {
        SkillData s = ClassDataBuilder.Buff(id, name, desc, 1, 50, 60f, BuffStat.Attack, 0f, 0f, 1f, new Color(0.85f, 0.85f, 0.95f), SkillFxKind.Shadow);
        s.kind = SkillKind.Summon;
        s.summonType = body;
        s.summonCount = count;
        return s;
    }

    static ItemData Book(SkillData s)
    {
        var (rarity, title, lore) = Books[s.id];
        var i = LoadOrCreate<ItemData>($"{ItemDir}/book_{s.id}.asset");
        i.id = "book_" + s.id;
        i.displayName = title;
        i.description = $"{lore}\n\nRead it to learn: {s.displayName}.";
        i.kind = ItemKind.SkillBook;
        i.rarity = rarity;
        i.maxStack = 1;
        i.sellPrice = 0;
        i.canSell = false;
        i.slot = EquipSlot.None;
        i.teachesSkill = s.id;
        i.runeId = "";
        EditorUtility.SetDirty(i);
        return i;
    }

    // --- Runes ------------------------------------------------------------------------------------------

    static List<SkillRuneData> Runes() => new List<SkillRuneData>
    {
        R("rune_overcharge", "Overcharge Rune", "+40% damage. Costs 50% more MP.", RuneEffect.Overcharge, 0.4f, 0.5f, Rarity.Rare),
        R("rune_furnace_heart", "Heart of the Furnace", "+90% damage. Costs twice the MP. It beats.", RuneEffect.Overcharge, 0.9f, 1f, Rarity.Mythic),
        R("rune_quiet", "Quiet Rune", "Costs 30% less MP.", RuneEffect.Thrift, 0.3f, 0f, Rarity.Uncommon, any: true),
        R("rune_hermit", "Hermit's Rune", "Costs 55% less MP. It asks for nothing.", RuneEffect.Thrift, 0.55f, 0f, Rarity.Epic, any: true),
        R("rune_quickening", "Quickening Rune", "Cooldown 20% shorter.", RuneEffect.Haste, 0.2f, 0f, Rarity.Uncommon, any: true),
        R("rune_hourglass", "Hourglass Rune", "Cooldown 40% shorter. The sand falls sideways.", RuneEffect.Haste, 0.4f, 0f, Rarity.Legendary, any: true),
        R("rune_splinter", "Splinter Rune", "Projectile skills fire 2 more shots.", RuneEffect.Split, 2f, 0f, Rarity.Rare, kinds: SkillKind.Projectile),
        R("rune_hydra", "Hydra Rune", "Projectile skills fire 4 more shots.", RuneEffect.Split, 4f, 0f, Rarity.Legendary, kinds: SkillKind.Projectile),
        R("rune_needle", "Needle Rune", "Projectiles pass through 2 more foes.", RuneEffect.Pierce, 2f, 0f, Rarity.Uncommon, kinds: SkillKind.Projectile),
        R("rune_echo", "Echo Rune", "Every hit echoes 0.2 s later for 40% damage.", RuneEffect.Echo, 0.4f, 0f, Rarity.Epic),
        R("rune_twin_moon", "Twin Moon Rune", "Every hit echoes for 75% damage. Two shadows, one blade.", RuneEffect.Echo, 0.75f, 0f, Rarity.Mythic),
        R("rune_ember", "Rune of Embers", "The skill becomes Fire.", RuneEffect.Infuse, 0f, 0f, Rarity.Rare, ElementType.Fire),
        R("rune_hoarfrost", "Rune of Hoarfrost", "The skill becomes Ice.", RuneEffect.Infuse, 0f, 0f, Rarity.Rare, ElementType.Ice),
        R("rune_gale", "Gale Rune", "The skill becomes Wind.", RuneEffect.Infuse, 0f, 0f, Rarity.Rare, ElementType.Wind),
        R("rune_deepstone", "Rune of the Deep Stone", "The skill becomes Earth.", RuneEffect.Infuse, 0f, 0f, Rarity.Rare, ElementType.Earth),
        R("rune_stormglass", "Stormglass Rune", "The skill becomes Lightning.", RuneEffect.Infuse, 0f, 0f, Rarity.Rare, ElementType.Lightning),
        R("rune_umbral", "Umbral Rune", "The skill becomes Shadow.", RuneEffect.Infuse, 0f, 0f, Rarity.Epic, ElementType.Shadow),
        R("rune_dawnmark", "Dawnmark Rune", "The skill becomes Light.", RuneEffect.Infuse, 0f, 0f, Rarity.Epic, ElementType.Light),
        R("rune_unwritten", "Rune of the Unwritten", "The skill becomes Void. It should not exist.", RuneEffect.Infuse, 0f, 0f, Rarity.Mythic, ElementType.Void),
        R("rune_headsman", "Headsman's Rune", "+70% damage against foes under 30% health.", RuneEffect.Executioner, 0.7f, 0f, Rarity.Epic),
        R("rune_leech", "Leech Rune", "Heals you for 6% of the damage dealt.", RuneEffect.Leech, 0.06f, 0f, Rarity.Rare),
        R("rune_crimson_pact", "Crimson Pact", "Heals you for 14% of the damage dealt. Signed in red.", RuneEffect.Leech, 0.14f, 0f, Rarity.Legendary),
        R("rune_keen", "Keen Rune", "25% chance for a guaranteed critical hit.", RuneEffect.Keen, 0.25f, 0f, Rarity.Rare),
        R("rune_wide", "Wide Rune", "The strike area is 50% larger.", RuneEffect.Vast, 0.5f, 0f, Rarity.Uncommon, kinds: new[] { SkillKind.Melee, SkillKind.Aoe, SkillKind.Dash }),
        R("rune_colossus", "Colossus Rune", "The strike area is twice as large.", RuneEffect.Vast, 1f, 0f, Rarity.Legendary, kinds: new[] { SkillKind.Melee, SkillKind.Aoe, SkillKind.Dash }),
        R("rune_lingering", "Lingering Rune", "Buffs and stealth last 60% longer.", RuneEffect.Lingering, 0.6f, 0f, Rarity.Rare, kinds: new[] { SkillKind.Buff, SkillKind.Stealth }),
        R("rune_mending", "Mending Rune", "Healing skills heal 50% more.", RuneEffect.Mending, 0.5f, 0f, Rarity.Rare, kinds: SkillKind.Heal),
        R("rune_thunderclap", "Thunderclap Rune", "Twice the stagger: foes break faster.", RuneEffect.Thunderclap, 1f, 0f, Rarity.Uncommon),
    };

    static SkillRuneData R(string id, string name, string desc, RuneEffect effect, float value, float value2, Rarity rarity,
        ElementType element = ElementType.Physical, bool any = false, SkillKind[] kinds = null) =>
        Rune(id, name, desc, effect, value, value2, rarity, element, any, kinds);

    static SkillRuneData R(string id, string name, string desc, RuneEffect effect, float value, float value2, Rarity rarity, SkillKind kinds) =>
        Rune(id, name, desc, effect, value, value2, rarity, ElementType.Physical, false, new[] { kinds });

    static readonly Dictionary<string, Rarity> RuneRarity = new Dictionary<string, Rarity>();

    static SkillRuneData Rune(string id, string name, string desc, RuneEffect effect, float value, float value2, Rarity rarity,
        ElementType element, bool any, SkillKind[] kinds)
    {
        var r = LoadOrCreate<SkillRuneData>($"{RuneDir}/{id}.asset");
        r.id = id; r.displayName = name; r.description = desc; r.effect = effect; r.value = value; r.value2 = value2; r.element = element;
        r.fits = new List<SkillKind>();
        if (any) r.fits.AddRange(new[] { SkillKind.Melee, SkillKind.Dash, SkillKind.Aoe, SkillKind.Projectile, SkillKind.Buff, SkillKind.Heal, SkillKind.Stealth, SkillKind.Trap, SkillKind.Summon, SkillKind.ShadowCall });
        else if (kinds != null) r.fits.AddRange(kinds);
        RuneRarity[id] = rarity;
        EditorUtility.SetDirty(r);
        return r;
    }

    static ItemData RuneItem(SkillRuneData r)
    {
        var i = LoadOrCreate<ItemData>($"{ItemDir}/{r.id}.asset");
        i.id = r.id;
        i.displayName = r.displayName;
        i.description = r.description + "\n\nSocket it into a skill (Status → Skills).";
        i.kind = ItemKind.Rune;
        i.rarity = RuneRarity[r.id];
        i.maxStack = 9;
        i.sellPrice = 20 * (1 + (int)i.rarity * (int)i.rarity);
        i.canSell = true;
        i.slot = EquipSlot.None;
        i.runeId = r.id;
        i.teachesSkill = "";
        EditorUtility.SetDirty(i);
        return i;
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
