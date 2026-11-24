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
    const string SkillDir = "Assets/Data/Skills/Paladin";
    const string ComboPath = "Assets/Data/Combat/Greatsword/GreatswordCombo.asset";

    [MenuItem("AETHVIRSE/Build Class Data")]
    public static void Build()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(TuningPath));
        Directory.CreateDirectory(ClassDir);
        Directory.CreateDirectory(SkillDir);
        LoadOrCreate<StatTuning>(TuningPath); // defaults are the sheet values
        BuildPaladin();
        AssetDatabase.SaveAssets();
        Debug.Log("AETHVIRSE: class data built");
    }

    static void BuildPaladin()
    {
        var skills = new List<SkillData>
        {
            Active("iron_charge", "Iron Charge", "Dash forward, ramming everything in the way.", SkillKind.Dash, 1, 20, 8f, 1.6f, 0.1f,
                new Color(0.75f, 0.8f, 0.9f), SkillFxKind.Charge,
                Atk("iron_charge", ElementType.Physical, 0.08f, 0.35f, 0.25f, 0.68f, 0.43f, new Vector3(1.8f, 1.6f, 2f), new Vector3(0, 1f, 0.8f), 6f, 40f, 0.06f, 22f, SwingKind.Thrust)),
            Active("rending_slash", "Rending Slash", "A heavy forward cleave that tears through guards.", SkillKind.Melee, 1, 15, 5f, 2.2f, 0.15f,
                new Color(0.45f, 0.75f, 1f), SkillFxKind.Cleave,
                Atk("rending_slash", ElementType.Physical, 0.14f, 0.14f, 0.32f, 0.6f, 0.28f, new Vector3(3.2f, 1.6f, 2.6f), new Vector3(0, 1f, 1.5f), 4f, 30f, 0.08f, 5f, SwingKind.Horizontal)),
            Passive("iron_body", "Iron Body", "Passive. Defense up.", 2, BuffStat.Defense, 0.06f, 0.012f),
            Active("earthsplitter", "Earthsplitter", "Slam the ground and split the earth around you.", SkillKind.Aoe, 3, 25, 10f, 2f, 0.12f,
                new Color(0.8f, 0.6f, 0.3f), SkillFxKind.GroundSplit,
                Atk("earthsplitter", ElementType.Earth, 0.35f, 0.15f, 0.45f, 0.95f, 0.5f, new Vector3(8f, 2f, 8f), new Vector3(0, 0.8f, 1f), 7f, 45f, 0.1f, 0f, SwingKind.Vertical)),
            Passive("heavy_hands", "Heavy Hands", "Passive. Attack up.", 4, BuffStat.Attack, 0.04f, 0.008f),
            Buff("war_cry", "War Cry", "A roar that sharpens your strikes. ATK up for a while.", 5, 20, 25f, BuffStat.Attack, 0.2f, 0.02f, 10f,
                new Color(1f, 0.45f, 0.3f)),
            Passive("battle_rhythm", "Battle Rhythm", "Passive. Attack speed up.", 6, BuffStat.AttackSpeed, 0.04f, 0.008f),
            Active("flame_edge", "Flame Edge", "Ignite the blade and carve a burning arc.", SkillKind.Melee, 8, 18, 7f, 1.8f, 0.12f,
                new Color(1f, 0.55f, 0.15f), SkillFxKind.FlameArc,
                Atk("flame_edge", ElementType.Fire, 0.12f, 0.2f, 0.3f, 0.62f, 0.32f, new Vector3(3f, 1.5f, 2.4f), new Vector3(0, 1f, 1.3f), 3f, 25f, 0.07f, 4f, SwingKind.Diagonal)),
            Active("titans_verdict", "Titan's Verdict", "Ultimate. Bring the sky down on everything nearby.", SkillKind.Aoe, 10, 60, 60f, 5f, 0.3f,
                new Color(1f, 0.85f, 0.3f), SkillFxKind.SkyFall,
                Atk("titans_verdict", ElementType.Physical, 0.6f, 0.25f, 0.6f, 1.45f, 0.85f, new Vector3(12f, 3f, 12f), new Vector3(0, 1f, 0f), 10f, 100f, 0.16f, 0f, SwingKind.Slam),
                ultimate: true),
        };

        var c = LoadOrCreate<ClassData>($"{ClassDir}/Paladin.asset");
        c.id = "vanguard";
        c.displayName = "Paladin";
        c.description = "Holy knight. Heavy sword combos, high HP and DEF.";
        c.magicDamage = false;
        c.baseStats = new StatBlock(12f, 6f, 3f, 11f, 4f);
        c.growth = new StatBlock(2f, 0.6f, 0.2f, 1.8f, 0.4f);
        c.recommendedSplit = new StatBlock(0.5f, 0.1f, 0f, 0.35f, 0.05f);
        c.combo = AssetDatabase.LoadAssetAtPath<ComboData>(ComboPath);
        c.skills = skills;
        c.skillCombos = new List<SkillComboData>
        {
            new SkillComboData { id = "charge_breaker", displayName = "Breaker", first = "iron_charge", second = "rending_slash", window = 2f, bonusMultiplier = 1f, forceCrit = true },
            new SkillComboData { id = "fury_quake", displayName = "Fury Quake", first = "war_cry", second = "earthsplitter", window = 4f, bonusMultiplier = 1.5f },
            new SkillComboData { id = "molten_rift", displayName = "Molten Rift", first = "flame_edge", second = "earthsplitter", window = 2.5f, bonusMultiplier = 1.3f, extraStagger = 20f },
        };
        EditorUtility.SetDirty(c);
    }

    static SkillData Active(string id, string name, string desc, SkillKind kind, int unlock, int mp, float cooldown,
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

    static SkillData Buff(string id, string name, string desc, int unlock, int mp, float cooldown, BuffStat stat,
        float amount, float perLevel, float duration, Color color)
    {
        SkillData s = Skill(id, name, desc, SkillKind.Buff, unlock, color);
        s.mpCost = mp;
        s.cooldown = cooldown;
        s.buffStat = stat;
        s.buffAmount = amount;
        s.buffAmountPerLevel = perLevel;
        s.buffDuration = duration;
        s.fx = SkillFxKind.Roar;
        return s;
    }

    static SkillData Passive(string id, string name, string desc, int unlock, BuffStat stat, float amount, float perLevel)
    {
        SkillData s = Skill(id, name, desc, SkillKind.Passive, unlock, new Color(0.9f, 0.4f, 0.3f));
        s.mpCost = 0;
        s.buffStat = stat;
        s.buffAmount = amount;
        s.buffAmountPerLevel = perLevel;
        return s;
    }

    static SkillData Skill(string id, string name, string desc, SkillKind kind, int unlock, Color color)
    {
        var s = LoadOrCreate<SkillData>($"{SkillDir}/{id}.asset");
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

    static AttackData Atk(string id, ElementType element, float startup, float active, float recovery, float comboWindow,
        float dodgeCancel, Vector3 size, Vector3 offset, float knockback, float stagger, float hitStop, float lunge, SwingKind swing)
    {
        var a = LoadOrCreate<AttackData>($"{SkillDir}/{id}_attack.asset");
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
