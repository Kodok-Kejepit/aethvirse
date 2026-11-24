using Aethvirse.Combat;
using Aethvirse.Stats;
using UnityEngine;

namespace Aethvirse.Enemies
{
    /// <summary>Enemy definition (data-driven, port of the Godot EnemyData). Level scaling comes with stats (M3).</summary>
    [CreateAssetMenu(menuName = "AETHVIRSE/Enemy", fileName = "Enemy")]
    public sealed class EnemyData : ScriptableObject
    {
        public string id = "";
        public string displayName = "";
        public int level = 1;
        public int maxHealth = 120;
        public CombatStats stats = new CombatStats();
        [Tooltip("Stagger damage needed to interrupt it (resets after a short time without hits).")]
        public float poise = 25f;
        public Color bodyColor = new Color(0.8f, 0.25f, 0.25f);
        [Tooltip("Body scale (elites ~1.25, bosses ~1.7).")]
        public float size = 1f;

        [Header("Level scaling (Balancing sheet)")]
        [Tooltip("HP / ATK / DEF / XP come from EnemyScaling for level and tier; the numbers above are fallbacks.")]
        public bool scaleByLevel = true;
        public EnemyTier tier = EnemyTier.Normal;
        [Tooltip("Per-type flavour on the scaled HP / ATK (tanky, glass cannon …).")]
        public float hpScale = 1f;
        public float attackScale = 1f;
        public int xpReward = 20;
        public int goldReward = 5;
        public bool dropsLoot = true;

        [Header("Movement & awareness")]
        public float walkSpeed = 1.6f;
        public float chaseSpeed = 4.2f;
        public float aggroRadius = 12f;
        public float leashRadius = 26f;
        public float patrolRadius = 4f;

        [Header("Attack")]
        public float attackRange = 2f;
        public float attackWindup = 0.7f;
        public float attackActive = 0.15f;
        public float attackRecovery = 0.6f;
        public float attackCooldown = 1.2f;
        public float attackMultiplier = 1f;
        public float attackStagger = 20f;
        public float attackKnockback = 4f;
        [Tooltip("Upward speed on the hero (heavy hitters throw it).")]
        public float attackLaunch;
        public float attackLungeSpeed = 5f;
        public Vector3 attackHitboxSize = new Vector3(1.8f, 1.4f, 1.6f);
        [Tooltip("Box centre in the enemy's local space (+z = in front).")]
        public Vector3 attackHitboxOffset = new Vector3(0f, 1f, 1f);
        public float telegraphRadius = 1.6f;

        [Header("Respawn (test arenas)")]
        [Tooltip("Seconds after death until it stands up at home again; 0 = stays dead.")]
        public float respawnDelay = 6f;
    }
}
