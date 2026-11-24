// Writes the 21 jobs (port of the Godot job tree) with 10 skills each into Resources/JobRegistry.
// Job skills are generated from a per-job theme (element, weapon style, looks) so every job plays differently.
using System.Collections.Generic;
using System.IO;
using Aethvirse.Combat;
using Aethvirse.Jobs;
using Aethvirse.Skills;
using Aethvirse.Stats;
using UnityEditor;
using UnityEngine;

public static class JobDataBuilder
{
    const string Dir = "Assets/Data/Jobs";

    struct J
    {
        public string id, name, cls, parent, desc, flag;
        public int tier;
        public bool hidden, shadows;
        public ElementType element;
        public Color color;
        public StatBlock bonus;
        public float atk, def, hp, mp;
        public bool ranged;
        public string[] words;
    }

    static readonly J[] Jobs =
    {
        new J { id = "guardian", name = "Guardian", cls = "vanguard", tier = 1, desc = "A wall that does not fall.", element = ElementType.Earth, color = new Color(0.7f, 0.75f, 0.85f), bonus = new StatBlock(2, 0, 0, 6, 1), def = 0.15f, hp = 0.15f, words = new[] { "Bulwark", "Aegis", "Rampart", "Stone", "Oath" } },
        new J { id = "bastion", name = "Bastion", cls = "vanguard", parent = "guardian", tier = 2, desc = "The fortress walks.", element = ElementType.Earth, color = new Color(0.8f, 0.8f, 0.6f), bonus = new StatBlock(4, 0, 0, 12, 2), def = 0.3f, hp = 0.3f, words = new[] { "Citadel", "Bastion", "Iron", "Titan", "Keep" } },
        new J { id = "berserker", name = "Berserker", cls = "vanguard", tier = 1, desc = "Pain is fuel.", element = ElementType.Fire, color = new Color(1f, 0.35f, 0.25f), bonus = new StatBlock(6, 2, 0, 1, 0), atk = 0.15f, words = new[] { "Rage", "Frenzy", "Blood", "Fury", "Ruin" } },
        new J { id = "warlord", name = "Warlord", cls = "vanguard", parent = "berserker", tier = 2, desc = "War itself, given a sword.", element = ElementType.Fire, color = new Color(1f, 0.45f, 0.15f), bonus = new StatBlock(12, 3, 0, 3, 1), atk = 0.3f, words = new[] { "Warlord", "Conquest", "Havoc", "Siege", "Crimson" } },
        new J { id = "shadowblade", name = "Shadowblade", cls = "phantom", tier = 1, desc = "A blade that strikes from the dark.", element = ElementType.Shadow, color = new Color(0.5f, 0.3f, 0.8f), bonus = new StatBlock(1, 5, 0, 0, 3), atk = 0.12f, words = new[] { "Shade", "Umbral", "Night", "Veil", "Hush" } },
        new J { id = "nightreaper", name = "Nightreaper", cls = "phantom", parent = "shadowblade", tier = 2, desc = "The night reaps.", element = ElementType.Shadow, color = new Color(0.4f, 0.15f, 0.6f), bonus = new StatBlock(2, 10, 0, 1, 6), atk = 0.25f, words = new[] { "Reaper", "Eclipse", "Dusk", "Grave", "Moonless" } },
        new J { id = "trickster", name = "Trickster", cls = "phantom", tier = 1, desc = "Now you see me.", element = ElementType.Wind, color = new Color(0.5f, 0.9f, 0.6f), bonus = new StatBlock(0, 6, 1, 0, 2), atk = 0.08f, words = new[] { "Trick", "Mirage", "Gale", "Jest", "Feint" } },
        new J { id = "illusionist", name = "Illusionist", cls = "phantom", parent = "trickster", tier = 2, desc = "Nothing you see is real.", element = ElementType.Light, color = new Color(0.9f, 0.8f, 1f), bonus = new StatBlock(0, 10, 4, 0, 5), atk = 0.18f, words = new[] { "Phantasm", "Prism", "Dream", "Mirror", "Glamour" } },
        new J { id = "elementalist", name = "Elementalist", cls = "arcanist", tier = 1, desc = "Fire, ice and storm obey.", element = ElementType.Lightning, color = new Color(1f, 0.85f, 0.3f), bonus = new StatBlock(0, 0, 7, 1, 1), atk = 0.15f, ranged = true, words = new[] { "Storm", "Ember", "Rime", "Tempest", "Spark" } },
        new J { id = "archmage", name = "Archmage", cls = "arcanist", parent = "elementalist", tier = 2, desc = "The elements bow.", element = ElementType.Fire, color = new Color(1f, 0.5f, 0.9f), bonus = new StatBlock(0, 0, 14, 2, 2), atk = 0.3f, ranged = true, words = new[] { "Arch", "Inferno", "Absolute", "Cataclysm", "Astral" } },
        new J { id = "sage", name = "Sage", cls = "arcanist", tier = 1, desc = "Knowledge that heals and wards.", element = ElementType.Light, color = new Color(0.6f, 1f, 0.8f), bonus = new StatBlock(0, 0, 5, 3, 2), def = 0.1f, ranged = true, words = new[] { "Sage", "Lumen", "Ward", "Grace", "Insight" } },
        new J { id = "oracle", name = "Oracle", cls = "arcanist", parent = "sage", tier = 2, desc = "Sees what comes before it comes.", element = ElementType.Light, color = new Color(1f, 0.95f, 0.6f), bonus = new StatBlock(0, 0, 10, 4, 6), def = 0.2f, ranged = true, words = new[] { "Oracle", "Fate", "Augury", "Dawn", "Vision" } },
        new J { id = "sharpshooter", name = "Sharpshooter", cls = "ranger", tier = 1, desc = "One arrow, one end.", element = ElementType.Physical, color = new Color(1f, 0.85f, 0.5f), bonus = new StatBlock(1, 4, 0, 0, 4), atk = 0.15f, ranged = true, words = new[] { "Snipe", "Hawk", "Precise", "True", "Pierce" } },
        new J { id = "deadeye", name = "Deadeye", cls = "ranger", parent = "sharpshooter", tier = 2, desc = "It never misses.", element = ElementType.Lightning, color = new Color(1f, 0.6f, 0.3f), bonus = new StatBlock(2, 8, 0, 1, 8), atk = 0.3f, ranged = true, words = new[] { "Deadeye", "Zenith", "Final", "Comet", "Bullseye" } },
        new J { id = "trapper", name = "Trapper", cls = "ranger", tier = 1, desc = "The ground itself hunts.", element = ElementType.Earth, color = new Color(0.6f, 0.5f, 0.3f), bonus = new StatBlock(2, 3, 0, 2, 3), atk = 0.08f, def = 0.08f, ranged = true, words = new[] { "Snare", "Barb", "Pitfall", "Thorn", "Wire" } },
        new J { id = "beastwarden", name = "Beastwarden", cls = "ranger", parent = "trapper", tier = 2, desc = "The wild fights beside you.", element = ElementType.Wind, color = new Color(0.5f, 0.8f, 0.4f), bonus = new StatBlock(3, 6, 0, 4, 4), atk = 0.15f, def = 0.15f, ranged = true, words = new[] { "Beast", "Pack", "Wild", "Fang", "Howl" } },
        new J { id = "summoner", name = "Summoner", cls = "", tier = 1, desc = "Calls familiars to fight beside you (any class).", element = ElementType.Light, color = new Color(0.6f, 0.8f, 1f), bonus = new StatBlock(0, 0, 4, 2, 2), mp = 1.25f, words = new[] { "Familiar", "Bond", "Pact", "Call", "Spirit" } },
        new J { id = "conjurer", name = "Conjurer", cls = "", parent = "summoner", tier = 2, desc = "A host answers.", element = ElementType.Light, color = new Color(0.7f, 0.9f, 1f), bonus = new StatBlock(0, 0, 8, 4, 4), mp = 1.3f, words = new[] { "Host", "Legion", "Covenant", "Choir", "Summons" } },
        new J { id = "necromancer", name = "Necromancer", cls = "", parent = "summoner", tier = 2, hidden = true, flag = "sealed_necromancer", shadows = true, desc = "The fallen rise for you.", element = ElementType.Shadow, color = new Color(0.4f, 0.2f, 0.5f), bonus = new StatBlock(0, 2, 8, 4, 4), mp = 1.2f, words = new[] { "Grave", "Bone", "Echo", "Wraith", "Requiem" } },
        new J { id = "shadow_sovereign", name = "Shadow Sovereign", cls = "", parent = "necromancer", tier = 3, hidden = true, flag = "codex_umbra_read", shadows = true, desc = "Every shadow is yours to command.", element = ElementType.Void, color = new Color(0.15f, 0.1f, 0.25f), bonus = new StatBlock(6, 6, 12, 6, 8), atk = 0.3f, hp = 0.2f, words = new[] { "Sovereign", "Abyss", "Dominion", "Eternal", "Throne" } },
        new J { id = "vanguard_of_echoes", name = "Echo Warden", cls = "", parent = "", tier = 1, hidden = true, flag = "echo_king_answered", desc = "A hidden path. Echoes fight with you.", element = ElementType.Void, color = new Color(0.8f, 0.8f, 0.9f), bonus = new StatBlock(3, 3, 3, 3, 3), atk = 0.1f, def = 0.1f, words = new[] { "Echo", "Hollow", "Resonant", "Unwritten", "Ripple" } },
    };

