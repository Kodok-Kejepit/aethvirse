using System;
using System.Collections.Generic;
using Aethvirse.Quests;
using UnityEngine;

namespace Aethvirse.Dialog
{
    public enum DialogEffectType { GiveItem, TakeItem, GiveGold, TakeGold, SetFlag, StartQuest, AcademyPoints, CodexMessage, OpenShop }

    [Serializable]
    public sealed class DialogEffect
    {
        public DialogEffectType type;
        public string key = "";
        public int amount = 1;
        public string text = "";
    }

    [Serializable]
    public sealed class DialogChoice
    {
        public string text = "";
        public List<Condition> conditions = new List<Condition>();
        public List<DialogEffect> effects = new List<DialogEffect>();
        public List<string> reply = new List<string>();
        public int goldCost;
    }

    /// <summary>One thing an NPC can say: the first entry whose conditions hold is used (top to bottom).</summary>
    [Serializable]
    public sealed class DialogEntry
    {
        public string id = "";
        public List<Condition> conditions = new List<Condition>();
        public List<string> lines = new List<string>();
        [Tooltip("One of these is added depending on the day (small talk changes daily).")]
        public List<string> dailyVariants = new List<string>();
        public List<DialogEffect> onShow = new List<DialogEffect>();
        public List<DialogChoice> choices = new List<DialogChoice>();
    }
}
