using Aethvirse.Interaction;
using Aethvirse.Player;
using Aethvirse.UI;
using UnityEngine;

namespace Aethvirse.World
{
    /// <summary>A waystone: unlocks when you pass it (flag waystone_&lt;id&gt;); using it opens fast travel.</summary>
    [RequireComponent(typeof(Interactable))]
    public sealed class Waystone : MonoBehaviour
    {
        public const float UnlockRange = 7f;
        [SerializeField] string waystoneId = "";
        [SerializeField] string displayName = "";

        public string Id => waystoneId;
        public static string Flag(string id) => "waystone_" + id;

        void Awake()
        {
            var i = GetComponent<Interactable>();
            i.Prompt = $"Waystone: {displayName} (fast travel)";
            i.Used += _ => FastTravelMenu.Open();
        }

        void Update()
        {
            Transform p = PlayerTarget.Current;
            if (p == null || GameSession.Instance.Flags.Has(Flag(waystoneId))) return;
            if ((p.position - transform.position).sqrMagnitude > UnlockRange * UnlockRange) return;
            GameSession.Instance.Flags.SetFlag(Flag(waystoneId));
            Codex.Notify($"Waystone attuned: {displayName}", CodexTone.System);
        }
    }
}