    static readonly SkillFxKind[] MeleeFx = { SkillFxKind.Cleave, SkillFxKind.Nova, SkillFxKind.Pillar, SkillFxKind.Spin, SkillFxKind.GroundSplit, SkillFxKind.Thunder, SkillFxKind.Charge };
    static readonly SkillFxKind[] RangedFx = { SkillFxKind.Bolt, SkillFxKind.Volley, SkillFxKind.Smite, SkillFxKind.Storm, SkillFxKind.Frost, SkillFxKind.Nova, SkillFxKind.Blink };

    [MenuItem("AETHVIRSE/Build Job Data")]
    public static void Build()
    {
        Directory.CreateDirectory(Dir);
        Directory.CreateDirectory("Assets/Resources");
        var reg = LoadOrCreate<JobRegistry>("Assets/Resources/JobRegistry.asset");
        reg.jobs = new List<JobData>();
        foreach (J j in Jobs) reg.jobs.Add(Job(j));
        EditorUtility.SetDirty(reg);
        AssetDatabase.SaveAssets();
        Debug.Log($"AETHVIRSE: job data built ({reg.jobs.Count} jobs)");
    }

    static JobData Job(J d)
    {
        var job = LoadOrCreate<JobData>($"{Dir}/{d.id}.asset");
        job.id = d.id; job.displayName = d.name; job.description = d.desc; job.baseClass = d.cls ?? ""; job.tier = d.tier;
        job.requiredLevel = d.tier == 1 ? 30 : d.tier == 2 ? 60 : 90; job.parent = d.parent ?? ""; job.hidden = d.hidden;
        job.requiredFlag = d.flag ?? ""; job.statBonus = d.bonus; job.attackBonus = d.atk; job.defenseBonus = d.def; job.hpBonus = d.hp;
        job.mpCostMultiplier = d.mp > 0f ? d.mp : 1f; job.shadowArmy = d.shadows;
        job.skills = Skills(d, job.requiredLevel);
        EditorUtility.SetDirty(job);
        return job;
    }

