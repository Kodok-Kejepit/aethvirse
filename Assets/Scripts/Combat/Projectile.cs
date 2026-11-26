using System;
using System.Collections.Generic;
using UnityEngine;

namespace Aethvirse.Combat
{
    /// <summary>
    /// A flying hit (bolts, arrows, thrown knives): moves straight (optionally homing a little), hits the first
    /// targets of the other team within its radius, may pierce, fades out at its range. Pooled-free and simple.
    /// </summary>
    public sealed class Projectile : MonoBehaviour
    {
        static readonly Collider[] Buffer = new Collider[16];
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        Team _team;
        Vector3 _velocity;
        float _range;
        float _travelled;
        float _radius;
        int _pierce;
        Transform _homing;
        float _homingRate;
        Func<Damageable, DamageInfo> _damage;
        Action<Damageable, DamageInfo> _onHit;
        readonly HashSet<Damageable> _hit = new HashSet<Damageable>();

        /// <summary>Fires one projectile. <paramref name="damage"/> builds the hit for a target (null = skip).</summary>
        public static Projectile Fire(Vector3 from, Vector3 direction, float speed, float range, float radius, int pierce, Team team,
            Color color, Func<Damageable, DamageInfo> damage, Action<Damageable, DamageInfo> onHit = null, Transform homing = null)
        {
            var go = Aethvirse.Core.RuntimeMat.Primitive(PrimitiveType.Sphere);
            go.name = "Projectile";
            go.transform.position = from;
            go.transform.localScale = new Vector3(radius * 1.2f, radius * 1.2f, radius * 3f);
            go.transform.rotation = Quaternion.LookRotation(direction.normalized);
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, color);
            go.GetComponent<Renderer>().SetPropertyBlock(block);
            var p = go.AddComponent<Projectile>();
            p._team = team;
            p._velocity = direction.normalized * speed;
            p._range = range;
            p._radius = radius;
            p._pierce = pierce;
            p._damage = damage;
            p._onHit = onHit;
            p._homing = homing;
            p._homingRate = homing != null ? 4f : 0f;
            return p;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (_homing != null)
            {
                Vector3 to = _homing.position + Vector3.up - transform.position;
                _velocity = Vector3.RotateTowards(_velocity, to, _homingRate * dt, 0f);
            }
            Vector3 step = _velocity * dt;
            transform.position += step;
            if (step.sqrMagnitude > 0f) transform.rotation = Quaternion.LookRotation(step);
            _travelled += step.magnitude;
            if (_travelled >= _range) { Destroy(gameObject); return; }

            int count = Physics.OverlapSphereNonAlloc(transform.position, _radius, Buffer, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                Damageable target = Buffer[i].GetComponentInParent<Damageable>();
                if (target == null)
                {
                    // Walls stop projectiles (static geometry without a Damageable).
                    if (!Buffer[i].isTrigger && Buffer[i].attachedRigidbody == null && !(Buffer[i] is CharacterController)) { Destroy(gameObject); return; }
                    continue;
                }
                if (target.Team == _team || target.IsDead || !_hit.Add(target)) continue;
                DamageInfo info = _damage?.Invoke(target);
                if (info == null) continue;
                bool applied = target.Receive(info);
                if (applied) _onHit?.Invoke(target, info);
                if (_pierce-- <= 0) { Destroy(gameObject); return; }
            }
        }
    }
}
