// Writes the combat data assets (port of the Godot data/combat/*.tres). Numbers are the Godot ones;
// hitbox offsets are flipped to Unity's +Z forward. Assets are updated in place, so references keep working.
using System.IO;
using Aethvirse.Combat;
using Aethvirse.Enemies;
using UnityEditor;
using UnityEngine;

public static class CombatDataBuilder
{
    const string GreatswordDir = "Assets/Data/Combat/Greatsword";
    const string EnemyDir = "Assets/Data/Enemies";

    [MenuItem("AETHVIRSE/Build Combat Data")]
    public static void Build()
    {
        Directory.CreateDirectory(GreatswordDir);
        BuildGreatsword();
        BuildDaggers();
        BuildStaff();
        BuildBow();
        Aerial(GreatswordDir, $"{GreatswordDir}/GreatswordCombo.asset", "gs", false, Color.white);
        Aerial("Assets/Data/Combat/Daggers", "Assets/Data/Combat/Daggers/DaggersCombo.asset", "dg", false, Color.white);
        Aerial("Assets/Data/Combat/Staff", "Assets/Data/Combat/Staff/StaffCombo.asset", "st", true, new Color(0.6f, 0.75f, 1f));
        Aerial("Assets/Data/Combat/Bow", "Assets/Data/Combat/Bow/BowCombo.asset", "bw", true, new Color(0.95f, 0.9f, 0.75f));
        Directory.CreateDirectory(EnemyDir);
        BuildAshHound();
        AssetDatabase.SaveAssets();
        Debug.Log("AETHVIRSE: combat data built");
    }

    static void BuildGreatsword()
    {
        // id, name, mult, heavy, startup, active, recovery, comboWindow, dodgeCancel, size, offset(+Z fwd), knockback, stagger, hitStop, lunge, swing
        AttackData l1 = Attack("light_1", "Slash I", 0.9f, false, 0.08f, 0.10f, 0.22f, 0.16f, 0.12f, new Vector3(2.4f, 1.4f, 1.9f), new Vector3(0, 1f, 1.1f), 1.5f, 8f, 0.04f, 3.5f, SwingKind.Horizontal);
        AttackData l2 = Attack("light_2", "Slash II", 1.0f, false, 0.08f, 0.10f, 0.22f, 0.16f, 0.12f, new Vector3(2.4f, 1.4f, 1.9f), new Vector3(0, 1f, 1.1f), 1.5f, 8f, 0.04f, 3.5f, SwingKind.Backhand);
        AttackData l3 = Attack("light_3", "Slash III", 1.1f, false, 0.10f, 0.10f, 0.25f, 0.18f, 0.14f, new Vector3(2.6f, 1.4f, 2.0f), new Vector3(0, 1f, 1.2f), 2f, 10f, 0.05f, 4f, SwingKind.Vertical);
        AttackData l4 = Attack("light_4", "Slash IV", 1.2f, false, 0.10f, 0.12f, 0.25f, 0.20f, 0.15f, new Vector3(2.6f, 1.4f, 2.0f), new Vector3(0, 1f, 1.2f), 2f, 10f, 0.05f, 4f, SwingKind.Diagonal);
        AttackData l5 = Attack("light_5", "Cyclone Finisher", 1.8f, false, 0.16f, 0.18f, 0.40f, 0.40f, 0.25f, new Vector3(4.2f, 1.6f, 4.2f), new Vector3(0, 1f, 0f), 6f, 30f, 0.08f, 2f, SwingKind.Spin);

        AttackData overhead = Attack("overhead_smash", "Overhead Smash", 2.0f, true, 0.30f, 0.12f, 0.45f, 0.40f, 0.32f, new Vector3(2.0f, 2.0f, 2.4f), new Vector3(0, 1f, 1.4f), 4.5f, 35f, 0.09f, 2.5f, SwingKind.Vertical);
        AttackData rising = Attack("rising_cleave", "Rising Cleave", 1.6f, true, 0.20f, 0.12f, 0.40f, 0.35f, 0.25f, new Vector3(2.2f, 2.2f, 2.2f), new Vector3(0, 1.2f, 1.2f), 3.5f, 25f, 0.07f, 3f, SwingKind.Rising);
        AttackData whirl = Attack("whirlwind", "Whirlwind", 1.4f, true, 0.18f, 0.30f, 0.40f, 0.45f, 0.30f, new Vector3(4.6f, 1.6f, 4.6f), new Vector3(0, 1f, 0f), 4f, 20f, 0.06f, 1f, SwingKind.DoubleSpin);
        AttackData slam = Attack("ground_slam", "Ground Slam", 2.4f, true, 0.32f, 0.14f, 0.50f, 0.50f, 0.38f, new Vector3(3.6f, 1.8f, 3.6f), new Vector3(0, 0.6f, 1.0f), 6f, 45f, 0.11f, 2f, SwingKind.Slam);
        AttackData thrust = Attack("piercing_thrust", "Piercing Thrust", 2.0f, true, 0.22f, 0.12f, 0.40f, 0.38f, 0.28f, new Vector3(1.2f, 1.2f, 4.0f), new Vector3(0, 1f, 2.2f), 5f, 30f, 0.08f, 6f, SwingKind.Thrust);

        // Combo graph: L L L L L chain; H after the n-th light gives a different finisher.
        Link(l1, l2, rising);
        Link(l2, l3, whirl);
        Link(l3, l4, slam);
        Link(l4, l5, thrust);
        Link(l5, null, null);
        foreach (AttackData heavy in new[] { overhead, rising, whirl, slam, thrust }) Link(heavy, null, null);

        var combo = LoadOrCreate<ComboData>($"{GreatswordDir}/GreatswordCombo.asset");
        combo.lightStart = l1;
        combo.heavyStart = overhead;
        combo.chainGrace = 0.35f;
        EditorUtility.SetDirty(combo);
    }

