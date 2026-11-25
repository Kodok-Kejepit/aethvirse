using System.Collections.Generic;
using Aethvirse.Combat;
using UnityEngine;

namespace Aethvirse.Skills
{
    /// <summary>What a rune does to the skill it sits in (append only: assets store the number).</summary>
    public enum RuneEffect
    {
        Overcharge, Thrift, Haste, Split, Pierce, Echo, Infuse, Executioner, Leech, Keen, Vast, Lingering, Mending, Thunderclap,
    }

    /// <summary>
    /// A skill rune (§8): socketed into one skill, it bends that skill — more projectiles, a second echoing hit, a
    /// new element, cheaper, faster, wider, deadlier against the weak. One rune per skill; runes are items.
    /// </summary>
    [CreateAssetMenu(menuName = "AETHVIRSE/Skill Rune", fileName = "Rune")]
    public sealed class SkillRuneData : ScriptableObject
    {
        public string id = "";
        public string displayName = "";
        [TextArea] public string description = "";
        public RuneEffect effect;
        [Tooltip("Strength: +damage share, −cost share, extra projectiles, echo share …")]
        public float value = 0.3f;
        [Tooltip("Second number (Overcharge: extra MP share).")]
        public float value2;
        public ElementType element = ElementType.Physical;
        [Tooltip("Skill kinds it fits (empty = any skill that deals damage).")]
        public List<SkillKind> fits = new List<SkillKind>();

        public bool Fits(SkillData s)
        {
            if (s == null || s.IsPassive) return false;
            if (fits.Count > 0) return fits.Contains(s.kind);
            return s.IsDamaging;
        }
    }
}
