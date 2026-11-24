using System.Collections.Generic;
using Aethvirse.Quests;
using UnityEngine;

namespace Aethvirse.Secrets
{
    /// <summary>Append only (saves and assets store the number).</summary>
    public enum SecretCategory { Room, SealedMission, Puzzle, Chest, Item, Lore, Anomaly }

    /// <summary>
    /// One line of the Mystery Journal (§9): how it is found (conditions checked where its SecretSpot stands, or a
    /// flag), at least two in-game hints, and what it gives. "Seal" is an internal word only — never shown.
    /// </summary>
    [CreateAssetMenu(menuName = "AETHVIRSE/Secret", fileName = "Secret")]
    public sealed class SecretData : ScriptableObject
    {
        public string id = "";
        public string region = "";
        public SecretCategory category;
        [Tooltip("Journal line once found (vague, never the answer).")]
        public string title = "";
        [Tooltip("What the player can learn about it before finding it (≥ 2, §9.5).")]
        public List<string> hints = new List<string>();
        [Tooltip("All must hold at the spot (time, weather, items, flags …).")]
        public List<Condition> conditions = new List<Condition>();
        [Tooltip("Found when this flag gets set anywhere (puzzles, sealed missions, dialog). Empty = by its SecretSpot.")]
        public string foundByFlag = "";
        public List<Reward> rewards = new List<Reward>();
        [Range(0, 2)] public int tier;
    }
}
