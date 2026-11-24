using System.Collections.Generic;
using Aethvirse.Combat;
using Aethvirse.Skills;
using UnityEngine;

namespace Aethvirse.Stats
{
    /// <summary>A playable class: stats (Balancing sheet "Kelas"), weapon combo graph, skills and skill combos.</summary>
    [CreateAssetMenu(menuName = "AETHVIRSE/Class", fileName = "Class")]
    public sealed class ClassData : ScriptableObject
    {
        public string id = "";
        public string displayName = "";
        [TextArea] public string description = "";
        public bool magicDamage;
        public StatBlock baseStats;
        [Tooltip("Automatic growth per level.")]
        public StatBlock growth;
        [Tooltip("Recommended split of free points (sums to 1): auto-allocate, balancing, enemy scaling.")]
        public StatBlock recommendedSplit;
        public ComboData combo;
        [Tooltip("Skills in unlock order. Slots 1–4 + ultimate are chosen from these.")]
        public List<SkillData> skills = new List<SkillData>();
        public List<SkillComboData> skillCombos = new List<SkillComboData>();

        public SkillData FindSkill(string skillId) => skills.Find(s => s != null && s.id == skillId);

        public SkillComboData FindCombo(string previous, float elapsed, string next) =>
            skillCombos.Find(c => c.Matches(previous, elapsed, next));
    }
}
