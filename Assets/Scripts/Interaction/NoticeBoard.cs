using Aethvirse.Combat;
using Aethvirse.Save;
using Aethvirse.Stats;
using Aethvirse.UI;
using Aethvirse.World;
using UnityEngine;

namespace Aethvirse.Interaction
{
    /// <summary>Shows its text as a Codex message (signs, notice boards, notes).</summary>
    [RequireComponent(typeof(Interactable))]
    public sealed class NoticeBoard : MonoBehaviour
    {
        [TextArea] [SerializeField] string text = "";

        void Awake() => GetComponent<Interactable>().Used += _ => Codex.Notify(text, CodexTone.Info);
    }
}
