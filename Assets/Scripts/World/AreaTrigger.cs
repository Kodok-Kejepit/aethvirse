using Aethvirse.Core;
using Aethvirse.Player;
using UnityEngine;

namespace Aethvirse.World
{
    /// <summary>A named box area: raises GameEvents.AreaEntered when the player walks in (quests, journal, secrets).</summary>
    public sealed class AreaTrigger : MonoBehaviour
    {
        [SerializeField] string areaId = "";
        [SerializeField] Vector3 size = new Vector3(10f, 6f, 10f);
        [Tooltip("Optional world flag set on first entry.")]
        [SerializeField] string setFlag = "";

        bool _inside;

        public string AreaId => areaId;
        public bool Contains(Vector3 p)
        {
            Vector3 local = transform.InverseTransformPoint(p);
            return Mathf.Abs(local.x) <= size.x * 0.5f && Mathf.Abs(local.y) <= size.y * 0.5f && Mathf.Abs(local.z) <= size.z * 0.5f;
        }

        void Update()
        {
            Transform player = PlayerTarget.Current;
            bool inside = player != null && Contains(player.position);
            if (inside && !_inside)
            {
                GameEvents.RaiseAreaEntered(areaId);
                if (!string.IsNullOrEmpty(setFlag)) GameSession.Instance.Flags.SetFlag(setFlag);
            }
            _inside = inside;
        }

        void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.3f);
            Gizmos.DrawWireCube(Vector3.zero, size);
        }
    }
}
