using System.Collections.Generic;
using UnityEngine;

namespace Aethvirse.Quests
{
    /// <summary>All authored quests (Resources/QuestDatabase, built by QuestDataBuilder).</summary>
    [CreateAssetMenu(menuName = "AETHVIRSE/Quest Database", fileName = "QuestDatabase")]
    public sealed class QuestDatabase : ScriptableObject
    {
        public List<QuestData> quests = new List<QuestData>();
        public QuestData Get(string id) => quests.Find(q => q != null && q.id == id);
    }
}
