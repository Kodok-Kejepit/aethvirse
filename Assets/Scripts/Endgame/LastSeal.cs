using Aethvirse.Interaction;
using Aethvirse.UI;
using Aethvirse.World;
using UnityEngine;

namespace Aethvirse.Endgame
{
    /// <summary>The last line of the first world (inside [ ??? ]): opening it opens the next world on the atlas.</summary>
    [RequireComponent(typeof(Interactable))]
    public sealed class LastSeal : MonoBehaviour
    {
        void Awake()
        {
            var i = GetComponent<Interactable>();
            i.Prompt = "Read the last page";
            i.Used += _ =>
            {
                WorldFlags f = GameSession.Instance.Flags;
                if (f.Has(Endings.LastSealFlag)) { Codex.Notify("The page is written. Something beyond is looking back.", CodexTone.Anomaly); return; }
                f.SetFlag(Endings.LastSealFlag);
                Codex.Notify("The Codex remembers.", CodexTone.Anomaly);
                Codex.Notify("[ SYSTEM UPDATE ]  A new world appears on the map.", CodexTone.System);
            };
        }
    }
}
