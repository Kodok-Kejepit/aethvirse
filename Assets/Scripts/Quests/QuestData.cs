using System;
using System.Collections.Generic;
using UnityEngine;

namespace Aethvirse.Quests
{
    public enum QuestKind { Story, Side, Daily, Emergency, Academy, Bounty }
    public enum ObjectiveType { Kill, Talk, Collect, ReachArea, ClearGate, Flag, Attack, Dodge, Distance, Pickup, Craft, UseItem, Skill, Secret }
    public enum RewardType { Item, Gold, Xp, Flag, AcademyPoints, CodexPoints, CodexMessage, StartQuest }

    /// <summary>One step of a quest. Objectives of the same stage run in parallel; stages run in order (§10 quest graph).</summary>
    [Serializable]
    public sealed class QuestObjective
    {
        public ObjectiveType type;
        [Tooltip("Enemy id, NPC id, item id, area id, flag … (empty = any).")]
        public string target = "";
        public int count = 1;
        public string text = "";
        public int stage;
        public bool optional;
    }

    [Serializable]
    public sealed class Reward
    {
        public RewardType type;
        public string key = "";
        public int amount = 1;
        public string text = "";
    }

    /// <summary>A quest (story, side, academy, bounty; daily / emergency ones are built at runtime).</summary>
    [CreateAssetMenu(menuName = "AETHVIRSE/Quest", fileName = "Quest")]
    public sealed class QuestData : ScriptableObject
    {
        public string id = "";
        public string title = "";
        [TextArea] public string description = "";
        public QuestKind kind = QuestKind.Side;
        [Tooltip("Starts by itself once its conditions hold.")]
        public bool autoStart;
        public List<Condition> startConditions = new List<Condition>();
        [Tooltip("Seconds; 0 = no limit (emergency quests).")]
        public float timeLimit;
        public int academyPoints;
        public List<QuestObjective> objectives = new List<QuestObjective>();
        public List<Reward> rewards = new List<Reward>();

        public int StageCount
        {
            get
            {
                int top = -1;
                foreach (QuestObjective o in objectives) top = Mathf.Max(top, o.stage);
                return top + 1;
            }
        }
    }
}