    // Shorthand for the other weapons: (id, name, mult, heavy, startup, active, recovery, swing).
    static AttackData A(string dir, string id, string name, float mult, bool heavy, float startup, float active, float recovery,
        SwingKind swing, Vector3 size, Vector3 offset, float knockback, float stagger, float hitStop, float lunge)
    {
        Directory.CreateDirectory(dir);
        var a = LoadOrCreate<AttackData>($"{dir}/{id}.asset");
        a.id = id; a.displayName = name; a.damageMultiplier = mult; a.isHeavy = heavy; a.element = ElementType.Physical;
        a.startup = startup; a.active = active; a.recovery = recovery;
        a.comboWindow = Mathf.Min(startup + active + recovery * 0.4f, startup + active + recovery);
        a.dodgeCancel = startup + active * 0.5f;
        a.hitboxSize = size; a.hitboxOffset = offset; a.knockback = knockback; a.stagger = stagger; a.hitStop = hitStop;
        a.lungeSpeed = lunge; a.swing = swing; a.ranged = false; a.homing = false; a.projectileCount = 1; a.projectilePierce = 0;
        a.launch = 0f; a.hover = false; a.slam = false;
        EditorUtility.SetDirty(a);
        return a;
    }

    static AttackData Ranged(AttackData a, int count, float spread, float speed, float range, float radius, int pierce, Color color, bool homing)
    {
        a.ranged = true; a.projectileCount = count; a.projectileSpread = spread; a.projectileSpeed = speed; a.projectileRange = range;
        a.projectileRadius = radius; a.projectilePierce = pierce; a.projectileColor = color; a.homing = homing;
        EditorUtility.SetDirty(a);
        return a;
    }

