using System.Collections.Generic;
using Aethvirse.Interaction;
using Aethvirse.UI;
using UnityEngine;

namespace Aethvirse.Quests
{
    /// <summary>The academy board: takes the first posted quest you have not done yet (bounties, academy jobs).</summary>
    [RequireComponent(typeof(Interactable))]
    public sealed class QuestBoard : MonoBehaviour
    {
        [SerializeField] List<string> quests = new List<string>();

        void Awake() => GetComponent<Interactable>().Used += _ => TakeNext();

        void TakeNext()
        {
            QuestSystem qs = QuestSystem.Instance;
            foreach (string id in quests)
            {
                if (qs.Log.IsActive(id) || qs.Log.IsCompleted(id)) continue;
                if (qs.Start(id)) return;
            }
            Codex.Notify("Nothing new on the board.", CodexTone.Info);
        }
    }
}
