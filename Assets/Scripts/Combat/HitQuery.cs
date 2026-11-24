using System.Collections.Generic;
using UnityEngine;

namespace Aethvirse.Combat
{
    /// <summary>
    /// One active hitbox window: an oriented box in front of the attacker. Each target is hit once per
    /// window (Godot HitboxComponent). Uses a shared buffer, so no allocations per frame.
    /// </summary>
    public sealed class HitQuery
    {
        static readonly Collider[] Buffer = new Collider[64];
        readonly HashSet<Damageable> _alreadyHit = new HashSet<Damageable>();
        readonly GameObject _owner;

        public HitQuery(GameObject owner) => _owner = owner;

        public void Begin() => _alreadyHit.Clear();

        /// <summary>Calls <paramref name="onNewTarget"/> for every target the box touches for the first time this window.</summary>
        public void Sweep(Transform frame, Vector3 localOffset, Vector3 size, System.Action<Damageable> onNewTarget)
        {
            Vector3 center = frame.TransformPoint(localOffset);
            int count = Physics.OverlapBoxNonAlloc(center, size * 0.5f, Buffer, frame.rotation, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                Damageable target = Buffer[i].GetComponentInParent<Damageable>();
                if (target == null || target.gameObject == _owner || target.IsDead || !_alreadyHit.Add(target)) continue;
                onNewTarget(target);
            }
        }
    }
}
