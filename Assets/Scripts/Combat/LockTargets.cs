using System.Collections.Generic;
using UnityEngine;

namespace Aethvirse.Combat
{
    /// <summary>Something the player can lock on to (enemies, dummies).</summary>
    public interface ILockable
    {
        bool IsLockable { get; }
        Vector3 LockPoint { get; }
        Transform transform { get; }
    }

    /// <summary>Registry of lockable targets (no scene searches per frame).</summary>
    public static class LockTargets
    {
        static readonly List<ILockable> _all = new List<ILockable>();
        public static IReadOnlyList<ILockable> All => _all;

        public static void Register(ILockable target) { if (!_all.Contains(target)) _all.Add(target); }
        public static void Unregister(ILockable target) => _all.Remove(target);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearOnPlay() => _all.Clear();

        /// <summary>Lower is better: distance plus angle away from the view (radians × weight). Pure, unit tested.</summary>
        public static float Score(Vector3 origin, Vector3 forward, Vector3 candidate, float angleWeight)
        {
            Vector3 to = candidate - origin;
            to.y = 0f;
            var flatForward = new Vector3(forward.x, 0f, forward.z);
            float angle = to.sqrMagnitude > 0.0001f && flatForward.sqrMagnitude > 0.0001f
                ? Vector3.Angle(flatForward, to) * Mathf.Deg2Rad : 0f;
            return to.magnitude + angle * angleWeight;
        }
    }
}
