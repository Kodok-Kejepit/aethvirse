using System.Collections.Generic;
using Aethvirse.Skills;
using Aethvirse.Stats;
using UnityEngine;

namespace Aethvirse.Jobs
{
    /// <summary>
    /// A job (§8): Job Change I at Lv 30 (two branches per class), II at Lv 60, hidden jobs through sealed missions.
    /// A job adds stats, multipliers and its own skills on top of the class.
    /// </summary>
    [CreateAssetMenu(menuName = "AETHVIRSE/Job", fileName = "Job")]
    public sealed class JobData : ScriptableObject
    {
        public string id = "";
        public string displayName = "";
        [TextArea] public string description = "";
        [Tooltip("Class id this job belongs to; empty = any class (Summoner line).")]
        public string baseClass = "";
        public int tier = 1;
        public int requiredLevel = 30;
        [Tooltip("Job that must come first (tier 2+).")]
        public string parent = "";
        public bool hidden;
        [Tooltip("World flag needed (hidden jobs: set by their sealed mission).")]
        public string requiredFlag = "";
        public StatBlock statBonus;
        public float attackBonus;
        public float defenseBonus;
        public float hpBonus;
        public float mpCostMultiplier = 1f;
        public bool shadowArmy;
        public List<SkillData> skills = new List<SkillData>();
    }
}
