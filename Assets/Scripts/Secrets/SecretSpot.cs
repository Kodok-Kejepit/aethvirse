using Aethvirse.Interaction;
using Aethvirse.Player;
using Aethvirse.Quests;
using UnityEngine;

namespace Aethvirse.Secrets
{
    /// <summary>
    /// Where a secret is found: walk in while its conditions hold (night, rain, an item in the bag …), or use it
    /// (interact mode). A faint shimmer shows within the Sense range (higher SEN = further), never the answer.
    /// </summary>
    public sealed class SecretSpot : MonoBehaviour
    {
        [SerializeField] string secretId = "";
        [SerializeField] float radius = 3f;
        [Tooltip("Found by using it (E) instead of standing in it.")]
        [SerializeField] bool interact;
        [SerializeField] string prompt = "Look closer";

        GameObject _shimmer;
        Interactable _use;

        void Start()
        {
            _shimmer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(_shimmer.GetComponent<Collider>());
            _shimmer.transform.SetParent(transform, false);
            _shimmer.transform.localPosition = Vector3.up;
            _shimmer.transform.localScale = Vector3.one * 0.3f;
            _shimmer.SetActive(false);
            if (interact)
            {
                _use = gameObject.AddComponent<Interactable>();
                _use.Prompt = prompt;
                _use.Range = radius;
                _use.Used += _ => TryFind();
            }
        }

        SecretData Data => SecretDatabase.Instance != null ? SecretDatabase.Instance.Get(secretId) : null;
        bool Found => SecretSystem.Instance.State.IsFound(secretId);

        void Update()
        {
            Transform p = PlayerTarget.Current;
            if (p == null || Found) { if (_shimmer != null) _shimmer.SetActive(false); if (_use != null) _use.Available = false; return; }
            float d = Vector3.Distance(p.position, transform.position);
            float sense = 6f + GameContext.Instance.Stat("sen") * 0.25f;
            if (_shimmer != null) _shimmer.SetActive(d < sense);
            if (_shimmer != null) _shimmer.transform.localScale = Vector3.one * (0.25f + 0.08f * Mathf.Sin(Time.time * 4f));
            if (!interact && d <= radius) TryFind();
        }

        void TryFind()
        {
            SecretData s = Data;
            if (s == null || !Condition.All(s.conditions, GameContext.Instance)) return;
            SecretSystem.Instance.Find(secretId);
        }
    }
}