    static void Chain(string comboPath, AttackData[] lights, AttackData[] heavies, AttackData heavyStart)
    {
        for (int i = 0; i < lights.Length; i++) Link(lights[i], i + 1 < lights.Length ? lights[i + 1] : null, i < heavies.Length ? heavies[i] : null);
        foreach (AttackData h in heavies) Link(h, null, null);
        Link(heavyStart, null, null);
        var combo = LoadOrCreate<ComboData>(comboPath);
        combo.lightStart = lights[0];
        combo.heavyStart = heavyStart;
        combo.chainGrace = 0.35f;
        EditorUtility.SetDirty(combo);
    }

    /// <summary>Twin daggers (Assassin): six quick cuts, low stagger, short reach; heavies are flips and lunges.</summary>
    static void BuildDaggers()
    {
        const string d = "Assets/Data/Combat/Daggers";
        Vector3 small = new Vector3(1.8f, 1.3f, 1.6f), near = new Vector3(0f, 1f, 1f);
        var lights = new[]
        {
            A(d, "dagger_1", "Cut I", 0.55f, false, 0.05f, 0.07f, 0.14f, SwingKind.Horizontal, small, near, 1f, 5f, 0.03f, 3f),
            A(d, "dagger_2", "Cut II", 0.55f, false, 0.05f, 0.07f, 0.14f, SwingKind.Backhand, small, near, 1f, 5f, 0.03f, 3f),
            A(d, "dagger_3", "Cut III", 0.6f, false, 0.05f, 0.08f, 0.15f, SwingKind.Thrust, small, near, 1f, 6f, 0.03f, 4f),
            A(d, "dagger_4", "Cut IV", 0.6f, false, 0.05f, 0.08f, 0.15f, SwingKind.Diagonal, small, near, 1f, 6f, 0.03f, 3f),
            A(d, "dagger_5", "Cut V", 0.65f, false, 0.06f, 0.08f, 0.16f, SwingKind.Rising, small, near, 1.5f, 7f, 0.04f, 3f),
            A(d, "dagger_6", "Whirling Fangs", 1.3f, false, 0.08f, 0.2f, 0.3f, SwingKind.DoubleSpin, new Vector3(3.6f, 1.4f, 3.6f), new Vector3(0, 1f, 0f), 4f, 18f, 0.06f, 2f),
        };
        var heavies = new[]
        {
            A(d, "dagger_h1", "Backflip Slash", 1.4f, true, 0.12f, 0.1f, 0.3f, SwingKind.Rising, small, near, 3f, 18f, 0.06f, -4f),
            A(d, "dagger_h2", "Viper Lunge", 1.6f, true, 0.1f, 0.12f, 0.3f, SwingKind.Thrust, new Vector3(1.2f, 1.2f, 3.4f), new Vector3(0, 1f, 1.8f), 3f, 20f, 0.06f, 12f),
            A(d, "dagger_h3", "Cross Rend", 1.7f, true, 0.12f, 0.12f, 0.32f, SwingKind.Diagonal, new Vector3(2.4f, 1.4f, 2f), new Vector3(0, 1f, 1.1f), 3.5f, 22f, 0.07f, 4f),
            A(d, "dagger_h4", "Shadow Fall", 2f, true, 0.18f, 0.12f, 0.36f, SwingKind.Slam, new Vector3(2.8f, 1.6f, 2.8f), new Vector3(0, 0.6f, 0.8f), 4f, 28f, 0.08f, 3f),
            A(d, "dagger_h5", "Fang Tempest", 2.2f, true, 0.16f, 0.26f, 0.4f, SwingKind.Spin, new Vector3(4.2f, 1.6f, 4.2f), new Vector3(0, 1f, 0f), 5f, 30f, 0.09f, 1f),
        };
        AttackData start = A(d, "dagger_heavy", "Twin Pierce", 1.3f, true, 0.1f, 0.1f, 0.28f, SwingKind.Thrust, new Vector3(1.4f, 1.3f, 2.6f), new Vector3(0, 1f, 1.4f), 3f, 16f, 0.06f, 8f);
        Chain($"{d}/DaggersCombo.asset", lights, heavies, start);
    }

