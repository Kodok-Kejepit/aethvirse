using Aethvirse.Cameras;
using Aethvirse.Combat;
using Aethvirse.Input;
using UnityEngine;

namespace Aethvirse.Player
{
    /// <summary>
    /// Lock-on (port of the Godot LockOnTargeter): picks the best target near the view, switches
    /// left/right (mouse wheel or a stick flick), drops lost targets, shows a marker above the target.
    /// </summary>
    public sealed class LockOnTargeter : MonoBehaviour
    {
        [SerializeField] PlayerCameraRig cameraRig;
        [SerializeField] Transform marker;
        [SerializeField] float maxDistance = 22f;
        [Tooltip("Targets outside this cone (degrees from the view) are only used if nothing is inside.")]
        [SerializeField] float preferredCone = 70f;
        [SerializeField] float angleWeight = 8f;
        [SerializeField] float markerHeight = 0.7f;
        [SerializeField] float stickFlick = 0.75f;

        GameInput _input;
        bool _stickArmed = true;

        public ILockable Target { get; private set; }

        void Awake()
        {
            _input = GameInput.Instance;
            if (marker != null) marker.gameObject.SetActive(false);
        }

        void Update()
        {
            if (_input.LockOn.WasPressedThisFrame())
                SetTarget(Target != null ? null : PickBest(null));
            else if (Target != null)
            {
                if (_input.LockNext.WasPressedThisFrame()) Switch(1);
                else if (_input.LockPrev.WasPressedThisFrame()) Switch(-1);
                HandleStickFlick();
            }
            if (Target != null && !IsValid(Target)) SetTarget(null);
        }

        void LateUpdate()
        {
            if (marker == null || Target == null) return;
            marker.position = Target.LockPoint + Vector3.up * markerHeight;
            marker.Rotate(0f, 180f * Time.unscaledDeltaTime, 0f, Space.World);
        }

        public void SetTarget(ILockable target)
        {
            Target = target;
            if (cameraRig != null) cameraRig.LockTarget = target;
            if (marker != null) marker.gameObject.SetActive(target != null);
        }

        ILockable PickBest(ILockable exclude)
        {
            Vector3 origin = transform.position;
            Vector3 forward = cameraRig != null ? cameraRig.transform.forward : transform.forward;
            ILockable best = null;
            float bestScore = float.PositiveInfinity;
            bool bestInCone = false;
            foreach (ILockable candidate in LockTargets.All)
            {
                if (candidate == exclude || !IsValid(candidate)) continue;
                Vector3 to = candidate.LockPoint - origin;
                bool inCone = Vector3.Angle(new Vector3(forward.x, 0f, forward.z), new Vector3(to.x, 0f, to.z)) <= preferredCone;
                float score = LockTargets.Score(origin, forward, candidate.LockPoint, angleWeight);
                if ((inCone && !bestInCone) || (inCone == bestInCone && score < bestScore))
                {
                    best = candidate;
                    bestScore = score;
                    bestInCone = inCone;
                }
            }
            return best;
        }

        /// <summary>+1 = next target to the right on screen, -1 = to the left.</summary>
        void Switch(int direction)
        {
            if (Target == null || cameraRig == null) return;
            Transform cam = cameraRig.transform;
            float currentSide = Vector3.Dot(cam.right, Target.LockPoint - cam.position);
            ILockable best = null;
            float bestDelta = float.PositiveInfinity;
            foreach (ILockable candidate in LockTargets.All)
            {
                if (candidate == Target || !IsValid(candidate)) continue;
                float delta = (Vector3.Dot(cam.right, candidate.LockPoint - cam.position) - currentSide) * direction;
                if (delta > 0f && delta < bestDelta)
                {
                    best = candidate;
                    bestDelta = delta;
                }
            }
            if (best != null) SetTarget(best);
        }

        void HandleStickFlick()
        {
            float x = _input.LookStick.ReadValue<Vector2>().x;
            if (Mathf.Abs(x) < stickFlick * 0.5f) _stickArmed = true;
            else if (_stickArmed && Mathf.Abs(x) >= stickFlick)
            {
                _stickArmed = false;
                Switch(x > 0f ? 1 : -1);
            }
        }

        bool IsValid(ILockable candidate)
        {
            // A destroyed MonoBehaviour compares equal to null through UnityEngine.Object.
            if (candidate == null || (candidate is Object o && o == null)) return false;
            return candidate.IsLockable && Vector3.Distance(transform.position, candidate.transform.position) <= maxDistance;
        }
    }
}
