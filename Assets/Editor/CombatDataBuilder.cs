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
        EditorUtility.SetDirty(a);
        return a;
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
