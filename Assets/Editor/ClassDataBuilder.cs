// Writes the stat tuning and the Paladin (id "vanguard") class with its skills and skill combos
// (port of the Godot data/classes/vanguard.tres and data/skills/vanguard/*.tres). Hitbox offsets
// are flipped to Unity's +Z forward. Assets are updated in place.
using System.Collections.Generic;
using System.IO;
using Aethvirse.Combat;
using Aethvirse.Skills;
using Aethvirse.Stats;
using UnityEditor;
using UnityEngine;

public static class ClassDataBuilder
{
    const string TuningPath = "Assets/Data/Balancing/StatTuning.asset";
    const string ClassDir = "Assets/Data/Classes";

    [MenuItem("AETHVIRSE/Build Class Data")]
    public static void Build()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(TuningPath));
        Directory.CreateDirectory(ClassDir);
        foreach (string d in new[] { "Paladin", "Assassin", "Mage", "Marksman" }) Directory.CreateDirectory("Assets/Data/Skills/" + d);
        LoadOrCreate<StatTuning>(TuningPath); // defaults are the sheet values
        BuildPaladin();
        BuildAssassin();
        BuildMage();
        BuildMarksman();
        Directory.CreateDirectory("Assets/Resources");
        var reg = LoadOrCreate<ClassRegistry>("Assets/Resources/ClassRegistry.asset");
        reg.classes = new List<ClassData>();
        foreach (string f in new[] { "Paladin", "Assassin", "Mage", "Marksman" }) reg.classes.Add(AssetDatabase.LoadAssetAtPath<ClassData>($"{ClassDir}/{f}.asset"));
        EditorUtility.SetDirty(reg);
        AssetDatabase.SaveAssets();
        Debug.Log("AETHVIRSE: class data built");
    }

    // Melee / AoE / dash timeline (Unity +Z forward offsets).
    internal static AttackData M(string id, ElementType e, float startup, float active, float recovery, Vector3 size, Vector3 offset,
        float knock, float stagger, float hitStop, float lunge, SwingKind swing) =>
        Atk(id, e, startup, active, recovery, startup + active + recovery * 0.6f, startup + active * 0.5f, size, offset, knock, stagger, hitStop, lunge, swing);

    // Projectile timeline.
    internal static AttackData P(string id, ElementType e, float startup, int count, float spread, float speed, float range, float radius, int pierce,
        Color color, bool homing, float stagger = 20f)
    {
        AttackData a = Atk(id, e, startup, 0.05f, 0.3f, startup + 0.3f, startup, new Vector3(1.5f, 1.5f, 1.5f), new Vector3(0, 1f, 1f), 3f, stagger, 0.06f, 0f, SwingKind.Thrust);
        a.ranged = true; a.projectileCount = count; a.projectileSpread = spread; a.projectileSpeed = speed; a.projectileRange = range;
        a.projectileRadius = radius; a.projectilePierce = pierce; a.projectileColor = color; a.homing = homing;
        return a;
    }

    static readonly Vector3 Close = new Vector3(3f, 1.6f, 2.6f), CloseAt = new Vector3(0, 1f, 1.4f);
    static readonly Vector3 Around = new Vector3(6f, 2f, 6f), Centre = new Vector3(0, 1f, 0f);

    static void BuildPaladin()
    {
        _dir = "Assets/Data/Skills/Paladin";
        var skills = new List<SkillData>
        {
            Active("iron_charge", "Iron Charge", "Dash forward, ramming everything in the way.", SkillKind.Dash, 1, 20, 8f, 1.6f, 0.1f,
                new Color(0.75f, 0.8f, 0.9f), SkillFxKind.Charge, M("iron_charge", ElementType.Physical, 0.08f, 0.35f, 0.25f, new Vector3(1.8f, 1.6f, 2f), new Vector3(0, 1f, 0.8f), 6f, 40f, 0.06f, 22f, SwingKind.Thrust)),
            Active("rending_slash", "Rending Slash", "A heavy forward cleave that tears through guards.", SkillKind.Melee, 1, 15, 5f, 2.2f, 0.15f,
                new Color(0.45f, 0.75f, 1f), SkillFxKind.Cleave, M("rending_slash", ElementType.Physical, 0.14f, 0.14f, 0.32f, new Vector3(3.2f, 1.6f, 2.6f), new Vector3(0, 1f, 1.5f), 4f, 30f, 0.08f, 5f, SwingKind.Horizontal)),
            Passive("iron_body", "Iron Body", "Passive. Defense up.", 2, BuffStat.Defense, 0.06f, 0.012f),
            Active("earthsplitter", "Earthsplitter", "Slam the ground and split the earth around you.", SkillKind.Aoe, 3, 25, 10f, 2f, 0.12f,
                new Color(0.8f, 0.6f, 0.3f), SkillFxKind.GroundSplit, M("earthsplitter", ElementType.Earth, 0.35f, 0.15f, 0.45f, new Vector3(8f, 2f, 8f), new Vector3(0, 0.8f, 1f), 7f, 45f, 0.1f, 0f, SwingKind.Vertical)),
            Passive("heavy_hands", "Heavy Hands", "Passive. Attack up.", 4, BuffStat.Attack, 0.04f, 0.008f),
            Buff("war_cry", "War Cry", "A roar that sharpens your strikes. ATK up for a while.", 5, 20, 25f, BuffStat.Attack, 0.2f, 0.02f, 10f, new Color(1f, 0.45f, 0.3f), SkillFxKind.Roar),
            Passive("battle_rhythm", "Battle Rhythm", "Passive. Attack speed up.", 6, BuffStat.AttackSpeed, 0.04f, 0.008f),
            Active("flame_edge", "Flame Edge", "Ignite the blade and carve a burning arc.", SkillKind.Melee, 8, 18, 7f, 1.8f, 0.12f,
                new Color(1f, 0.55f, 0.15f), SkillFxKind.FlameArc, M("flame_edge", ElementType.Fire, 0.12f, 0.2f, 0.3f, new Vector3(3f, 1.5f, 2.4f), new Vector3(0, 1f, 1.3f), 3f, 25f, 0.07f, 4f, SwingKind.Diagonal)),
            Active("titans_verdict", "Titan's Verdict", "Ultimate. Bring the sky down on everything nearby.", SkillKind.Aoe, 10, 60, 60f, 5f, 0.3f,
                new Color(1f, 0.85f, 0.3f), SkillFxKind.SkyFall, M("titans_verdict", ElementType.Physical, 0.6f, 0.25f, 0.6f, new Vector3(12f, 3f, 12f), Centre, 10f, 100f, 0.16f, 0f, SwingKind.Slam), ultimate: true),
            Active("shield_wave", "Shield Wave", "Drive your guard into the ground: a ring of force throws foes back.", SkillKind.Aoe, 12, 22, 9f, 1.5f, 0.1f,
                new Color(0.7f, 0.85f, 1f), SkillFxKind.Nova, M("shield_wave", ElementType.Physical, 0.15f, 0.12f, 0.35f, Around, Centre, 9f, 35f, 0.07f, 0f, SwingKind.Vertical)),
            Buff("holy_ward", "Holy Ward", "Light hardens around you. DEF up for a while.", 14, 25, 30f, BuffStat.Defense, 0.35f, 0.03f, 12f, new Color(1f, 0.95f, 0.6f), SkillFxKind.Barrier),
            Active("judgement", "Judgement", "Call a pillar of light down in front of you.", SkillKind.Aoe, 16, 30, 12f, 3f, 0.2f,
                new Color(1f, 0.9f, 0.5f), SkillFxKind.Smite, M("judgement", ElementType.Light, 0.4f, 0.15f, 0.4f, new Vector3(3.5f, 4f, 3.5f), new Vector3(0, 1f, 3f), 5f, 50f, 0.12f, 0f, SwingKind.Vertical)),
            Active("whirling_blade", "Whirling Blade", "Spin with the greatsword, cutting everything around you.", SkillKind.Aoe, 18, 24, 8f, 2.2f, 0.15f,
                new Color(0.8f, 0.8f, 0.9f), SkillFxKind.Spin, M("whirling_blade", ElementType.Physical, 0.1f, 0.45f, 0.35f, new Vector3(5f, 1.6f, 5f), Centre, 5f, 30f, 0.06f, 1f, SwingKind.DoubleSpin)),
            Heal("rallying_light", "Rallying Light", "A warm light closes your wounds.", 20, 35, 30f, 0.3f, new Color(0.6f, 1f, 0.6f)),
            Active("thunder_drive", "Thunder Drive", "Plant the blade; lightning answers all around.", SkillKind.Aoe, 25, 35, 14f, 2.8f, 0.2f,
                new Color(1f, 0.95f, 0.3f), SkillFxKind.Thunder, M("thunder_drive", ElementType.Lightning, 0.3f, 0.2f, 0.4f, new Vector3(9f, 3f, 9f), Centre, 6f, 45f, 0.1f, 0f, SwingKind.Slam)),
        };
        Class("Paladin", "vanguard", "Paladin", "Holy knight. Heavy sword combos, high HP and DEF.", false,
            new StatBlock(12f, 6f, 3f, 11f, 4f), new StatBlock(2f, 0.6f, 0.2f, 1.8f, 0.4f), new StatBlock(0.5f, 0.1f, 0f, 0.35f, 0.05f),
            "Assets/Data/Combat/Greatsword/GreatswordCombo.asset", skills,
            Combo("charge_breaker", "Breaker", "iron_charge", "rending_slash", 2f, 1f, true, 0f),
            Combo("fury_quake", "Fury Quake", "war_cry", "earthsplitter", 4f, 1.5f, false, 0f),
            Combo("molten_rift", "Molten Rift", "flame_edge", "earthsplitter", 2.5f, 1.3f, false, 20f),
            Combo("dawn_verdict", "Dawn Verdict", "holy_ward", "judgement", 4f, 1.4f, true, 0f));
    }

    static void BuildAssassin()
    {
        _dir = "Assets/Data/Skills/Assassin";
        Color shadow = new Color(0.55f, 0.35f, 0.85f);
        var skills = new List<SkillData>
        {
            Active("shadow_step", "Shadow Step", "Vanish and reappear past your target, cutting on the way.", SkillKind.Dash, 1, 15, 6f, 1.5f, 0.1f,
                shadow, SkillFxKind.Blink, M("shadow_step", ElementType.Shadow, 0.05f, 0.2f, 0.2f, new Vector3(1.6f, 1.6f, 2.4f), new Vector3(0, 1f, 0.6f), 3f, 25f, 0.05f, 26f, SwingKind.Thrust)),
            Active("rending_fangs", "Rending Fangs", "A flurry of crossing cuts.", SkillKind.Melee, 1, 12, 5f, 2f, 0.14f,
                new Color(0.85f, 0.3f, 0.35f), SkillFxKind.Cleave, M("rending_fangs", ElementType.Physical, 0.08f, 0.25f, 0.25f, Close, CloseAt, 2f, 20f, 0.05f, 4f, SwingKind.Diagonal)),
            Passive("keen_eye", "Keen Eye", "Passive. Critical chance up.", 2, BuffStat.Crit, 0.03f, 0.005f),
            Stealth("vanish", "Vanish", "Fade from sight. Enemies lose you; your first strike from the dark is a guaranteed critical.", 3, 20, 18f, 5f, new Color(0.25f, 0.2f, 0.35f)),
            Passive("light_feet", "Light Feet", "Passive. Move speed up.", 4, BuffStat.MoveSpeed, 0.04f, 0.006f),
            Active("fan_of_knives", "Fan of Knives", "Throw a fan of knives.", SkillKind.Projectile, 5, 18, 7f, 1.2f, 0.1f,
                new Color(0.8f, 0.85f, 0.9f), SkillFxKind.Volley, P("fan_of_knives", ElementType.Physical, 0.1f, 7, 45f, 34f, 18f, 0.25f, 0, new Color(0.85f, 0.9f, 1f), false)),
            Passive("twin_mastery", "Twin Mastery", "Passive. Attack speed up.", 6, BuffStat.AttackSpeed, 0.05f, 0.01f),
            Trap("caltrops", "Caltrops", "Scatter barbed caltrops; they bite the first foes to step in.", 8, 15, 10f, 1.6f, 0.12f, 2.5f, new Color(0.6f, 0.6f, 0.65f),
                M("caltrops", ElementType.Physical, 0.05f, 0.05f, 0.2f, Around, Centre, 2f, 30f, 0.05f, 0f, SwingKind.Thrust)),
            Active("death_blossom", "Death Blossom", "Spin through a circle of blades.", SkillKind.Aoe, 10, 25, 9f, 2.4f, 0.15f,
                new Color(0.9f, 0.4f, 0.6f), SkillFxKind.Spin, M("death_blossom", ElementType.Physical, 0.08f, 0.4f, 0.3f, new Vector3(5f, 1.6f, 5f), Centre, 4f, 25f, 0.05f, 2f, SwingKind.DoubleSpin)),
            Active("shadow_strike", "Shadow Strike", "A pillar of shadow erupts under your target.", SkillKind.Aoe, 12, 28, 10f, 3f, 0.2f,
                shadow, SkillFxKind.Pillar, M("shadow_strike", ElementType.Shadow, 0.25f, 0.15f, 0.35f, new Vector3(3f, 4f, 3f), new Vector3(0, 1f, 3f), 4f, 40f, 0.1f, 0f, SwingKind.Rising)),
            Buff("adrenaline", "Adrenaline", "Your heart races. Attack speed up for a while.", 14, 20, 25f, BuffStat.AttackSpeed, 0.25f, 0.02f, 10f, new Color(1f, 0.35f, 0.3f), SkillFxKind.Roar),
            Active("frost_kunai", "Frost Kunai", "A kunai of ice that seeks its mark.", SkillKind.Projectile, 16, 14, 4f, 1.8f, 0.12f,
                new Color(0.55f, 0.85f, 1f), SkillFxKind.Bolt, P("frost_kunai", ElementType.Ice, 0.08f, 1, 0f, 40f, 30f, 0.3f, 1, new Color(0.6f, 0.9f, 1f), true)),
            Buff("night_hunt", "Night Hunt", "You see every weakness. Critical chance up.", 18, 25, 30f, BuffStat.Crit, 0.15f, 0.01f, 12f, new Color(0.4f, 0.3f, 0.6f), SkillFxKind.Nova),
            Active("thunder_step", "Thunder Step", "Dash in a crack of lightning.", SkillKind.Dash, 20, 22, 8f, 2.2f, 0.15f,
                new Color(1f, 0.95f, 0.4f), SkillFxKind.Thunder, M("thunder_step", ElementType.Lightning, 0.05f, 0.25f, 0.25f, new Vector3(2f, 1.6f, 2.6f), new Vector3(0, 1f, 0.8f), 4f, 30f, 0.06f, 24f, SwingKind.Thrust)),
            Active("thousand_cuts", "Thousand Cuts", "Ultimate. Become a storm of blades.", SkillKind.Aoe, 10, 60, 60f, 6f, 0.3f,
                new Color(0.9f, 0.3f, 0.9f), SkillFxKind.Storm, M("thousand_cuts", ElementType.Shadow, 0.3f, 0.6f, 0.5f, new Vector3(9f, 3f, 9f), Centre, 6f, 80f, 0.14f, 0f, SwingKind.DoubleSpin), ultimate: true),
        };
        Class("Assassin", "phantom", "Assassin", "Fast, critical, unseen. Twin daggers.", false,
            new StatBlock(7f, 12f, 4f, 7f, 8f), new StatBlock(0.8f, 2f, 0.3f, 1f, 0.9f), new StatBlock(0.2f, 0.5f, 0f, 0.15f, 0.15f),
            "Assets/Data/Combat/Daggers/DaggersCombo.asset", skills,
            Combo("ambush", "Ambush", "vanish", "shadow_strike", 6f, 1.5f, true, 0f),
            Combo("blink_rend", "Blink Rend", "shadow_step", "rending_fangs", 2f, 1.2f, true, 0f),
            Combo("bloom_field", "Bloom Field", "caltrops", "death_blossom", 4f, 1.3f, false, 15f));
    }

    static void BuildMage()
    {
        _dir = "Assets/Data/Skills/Mage";
        var skills = new List<SkillData>
        {
            Active("fireball", "Fireball", "A burning orb that seeks its target.", SkillKind.Projectile, 1, 14, 3f, 1.9f, 0.14f,
                new Color(1f, 0.5f, 0.15f), SkillFxKind.Bolt, P("fireball", ElementType.Fire, 0.15f, 1, 0f, 26f, 28f, 0.5f, 0, new Color(1f, 0.5f, 0.15f), true, 25f)),
            Active("frost_nova", "Frost Nova", "Ice bursts from the ground around you.", SkillKind.Aoe, 1, 18, 8f, 1.6f, 0.12f,
                new Color(0.55f, 0.85f, 1f), SkillFxKind.Frost, M("frost_nova", ElementType.Ice, 0.2f, 0.15f, 0.35f, Around, Centre, 3f, 30f, 0.06f, 0f, SwingKind.Vertical)),
            Passive("arcane_mind", "Arcane Mind", "Passive. Attack up.", 2, BuffStat.Attack, 0.05f, 0.01f),
            Active("chain_lightning", "Chain Lightning", "A bolt that leaps through several foes.", SkillKind.Projectile, 3, 20, 6f, 1.7f, 0.12f,
                new Color(1f, 0.95f, 0.35f), SkillFxKind.Thunder, P("chain_lightning", ElementType.Lightning, 0.12f, 1, 0f, 50f, 30f, 0.4f, 4, new Color(1f, 1f, 0.5f), true)),
            Passive("focus", "Focus", "Passive. Critical chance up.", 4, BuffStat.Crit, 0.03f, 0.005f),
            Active("wind_blades", "Wind Blades", "A fan of cutting wind.", SkillKind.Projectile, 5, 18, 6f, 1.1f, 0.1f,
                new Color(0.6f, 1f, 0.7f), SkillFxKind.Volley, P("wind_blades", ElementType.Wind, 0.12f, 5, 35f, 30f, 20f, 0.35f, 1, new Color(0.7f, 1f, 0.8f), false)),
            Passive("spell_flow", "Spell Flow", "Passive. Attack speed up.", 6, BuffStat.AttackSpeed, 0.04f, 0.008f),
            Active("blink", "Blink", "Step through space.", SkillKind.Dash, 8, 15, 7f, 0.8f, 0.05f,
                new Color(0.7f, 0.6f, 1f), SkillFxKind.Blink, M("blink", ElementType.Physical, 0.03f, 0.15f, 0.15f, new Vector3(1.4f, 1.4f, 1.4f), new Vector3(0, 1f, 0.6f), 1f, 5f, 0.02f, 30f, SwingKind.Thrust)),
            Active("meteor", "Meteor", "A falling star strikes the ground ahead.", SkillKind.Aoe, 10, 35, 12f, 3.2f, 0.22f,
                new Color(1f, 0.4f, 0.1f), SkillFxKind.Pillar, M("meteor", ElementType.Fire, 0.55f, 0.15f, 0.4f, new Vector3(4.5f, 4f, 4.5f), new Vector3(0, 1f, 4f), 7f, 55f, 0.14f, 0f, SwingKind.Vertical)),
            Buff("arcane_barrier", "Arcane Barrier", "A shell of force. DEF up for a while.", 12, 25, 25f, BuffStat.Defense, 0.4f, 0.03f, 10f, new Color(0.6f, 0.7f, 1f), SkillFxKind.Barrier),
            Active("earth_spike", "Earth Spike", "Stone spears burst from the ground.", SkillKind.Aoe, 14, 24, 9f, 2.2f, 0.15f,
                new Color(0.75f, 0.55f, 0.3f), SkillFxKind.GroundSplit, M("earth_spike", ElementType.Earth, 0.3f, 0.15f, 0.35f, new Vector3(6f, 2f, 6f), new Vector3(0, 0.8f, 2f), 6f, 40f, 0.1f, 0f, SwingKind.Rising)),
            Trap("gravity_well", "Gravity Well", "A well of pull that bursts when something steps in.", 16, 25, 14f, 2.4f, 0.15f, 3.5f, new Color(0.4f, 0.3f, 0.8f),
                M("gravity_well", ElementType.Void, 0.05f, 0.05f, 0.2f, Around, Centre, -6f, 40f, 0.08f, 0f, SwingKind.Thrust)),
            Heal("mend", "Mend", "Mend flesh with aether.", 18, 30, 25f, 0.25f, new Color(0.6f, 1f, 0.8f)),
            Active("arcane_nova", "Arcane Nova", "Raw aether explodes outward.", SkillKind.Aoe, 20, 35, 12f, 2.8f, 0.2f,
                new Color(0.8f, 0.6f, 1f), SkillFxKind.Nova, M("arcane_nova", ElementType.Void, 0.25f, 0.15f, 0.4f, new Vector3(8f, 2.4f, 8f), Centre, 8f, 50f, 0.1f, 0f, SwingKind.Spin)),
            Active("cataclysm", "Cataclysm", "Ultimate. Tear the sky open above your enemies.", SkillKind.Aoe, 10, 70, 60f, 6.5f, 0.35f,
                new Color(1f, 0.5f, 1f), SkillFxKind.Storm, M("cataclysm", ElementType.Lightning, 0.7f, 0.4f, 0.6f, new Vector3(14f, 4f, 14f), new Vector3(0, 1f, 3f), 8f, 90f, 0.16f, 0f, SwingKind.Rising), ultimate: true),
        };
        Class("Mage", "arcanist", "Mage", "Elements at your command. Ranged area damage.", true,
            new StatBlock(3f, 5f, 13f, 7f, 7f), new StatBlock(0.2f, 0.6f, 2.2f, 1f, 0.8f), new StatBlock(0f, 0.1f, 0.6f, 0.2f, 0.1f),
            "Assets/Data/Combat/Staff/StaffCombo.asset", skills,
            Combo("melt", "Melt", "frost_nova", "fireball", 3f, 2f, false, 0f),
            Combo("tempest", "Tempest", "chain_lightning", "wind_blades", 3f, 1.4f, false, 10f),
            Combo("crush", "Crush", "gravity_well", "meteor", 5f, 1.5f, true, 0f));
    }

    static void BuildMarksman()
    {
        _dir = "Assets/Data/Skills/Marksman";
        var skills = new List<SkillData>
        {
            Active("power_shot", "Power Shot", "A heavy arrow that punches through.", SkillKind.Projectile, 1, 14, 4f, 2.2f, 0.15f,
                new Color(1f, 0.85f, 0.5f), SkillFxKind.Bolt, P("power_shot", ElementType.Physical, 0.25f, 1, 0f, 60f, 44f, 0.35f, 3, new Color(1f, 0.85f, 0.5f), false, 30f)),
            Active("multishot", "Multishot", "Loose a spread of arrows.", SkillKind.Projectile, 1, 16, 6f, 1.1f, 0.1f,
                new Color(0.95f, 0.9f, 0.75f), SkillFxKind.Volley, P("multishot", ElementType.Physical, 0.12f, 7, 40f, 42f, 26f, 0.25f, 0, new Color(0.95f, 0.9f, 0.75f), false)),
            Passive("steady_aim", "Steady Aim", "Passive. Critical chance up.", 2, BuffStat.Crit, 0.03f, 0.005f),
            Active("roll_back", "Roll Back", "Tumble backwards out of reach.", SkillKind.Dash, 3, 10, 5f, 0.6f, 0.05f,
                new Color(0.7f, 0.9f, 0.7f), SkillFxKind.Blink, M("roll_back", ElementType.Physical, 0.03f, 0.2f, 0.15f, new Vector3(1.4f, 1.4f, 1.4f), new Vector3(0, 1f, 0.6f), 2f, 5f, 0.02f, -20f, SwingKind.Thrust)),
            Passive("swift_draw", "Swift Draw", "Passive. Attack speed up.", 4, BuffStat.AttackSpeed, 0.05f, 0.01f),
            Trap("snare_trap", "Snare Trap", "A spring trap that bites what steps on it.", 5, 15, 10f, 1.8f, 0.12f, 2.2f, new Color(0.6f, 0.5f, 0.3f),
                M("snare_trap", ElementType.Physical, 0.05f, 0.05f, 0.2f, Around, Centre, 1f, 60f, 0.06f, 0f, SwingKind.Thrust)),
            Passive("fleet", "Fleet", "Passive. Move speed up.", 6, BuffStat.MoveSpeed, 0.04f, 0.006f),
            Active("explosive_arrow", "Explosive Arrow", "An arrow that bursts in a ring of fire.", SkillKind.Projectile, 8, 22, 8f, 2.4f, 0.15f,
                new Color(1f, 0.45f, 0.2f), SkillFxKind.Nova, P("explosive_arrow", ElementType.Fire, 0.2f, 1, 0f, 40f, 34f, 0.6f, 2, new Color(1f, 0.5f, 0.2f), true, 35f)),
            Buff("hawk_eye", "Hawk Eye", "Nothing escapes your sight. Critical chance up.", 10, 20, 25f, BuffStat.Crit, 0.15f, 0.01f, 12f, new Color(1f, 0.8f, 0.4f), SkillFxKind.Roar),
            Active("arrow_rain", "Arrow Rain", "Arrows fall on the ground ahead.", SkillKind.Aoe, 12, 28, 10f, 2.6f, 0.18f,
                new Color(0.9f, 0.85f, 0.6f), SkillFxKind.Storm, M("arrow_rain", ElementType.Physical, 0.4f, 0.4f, 0.4f, new Vector3(6f, 3f, 6f), new Vector3(0, 1f, 6f), 2f, 30f, 0.06f, 0f, SwingKind.Rising)),
            Active("frost_arrow", "Frost Arrow", "An arrow of ice that seeks its mark.", SkillKind.Projectile, 14, 16, 5f, 1.9f, 0.12f,
                new Color(0.55f, 0.85f, 1f), SkillFxKind.Frost, P("frost_arrow", ElementType.Ice, 0.15f, 1, 0f, 48f, 36f, 0.3f, 1, new Color(0.6f, 0.9f, 1f), true)),
            Active("thunder_arrow", "Thunder Arrow", "Lightning rides the arrow through a line of foes.", SkillKind.Projectile, 16, 22, 8f, 2.2f, 0.15f,
                new Color(1f, 0.95f, 0.4f), SkillFxKind.Thunder, P("thunder_arrow", ElementType.Lightning, 0.2f, 1, 0f, 60f, 44f, 0.4f, 6, new Color(1f, 1f, 0.5f), false)),
            Stealth("camouflage", "Camouflage", "Blend into the world. Enemies lose you; your next shot is a guaranteed critical.", 18, 20, 20f, 6f, new Color(0.4f, 0.5f, 0.35f)),
            Heal("first_aid", "First Aid", "Bind your wounds quickly.", 20, 25, 25f, 0.25f, new Color(0.6f, 1f, 0.6f)),
            Active("starfall_barrage", "Starfall Barrage", "Ultimate. A sky full of arrows, each seeking a heart.", SkillKind.Projectile, 10, 60, 60f, 2.2f, 0.2f,
                new Color(1f, 0.95f, 0.7f), SkillFxKind.Smite, P("starfall_barrage", ElementType.Light, 0.4f, 15, 80f, 36f, 32f, 0.3f, 1, new Color(1f, 0.95f, 0.7f), true, 30f), ultimate: true),
        };
        Class("Marksman", "ranger", "Marksman", "Bow and traps. Strike from afar, never be caught.", false,
            new StatBlock(6f, 11f, 4f, 7f, 10f), new StatBlock(0.7f, 1.8f, 0.3f, 1f, 1.2f), new StatBlock(0.15f, 0.45f, 0f, 0.15f, 0.25f),
            "Assets/Data/Combat/Bow/BowCombo.asset", skills,
            Combo("pinned", "Pinned", "snare_trap", "power_shot", 4f, 1.3f, true, 0f),
            Combo("eagle_volley", "Eagle Volley", "hawk_eye", "multishot", 4f, 1.4f, false, 0f),
            Combo("parting_shot", "Parting Shot", "roll_back", "explosive_arrow", 2f, 1.4f, true, 0f));
    }

    internal static string _dir = "Assets/Data/Skills/Paladin";

    static SkillComboData Combo(string id, string name, string first, string second, float window, float bonus, bool crit, float stagger) =>
        new SkillComboData { id = id, displayName = name, first = first, second = second, window = window, bonusMultiplier = bonus, forceCrit = crit, extraStagger = stagger };

    static void Class(string file, string id, string name, string desc, bool magic, StatBlock baseStats, StatBlock growth, StatBlock split,
        string comboPath, List<SkillData> skills, params SkillComboData[] combos)
    {
        var c = LoadOrCreate<ClassData>($"{ClassDir}/{file}.asset");
        c.id = id;
        c.displayName = name;
        c.description = desc;
        c.magicDamage = magic;
        c.baseStats = baseStats;
        c.growth = growth;
        c.recommendedSplit = split;
        c.combo = AssetDatabase.LoadAssetAtPath<ComboData>(comboPath);
        c.skills = skills;
        c.skillCombos = new List<SkillComboData>(combos);
        EditorUtility.SetDirty(c);
    }

    internal static SkillData Heal(string id, string name, string desc, int unlock, int mp, float cooldown, float share, Color color)
    {
        SkillData s = Skill(id, name, desc, SkillKind.Heal, unlock, color);
        s.mpCost = mp; s.cooldown = cooldown; s.healShare = share; s.multiplierPerLevel = 0.02f; s.fx = SkillFxKind.Heal;
        return s;
    }

    internal static SkillData Stealth(string id, string name, string desc, int unlock, int mp, float cooldown, float duration, Color color)
    {
        SkillData s = Skill(id, name, desc, SkillKind.Stealth, unlock, color);
        s.mpCost = mp; s.cooldown = cooldown; s.stealthDuration = duration; s.fx = SkillFxKind.Shadow;
        return s;
    }

    internal static SkillData Trap(string id, string name, string desc, int unlock, int mp, float cooldown, float mult, float perLevel, float radius, Color color, AttackData attack)
    {
        SkillData s = Skill(id, name, desc, SkillKind.Trap, unlock, color);
        s.mpCost = mp; s.cooldown = cooldown; s.damageMultiplier = mult; s.multiplierPerLevel = perLevel; s.trapRadius = radius; s.trapLifetime = 15f;
        s.attack = attack; s.fx = SkillFxKind.TrapMark;
        return s;
    }

    internal static SkillData Active(string id, string name, string desc, SkillKind kind, int unlock, int mp, float cooldown,
        float mult, float multPerLevel, Color color, SkillFxKind fx, AttackData attack, bool ultimate = false)
    {
        SkillData s = Skill(id, name, desc, kind, unlock, color);
        s.mpCost = mp;
        s.cooldown = cooldown;
        s.damageMultiplier = mult;
        s.multiplierPerLevel = multPerLevel;
        s.attack = attack;
        s.fx = fx;
        s.isUltimate = ultimate;
        return s;
    }

    internal static SkillData Buff(string id, string name, string desc, int unlock, int mp, float cooldown, BuffStat stat,
        float amount, float perLevel, float duration, Color color, SkillFxKind fx)
    {
        SkillData s = Skill(id, name, desc, SkillKind.Buff, unlock, color);
        s.mpCost = mp;
        s.cooldown = cooldown;
        s.buffStat = stat;
        s.buffAmount = amount;
        s.buffAmountPerLevel = perLevel;
        s.buffDuration = duration;
        s.fx = fx;
        return s;
    }

    internal static SkillData Passive(string id, string name, string desc, int unlock, BuffStat stat, float amount, float perLevel)
    {
        SkillData s = Skill(id, name, desc, SkillKind.Passive, unlock, new Color(0.9f, 0.4f, 0.3f));
        s.mpCost = 0;
        s.buffStat = stat;
        s.buffAmount = amount;
        s.buffAmountPerLevel = perLevel;
        return s;
    }

    internal static SkillData Skill(string id, string name, string desc, SkillKind kind, int unlock, Color color)
    {
        var s = LoadOrCreate<SkillData>($"{_dir}/{id}.asset");
        s.id = id;
        s.displayName = name;
        s.description = desc;
        s.kind = kind;
        s.unlockLevel = unlock;
        s.iconColor = color;
        s.isUltimate = false;
        s.attack = null;
        s.fx = SkillFxKind.None;
        EditorUtility.SetDirty(s);
        return s;
    }

    internal static AttackData Atk(string id, ElementType element, float startup, float active, float recovery, float comboWindow,
        float dodgeCancel, Vector3 size, Vector3 offset, float knockback, float stagger, float hitStop, float lunge, SwingKind swing)
    {
        var a = LoadOrCreate<AttackData>($"{_dir}/{id}_attack.asset");
        a.id = id;
        a.displayName = id;
        a.isHeavy = true;
        a.element = element;
        a.startup = startup;
        a.active = active;
        a.recovery = recovery;
        a.comboWindow = comboWindow;
        a.dodgeCancel = dodgeCancel;
        a.hitboxSize = size;
        a.hitboxOffset = offset;
        a.knockback = knockback;
        a.stagger = stagger;
        a.hitStop = hitStop;
        a.lungeSpeed = lunge;
        a.swing = swing;
        a.damageMultiplier = 1f; // skills use SkillData.MultiplierAt instead
        a.nextLight = a.nextHeavy = null;
        a.ranged = false;
        EditorUtility.SetDirty(a);
        return a;
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
