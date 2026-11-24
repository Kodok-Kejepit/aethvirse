using Aethvirse.Combat;
using Aethvirse.Stats;
using UnityEngine;

namespace Aethvirse.Skills
{
    /// <summary>MELEE/DASH/AOE use <see cref="SkillData.attack"/> as timeline + hitbox; BUFF is instant; PASSIVE is always on.</summary>
    /// <summary>Append only: assets store the number.</summary>
    public enum SkillKind { Melee, Dash, Aoe, Buff, Passive, Projectile, Heal, Stealth, Trap, Summon, ShadowExtract, ShadowCall }

    /// <summary>The look of a skill (owner: every skill must move and look different).</summary>
    /// <summary>Append only. Generic looks are tinted with the skill's icon colour.</summary>
    public enum SkillFxKind { None, Charge, Cleave, FlameArc, GroundSplit, Roar, SkyFall, Nova, Pillar, Blink, Barrier, Heal, Shadow, Volley, Bolt, TrapMark, Storm, Frost, Thunder, Spin, Smite }

    /// <summary>An active or passive skill (port of the Godot SkillData).</summary>
    [CreateAssetMenu(menuName = "AETHVIRSE/Skill", fileName = "Skill")]
    public sealed class SkillData : ScriptableObject
    {
        public string id = "";
        public string displayName = "";
        [TextArea] public string description = "";
        public SkillKind kind = SkillKind.Melee;
        public bool isUltimate;
        public Color iconColor = new Color(0.4f, 0.7f, 1f);
        public int unlockLevel = 1;
        public int maxLevel = 10;
        public SkillFxKind fx = SkillFxKind.None;

        [Header("Cost")]
        public int mpCost = 10;
        public float cooldown = 6f;

        [Header("Damage")]
        [Tooltip("Timeline + hitbox (+ element) for Melee / Dash / Aoe.")]
        public AttackData attack;
        public float damageMultiplier = 1.5f;
        [Tooltip("Added per skill level above 1.")]
        public float multiplierPerLevel = 0.1f;

        [Header("Buff / passive")]
        public BuffStat buffStat = BuffStat.Attack;
        [Tooltip("Multiplier added while active (0.2 = +20%).")]
        public float buffAmount = 0.2f;
        public float buffDuration = 10f;
        public float buffAmountPerLevel = 0.02f;

        [Header("Heal / stealth / trap")]
        [Tooltip("Heal: share of max HP restored (0.25 = 25%).")]
        public float healShare = 0.25f;
        [Tooltip("Stealth: seconds unseen; the first hit out of it is a guaranteed crit.")]
        public float stealthDuration = 5f;
        [Tooltip("Trap: radius and how long it waits for prey.")]
        public float trapRadius = 2.5f;
        public float trapLifetime = 15f;
        public float MultiplierAt(int level) => damageMultiplier + multiplierPerLevel * (Mathf.Max(level, 1) - 1);
        public float BuffAt(int level) => buffAmount + buffAmountPerLevel * (Mathf.Max(level, 1) - 1);
        public bool IsPassive => kind == SkillKind.Passive;
        public bool IsInstant => kind == SkillKind.Buff || kind == SkillKind.Heal || kind == SkillKind.Stealth || kind == SkillKind.Trap
            || kind == SkillKind.Summon || kind == SkillKind.ShadowExtract || kind == SkillKind.ShadowCall;

        [Header("Summons")]
        [Tooltip("Summon: enemy type used as the familiar's body; ShadowCall: how many soldiers come.")]
        public string summonType = "ash_hound";
        public int summonCount = 1;
        public bool IsDamaging => (kind == SkillKind.Melee || kind == SkillKind.Dash || kind == SkillKind.Aoe || kind == SkillKind.Projectile) && attack != null;
    }
}
