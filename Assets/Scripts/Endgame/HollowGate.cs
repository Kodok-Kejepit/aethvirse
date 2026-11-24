using System.Linq;
using Aethvirse.Interaction;
using Aethvirse.Save;
using Aethvirse.World;
using UnityEngine;

namespace Aethvirse.Endgame
{
    /// <summary>A white ring where the city fell: only at 03:xx, only for whoever holds the three keys. Leads to [ ??? ].</summary>
    [RequireComponent(typeof(Interactable))]
    public sealed class HollowGate : MonoBehaviour
    {
        public const string UnknownScene = "R0_Unknown";
        [SerializeField] GameObject ring;

        Interactable _use;

        void Awake()
        {
            _use = GetComponent<Interactable>();
            _use.Prompt = "Step into the white ring";
            _use.Used += _ =>
            {
                GameSession.Instance.Flags.SetFlag("region_r0_unknown_found");
                SceneTravel.Go(UnknownScene, null, rememberReturn: true);
            };
        }

        public static bool Open(WorldFlags flags, int hour) => hour == 3 && Endings.Keys.All(flags.Has);

        void Update()
        {
            GameSession s = GameSession.Instance;
            bool open = Open(s.Flags, s.Clock.Hour);
            _use.Available = open;
            if (ring != null && ring.activeSelf != open) ring.SetActive(open);
        }
    }
}