    /// <summary>10 skills: 7 actives (distinct looks; summoner / shadow lines get their own kinds) and 3 passives.</summary>
    static List<SkillData> Skills(J d, int baseLevel)
    {
        string dir = $"{Dir}/{d.id}";
        Directory.CreateDirectory(dir);
        var list = new List<SkillData>();
        SkillFxKind[] fx = d.ranged ? RangedFx : MeleeFx;
        string[] nouns = d.ranged ? new[] { "Bolt", "Volley", "Lance", "Barrage", "Nova", "Burst", "Rift" } : new[] { "Cleave", "Wave", "Smite", "Whirl", "Quake", "Crash", "Rush" };
        float power = 1.8f + d.tier * 0.8f;
        for (int i = 0; i < 7; i++)
        {
            string id = $"{d.id}_{i}";
            string name = $"{d.words[i % d.words.Length]} {nouns[i]}";
            var s = LoadOrCreate<SkillData>($"{dir}/{id}.asset");
            s.id = id; s.displayName = name; s.description = $"{d.name} technique."; s.iconColor = Color.Lerp(d.color, Color.white, i * 0.06f);
            s.unlockLevel = baseLevel + i * 3; s.maxLevel = 10; s.isUltimate = false; s.fx = fx[i];
            s.mpCost = 20 + d.tier * 8 + i * 2; s.cooldown = 6f + i * 1.5f; s.damageMultiplier = power + i * 0.25f; s.multiplierPerLevel = 0.15f + d.tier * 0.05f;
            s.kind = d.ranged ? (i < 3 ? SkillKind.Projectile : SkillKind.Aoe) : (i == 6 ? SkillKind.Dash : i < 2 ? SkillKind.Melee : SkillKind.Aoe);
            s.attack = SkillAttack(dir, id, d, i, s.kind);
            // Lines with companions swap two actives for their own kinds.
            if (d.id == "summoner" || d.id == "conjurer") { if (i == 5) { s.kind = SkillKind.Summon; s.summonCount = d.tier; s.fx = SkillFxKind.Barrier; s.attack = null; } }
            if (d.shadows)
            {
                if (i == 4) { s.kind = SkillKind.ShadowExtract; s.displayName = "Rise, Echo"; s.fx = SkillFxKind.Shadow; s.attack = null; s.cooldown = 3f; }
                if (i == 5) { s.kind = SkillKind.ShadowCall; s.displayName = "Answer Me"; s.summonCount = 2 + d.tier; s.fx = SkillFxKind.Roar; s.attack = null; s.cooldown = 20f; }
            }
            EditorUtility.SetDirty(s);
            list.Add(s);
        }
        BuffStat[] passives = { BuffStat.Attack, BuffStat.Defense, d.ranged ? BuffStat.Crit : BuffStat.AttackSpeed };
        for (int i = 0; i < 3; i++)
        {
            string id = $"{d.id}_p{i}";
            var s = LoadOrCreate<SkillData>($"{dir}/{id}.asset");
            s.id = id; s.displayName = $"{d.words[(i + 2) % d.words.Length]} {(i == 0 ? "Might" : i == 1 ? "Resolve" : "Rhythm")}";
            s.description = $"Passive. {passives[i]} up."; s.kind = SkillKind.Passive; s.fx = SkillFxKind.None; s.attack = null;
            s.unlockLevel = baseLevel + 2 + i * 5; s.buffStat = passives[i]; s.buffAmount = 0.04f + 0.02f * d.tier; s.buffAmountPerLevel = 0.01f;
            s.iconColor = d.color; s.mpCost = 0; s.isUltimate = false;
            EditorUtility.SetDirty(s);
            list.Add(s);
        }
        return list;
    }