    /// <summary>Focus staff (Mage): homing bolts; heavies are piercing orbs, fans and a close blast.</summary>
    static void BuildStaff()
    {
        const string d = "Assets/Data/Combat/Staff";
        Color arcane = new Color(0.6f, 0.75f, 1f);
        Vector3 box = new Vector3(1.6f, 1.4f, 1.6f), front = new Vector3(0f, 1f, 1f);
        var lights = new[]
        {
            Ranged(A(d, "staff_1", "Spark Bolt", 0.7f, false, 0.1f, 0.05f, 0.2f, SwingKind.Thrust, box, front, 1f, 6f, 0.03f, 0f), 1, 0f, 30f, 24f, 0.3f, 0, arcane, true),
            Ranged(A(d, "staff_2", "Spark Bolt", 0.7f, false, 0.1f, 0.05f, 0.2f, SwingKind.Thrust, box, front, 1f, 6f, 0.03f, 0f), 1, 0f, 30f, 24f, 0.3f, 0, arcane, true),
            Ranged(A(d, "staff_3", "Twin Bolt", 0.55f, false, 0.12f, 0.05f, 0.22f, SwingKind.Vertical, box, front, 1f, 6f, 0.03f, 0f), 2, 8f, 30f, 24f, 0.3f, 0, arcane, true),
            Ranged(A(d, "staff_4", "Arc Burst", 1.4f, false, 0.18f, 0.08f, 0.35f, SwingKind.Spin, box, front, 3f, 16f, 0.05f, 0f), 5, 40f, 24f, 16f, 0.35f, 0, arcane, false),
        };
        var heavies = new[]
        {
            Ranged(A(d, "staff_h1", "Lance of Light", 1.6f, true, 0.25f, 0.05f, 0.35f, SwingKind.Thrust, box, front, 3f, 18f, 0.06f, 0f), 1, 0f, 40f, 32f, 0.45f, 4, new Color(1f, 0.95f, 0.6f), false),
            Ranged(A(d, "staff_h2", "Fan of Sparks", 1.1f, true, 0.22f, 0.05f, 0.35f, SwingKind.Horizontal, box, front, 2f, 12f, 0.05f, 0f), 7, 50f, 26f, 18f, 0.3f, 0, arcane, false),
            A(d, "staff_h3", "Repulse", 1.8f, true, 0.2f, 0.12f, 0.4f, SwingKind.Spin, new Vector3(5f, 2f, 5f), new Vector3(0, 1f, 0f), 9f, 30f, 0.08f, 0f),
            Ranged(A(d, "staff_h4", "Great Orb", 2.6f, true, 0.35f, 0.05f, 0.45f, SwingKind.Vertical, box, front, 5f, 35f, 0.1f, 0f), 1, 0f, 16f, 26f, 0.9f, 8, new Color(0.7f, 0.5f, 1f), true),
        };
        AttackData start = Ranged(A(d, "staff_heavy", "Arcane Orb", 1.5f, true, 0.25f, 0.05f, 0.38f, SwingKind.Vertical, box, front, 3f, 20f, 0.06f, 0f), 1, 0f, 22f, 26f, 0.6f, 2, new Color(0.7f, 0.5f, 1f), true);
        Chain($"{d}/StaffCombo.asset", lights, heavies, start);
    }

