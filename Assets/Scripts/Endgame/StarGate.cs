using Aethvirse.Interaction;
using Aethvirse.Save;
using Aethvirse.UI;
using Aethvirse.World;
using UnityEngine;

namespace Aethvirse.Endgame
{
    /// <summary>A gate between worlds (Part 2 / 3): dark until its flag is set, then it carries you to another planet.</summary>
    [RequireComponent(typeof(Interactable))]
    public sealed class StarGate : MonoBehaviour
    {
        [SerializeField] string targetScene = "";
        [SerializeField] string requiredFlag = "";
        [SerializeField] string label = "Step through to another world";
        [SerializeField] GameObject glow;

        Interactable _use;
        bool _announced;

        void Awake()
        {
            _use = GetComponent<Interactable>();
            _use.Prompt = label;
            _use.Used += _ => SceneTravel.Go(targetScene, null);
        }

        public static bool Open(WorldFlags flags, string flag) => string.IsNullOrEmpty(flag) || flags.Has(flag);

        void Update()
        {
            bool open = Open(GameSession.Instance.Flags, requiredFlag);
            _use.Available = open;
            if (glow != null && glow.activeSelf != open) glow.SetActive(open);
            if (open && !_announced && !string.IsNullOrEmpty(requiredFlag))
            {
                _announced = true;
                if (PlayerPrefs.GetInt("stargate_" + targetScene, 0) == 0)
                {
                    PlayerPrefs.SetInt("stargate_" + targetScene, 1);
                    Codex.Notify("A gate to another world is open.", CodexTone.System);
                }
            }
        }
    }
}