    static AttackData SkillAttack(string dir, string id, J d, int i, SkillKind kind)
    {
        var a = LoadOrCreate<AttackData>($"{dir}/{id}_attack.asset");
        a.id = id; a.displayName = id; a.isHeavy = true; a.element = d.element; a.damageMultiplier = 1f;
        a.startup = 0.12f + i * 0.03f; a.active = 0.15f + (kind == SkillKind.Aoe ? 0.1f : 0f); a.recovery = 0.35f;
        a.comboWindow = a.startup + a.active + 0.2f; a.dodgeCancel = a.startup + a.active * 0.5f;
        bool wide = kind == SkillKind.Aoe;
        a.hitboxSize = wide ? new Vector3(6f + d.tier, 2.2f, 6f + d.tier) : new Vector3(3.2f, 1.6f, 2.8f);
        a.hitboxOffset = wide ? new Vector3(0f, 1f, i % 2 == 0 ? 0f : 2.5f) : new Vector3(0f, 1f, 1.5f);
        a.knockback = 4f + i; a.stagger = 30f + i * 5f; a.hitStop = 0.08f; a.lungeSpeed = kind == SkillKind.Dash ? 22f : 3f;
        a.swing = (SwingKind)(i % 9); a.nextLight = null; a.nextHeavy = null;
        a.ranged = kind == SkillKind.Projectile;
        a.projectileCount = i == 1 ? 5 : i == 2 ? 3 : 1; a.projectileSpread = i == 1 ? 40f : 15f; a.projectileSpeed = 36f; a.projectileRange = 32f;
        a.projectileRadius = 0.4f; a.projectilePierce = i == 2 ? 4 : 1; a.projectileColor = d.color; a.homing = i == 0;
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
