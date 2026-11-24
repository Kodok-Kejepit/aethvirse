using System;
using Aethvirse.Combat;
using UnityEngine;

namespace Aethvirse.Skills
{
    /// <summary>A trap on the ground: arms after a moment, bursts when an enemy steps in, fades after its lifetime.</summary>
    public sealed class Trap : MonoBehaviour
    {
        const float ArmTime = 0.6f;
        static readonly Collider[] Buffer = new Collider[24];
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        float _radius;
        float _life;
        float _age;
        Func<Damageable, DamageInfo> _damage;
        Action<Damageable, DamageInfo> _onHit;

        public static void Place(Vector3 at, float radius, float lifetime, Color color, Func<Damageable, DamageInfo> damage, Action<Damageable, DamageInfo> onHit)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Trap";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = at + Vector3.up * 0.03f;
            go.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, color);
            go.GetComponent<Renderer>().SetPropertyBlock(block);
            var t = go.AddComponent<Trap>();
            t._radius = radius;
            t._life = lifetime;
            t._damage = damage;
            t._onHit = onHit;
        }

        void Update()
        {
            _age += Time.deltaTime;
            if (_age >= _life) { Destroy(gameObject); return; }
            if (_age < ArmTime) return;
            int count = Physics.OverlapSphereNonAlloc(transform.position, _radius, Buffer, ~0, QueryTriggerInteraction.Collide);
            bool triggered = false;
            for (int i = 0; i < count; i++)
            {
                Damageable d = Buffer[i].GetComponentInParent<Damageable>();
                if (d != null && d.Team == Team.Enemy && !d.IsDead) { triggered = true; break; }
            }
            if (!triggered) return;
            // Burst: everything of the enemy team inside takes the hit once.
            var hit = new System.Collections.Generic.HashSet<Damageable>();
            for (int i = 0; i < count; i++)
            {
                Damageable d = Buffer[i].GetComponentInParent<Damageable>();
                if (d == null || d.Team != Team.Enemy || d.IsDead || !hit.Add(d)) continue;
                DamageInfo info = _damage(d);
                if (d.Receive(info)) _onHit?.Invoke(d, info);
            }
            Destroy(gameObject);
        }
    }
}
