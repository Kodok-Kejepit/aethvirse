using Aethvirse.Interaction;
using Aethvirse.World;
using UnityEngine;

namespace Aethvirse.Dialog
{
    /// <summary>A talking NPC: interact to open its dialog; hidden while away (schedule) or when its flag is set.</summary>
    [RequireComponent(typeof(Interactable))]
    public sealed class Npc : MonoBehaviour
    {
        [SerializeField] NpcData data;
        [SerializeField] GameObject body;

        Interactable _interact;

        public NpcData Data => data;

        void Awake()
        {
            _interact = GetComponent<Interactable>();
            _interact.Prompt = $"Talk to {data.displayName}";
            _interact.Used += _ => DialogBox.Begin(data);
        }

        void Update()
        {
            GameSession s = GameSession.Instance;
            bool here = !data.IsAway(s.Clock.Hour) && !s.Flags.Has(data.hiddenWhenFlag);
            _interact.Available = here;
            if (body != null && body.activeSelf != here) body.SetActive(here);
        }
    }
}
