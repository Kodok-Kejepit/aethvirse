using Aethvirse.Combat;
using Aethvirse.Stats;
using UnityEngine;

namespace Aethvirse.Skills
{
    /// <summary>MELEE/DASH/AOE use <see cref="SkillData.attack"/> as timeline + hitbox; BUFF is instant; PASSIVE is always on.</summary>
    public enum SkillKind { Melee, Dash, Aoe, Buff, Passive }

    /// <summary>The look of a skill (owner: every skill must move and look different).</summary>
    public enum SkillFxKind { None, Charge, Cleave, FlameArc, GroundSplit, Roar, SkyFall }

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

        public float MultiplierAt(int level) => damageMultiplier + multiplierPerLevel * (Mathf.Max(level, 1) - 1);
        public float BuffAt(int level) => buffAmount + buffAmountPerLevel * (Mathf.Max(level, 1) - 1);
        public bool IsPassive => kind == SkillKind.Passive;
        public bool IsInstant => kind == SkillKind.Buff;
        public bool IsDamaging => (kind == SkillKind.Melee || kind == SkillKind.Dash || kind == SkillKind.Aoe) && attack != null;
    }
}
