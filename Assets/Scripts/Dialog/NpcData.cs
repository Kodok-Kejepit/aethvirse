using System.Collections.Generic;
using Aethvirse.Quests;
using UnityEngine;

namespace Aethvirse.Dialog
{
    /// <summary>An NPC: name, schedule and what they say (§10 dialog: branching, conditions, effects).</summary>
    [CreateAssetMenu(menuName = "AETHVIRSE/NPC", fileName = "Npc")]
    public sealed class NpcData : ScriptableObject
    {
        public string id = "";
        public string displayName = "";
        public Color bodyColor = new Color(0.6f, 0.6f, 0.6f);
        public List<DialogEntry> entries = new List<DialogEntry>();
        [Tooltip("Not around between these hours (-1 = always there).")]
        public int awayFromHour = -1;
        public int awayToHour = -1;
        public string hiddenWhenFlag = "";

        public bool IsAway(int hour)
        {
            if (awayFromHour < 0) return false;
            return awayFromHour <= awayToHour ? hour >= awayFromHour && hour < awayToHour : hour >= awayFromHour || hour < awayToHour;
        }

        public DialogEntry Pick(IConditionContext ctx)
        {
            foreach (DialogEntry e in entries) if (Condition.All(e.conditions, ctx)) return e;
            return null;
        }
    }
}