    /// <summary>Hunting bow (Marksman): fast arrows that curve to the lock-on; heavies pierce and spread.</summary>
    static void BuildBow()
    {
        const string d = "Assets/Data/Combat/Bow";
        Color arrow = new Color(0.95f, 0.9f, 0.75f);
        Vector3 box = new Vector3(1.6f, 1.4f, 1.6f), front = new Vector3(0f, 1f, 1f);
        var lights = new[]
        {
            Ranged(A(d, "bow_1", "Quick Shot", 0.75f, false, 0.08f, 0.04f, 0.18f, SwingKind.Thrust, box, front, 1f, 6f, 0.03f, 0f), 1, 0f, 42f, 34f, 0.22f, 0, arrow, true),
            Ranged(A(d, "bow_2", "Quick Shot", 0.75f, false, 0.08f, 0.04f, 0.18f, SwingKind.Thrust, box, front, 1f, 6f, 0.03f, 0f), 1, 0f, 42f, 34f, 0.22f, 0, arrow, true),
            Ranged(A(d, "bow_3", "Double Shot", 0.6f, false, 0.1f, 0.04f, 0.2f, SwingKind.Thrust, box, front, 1f, 6f, 0.03f, 0f), 2, 5f, 42f, 34f, 0.22f, 0, arrow, true),
            Ranged(A(d, "bow_4", "Rain Volley", 0.6f, false, 0.16f, 0.06f, 0.3f, SwingKind.Rising, box, front, 1f, 8f, 0.04f, 0f), 5, 30f, 36f, 26f, 0.25f, 0, arrow, false),
        };
        var heavies = new[]
        {
            Ranged(A(d, "bow_h1", "Piercing Arrow", 1.8f, true, 0.3f, 0.04f, 0.3f, SwingKind.Thrust, box, front, 4f, 22f, 0.06f, 0f), 1, 0f, 60f, 44f, 0.3f, 5, new Color(1f, 0.8f, 0.4f), false),
            Ranged(A(d, "bow_h2", "Scatter Shot", 0.9f, true, 0.22f, 0.04f, 0.32f, SwingKind.Horizontal, box, front, 3f, 12f, 0.05f, -5f), 6, 45f, 36f, 18f, 0.25f, 0, arrow, false),
            A(d, "bow_h3", "Kick Away", 1.3f, true, 0.12f, 0.1f, 0.3f, SwingKind.Thrust, new Vector3(1.8f, 1.4f, 1.8f), new Vector3(0, 1f, 1f), 8f, 25f, 0.06f, -8f),
            Ranged(A(d, "bow_h4", "Heartseeker", 3f, true, 0.45f, 0.04f, 0.4f, SwingKind.Thrust, box, front, 5f, 40f, 0.1f, 0f), 1, 0f, 70f, 50f, 0.35f, 2, new Color(1f, 0.4f, 0.4f), true),
        };
        AttackData start = Ranged(A(d, "bow_heavy", "Charged Arrow", 1.5f, true, 0.28f, 0.04f, 0.3f, SwingKind.Thrust, box, front, 4f, 20f, 0.06f, 0f), 1, 0f, 55f, 40f, 0.3f, 2, new Color(1f, 0.85f, 0.5f), true);
        Chain($"{d}/BowCombo.asset", lights, heavies, start);
    }

    /// <summary>Ash Hound (Godot data/enemies/ash_hound.tres): fast, short windup, hits that shove you.</summary>
    static void BuildAshHound()
    {
        var e = LoadOrCreate<EnemyData>($"{EnemyDir}/AshHound.asset");
        e.id = "ash_hound";
        e.displayName = "Ash Hound";
        e.level = 2;
        e.scaleByLevel = true;
        e.tier = Aethvirse.Stats.EnemyTier.Normal;
        // Godot: a bit fragile, bites harder.
        e.hpScale = 0.8f;
        e.attackScale = 1.15f;
        // Flat numbers until level scaling arrives with stats (M3): ~8 light hits to kill, ~25 bites to kill you.
        e.maxHealth = 140;
        e.stats = new CombatStats { attack = 16f, defense = 4f, critChance = 0.05f, critDamage = 1.5f };
        e.poise = 25f;
        e.bodyColor = new Color(0.45f, 0.40f, 0.38f);
        e.walkSpeed = 1.6f;
        e.chaseSpeed = 5f;
        e.aggroRadius = 12f;
        e.leashRadius = 26f;
        e.patrolRadius = 4f;
        e.attackRange = 2f;
        e.attackWindup = 0.55f;
        e.attackActive = 0.15f;
        e.attackRecovery = 0.6f;
        e.attackCooldown = 0.9f;
        e.attackMultiplier = 1f;
        e.attackStagger = 20f;
        e.attackKnockback = 6f;
        e.attackLaunch = 0f;
        e.attackLungeSpeed = 5f;
        e.attackHitboxSize = new Vector3(1.8f, 1.4f, 1.6f);
        e.attackHitboxOffset = new Vector3(0f, 1f, 1f);
        e.telegraphRadius = 1.6f;
        e.respawnDelay = 6f;
        EditorUtility.SetDirty(e);
    }

