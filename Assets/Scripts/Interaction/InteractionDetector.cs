using Aethvirse.Input;
using UnityEngine;

namespace Aethvirse.Interaction
{
    /// <summary>
    /// Picks the best interactable near the player (in range, preferring what is in front) and uses it on
    /// Interact (E / D-pad up / touch contextual button). Pure scoring is unit tested.
    /// </summary>
    public sealed class InteractionDetector : MonoBehaviour
    {
        [Tooltip("Facing reference (the player's visual).")]
        [SerializeField] Transform facing;

        public Interactable Current { get; private set; }

        void Update()
        {
            Current = PickBest();
            if (Current != null && !Dialog.DialogBox.IsOpen && !Secrets.CipherLock.IsOpen && !UI.MapView.IsOpen && GameInput.Instance.Interact.WasPressedThisFrame()) Current.Use(gameObject);
        }

        /// <summary>Touch button / UI.</summary>
        public void UseCurrent()
        {
            if (Current != null) Current.Use(gameObject);
        }

        Interactable PickBest()
        {
            Vector3 origin = transform.position;
            Vector3 forward = facing != null ? facing.forward : transform.forward;
            Interactable best = null;
            float bestScore = float.PositiveInfinity;
            foreach (Interactable it in Interactable.All)
            {
                if (!it.Available) continue;
                float score = Score(origin, forward, it.Point, it.Range);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = it;
                }
            }
            return best;
        }

        /// <summary>Distance, doubled for things behind you; +∞ out of range (height counts half).</summary>
        public static float Score(Vector3 origin, Vector3 forward, Vector3 point, float range)
        {
            Vector3 to = point - origin;
            float flat = new Vector2(to.x, to.z).magnitude;
            float dist = Mathf.Sqrt(flat * flat + to.y * to.y * 0.25f);
            if (dist > range) return float.PositiveInfinity;
            var f = new Vector3(forward.x, 0f, forward.z);
            bool behind = flat > 0.3f && Vector3.Dot(f, new Vector3(to.x, 0f, to.z)) < 0f;
            return behind ? dist * 2f : dist;
        }
    }
}