    static AttackData Attack(string id, string name, float mult, bool heavy, float startup, float active, float recovery,
        float comboWindow, float dodgeCancel, Vector3 size, Vector3 offset, float knockback, float stagger, float hitStop,
        float lunge, SwingKind swing)
    {
        var a = LoadOrCreate<AttackData>($"{GreatswordDir}/{id}.asset");
        a.id = id;
        a.displayName = name;
        a.damageMultiplier = mult;
        a.isHeavy = heavy;
        a.element = ElementType.Physical;
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
        a.launch = 0f; a.hover = false; a.slam = false;
        EditorUtility.SetDirty(a);
        return a;
    }

    /// <summary>
    /// The aerial kit of a weapon (§6): a launcher (heavy from neutral while locked on), three hanging air strikes
    /// that keep the target up, and a plunge that ends in a shockwave. Ranged weapons shoot in the air instead.
    /// </summary>
    static void Aerial(string dir, string comboPath, string w, bool ranged, Color shot)
    {
        Vector3 box = new Vector3(2.4f, 2f, 2.2f), front = new Vector3(0f, 1f, 1.1f);
        AttackData launcher = A(dir, $"{w}_launcher", "Launcher", 1.3f, true, 0.16f, 0.1f, 0.3f, SwingKind.Rising, new Vector3(2.2f, 2.4f, 2.2f), new Vector3(0, 1.2f, 1.1f), 1f, 25f, 0.08f, 3f);
        launcher.launch = 10f;
        launcher.comboWindow = launcher.startup + launcher.active + 0.05f;
        var air = new AttackData[3];
        string[] names = ranged ? new[] { "Air Shot", "Air Shot", "Air Burst" } : new[] { "Air Cut I", "Air Cut II", "Air Cross" };
        SwingKind[] swings = { SwingKind.Horizontal, SwingKind.Backhand, SwingKind.Diagonal };
        for (int i = 0; i < 3; i++)
        {
            air[i] = A(dir, $"{w}_air_{i + 1}", names[i], 0.9f + 0.15f * i, false, 0.07f, 0.1f, 0.2f, swings[i], box, front, 0.5f, 6f, 0.05f, 1f);
            if (ranged) Ranged(air[i], i == 2 ? 3 : 1, i == 2 ? 20f : 0f, 36f, 22f, 0.3f, 0, shot, true);
            air[i].hover = true;
            air[i].launch = 3.5f;
        }
        for (int i = 0; i < 3; i++) Link(air[i], i < 2 ? air[i + 1] : null, null);
        AttackData plunge = A(dir, $"{w}_plunge", "Plunge", 2.2f, true, 0.18f, 0.12f, 0.35f, SwingKind.Slam, new Vector3(2.4f, 3f, 2.4f), new Vector3(0, 0.5f, 0.6f), 7f, 40f, 0.12f, 0f);
        plunge.slam = true;
        plunge.launch = -14f;
        Link(launcher, null, null);
        Link(plunge, null, null);
        var combo = LoadOrCreate<ComboData>(comboPath);
        combo.launcher = launcher;
        combo.airLightStart = air[0];
        combo.airHeavyStart = plunge;
        EditorUtility.SetDirty(combo);
    }

    static void Link(AttackData a, AttackData nextLight, AttackData nextHeavy)
    {
        a.nextLight = nextLight;
        a.nextHeavy = nextHeavy;
        EditorUtility.SetDirty(a);
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
