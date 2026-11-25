using System.Collections;
using Aethvirse.Combat;
using Aethvirse.Player;
using Aethvirse.UI;
using UnityEngine;

namespace Aethvirse.Enemies
{
    /// <summary>What a rival can do besides its basic swings (STORY §6.6: rivals fight with hero skills).</summary>
    public enum RivalMove { Dash, Nova, Volley, BlinkStrike }

    /// <summary>
    /// A duelist on top of an Enemy body: reads your attacks and side-steps them, and every few seconds uses one of
    /// its own skills — a telegraphed dash, a ground nova, a volley, a blink behind you. Each move is announced so it
    /// can be dodged.
    /// </summary>
    public sealed class RivalDuelist : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        Enemy _enemy;
        Damageable _self;
        RivalMove[] _moves;
        float _evadeChance;
        ElementType _element;
        Color _color;
        float _moveTimer = 3f;
        float _evadeCooldown;
        bool _busy;
        string[] _lines;
        bool _spokeHalf;

        public static RivalDuelist Make(Enemy e, RivalMove[] moves, float evadeChance, ElementType element, Color color, string[] lines)
        {
            var r = e.gameObject.AddComponent<RivalDuelist>();
            r._enemy = e;
            r._self = e.GetComponent<Damageable>();
            r._moves = moves;
            r._evadeChance = evadeChance;
            r._element = element;
            r._color = color;
            r._lines = lines;
            r._self.Stats.element = element;
            return r;
        }

        void OnEnable() => Core.GameEvents.Attacked += OnPlayerAttack;
        void OnDisable() => Core.GameEvents.Attacked -= OnPlayerAttack;

        bool Alive => _enemy != null && _enemy.State != EnemyState.Dead && !_self.IsDead;

        void Update()
        {
            if (!Alive || _busy) return;
            _evadeCooldown -= Time.deltaTime;
            _moveTimer -= Time.deltaTime;
            if (!_spokeHalf && _enemy.HealthRatio < 0.5f && _lines.Length > 1)
            {
                _spokeHalf = true;
                Codex.Notify(_lines[1], CodexTone.Anomaly);
                _enemy.Enrage(1.2f, 0.8f);
            }
            Transform p = PlayerTarget.Current;
            if (_moveTimer <= 0f && p != null && _self.Status.Disabled == false)
            {
                _moveTimer = Random.Range(3.5f, 6f) * (_enemy.HealthRatio < 0.5f ? 0.7f : 1f);
                StartCoroutine(Perform(_moves[Random.Range(0, _moves.Length)], p));
            }
        }

        /// <summary>Reads your swing: sometimes steps aside before it lands.</summary>
        void OnPlayerAttack()
        {
            Transform p = PlayerTarget.Current;
            if (!Alive || _busy || _evadeCooldown > 0f || p == null || (p.position - transform.position).sqrMagnitude > 16f) return;
            if (Random.value > _evadeChance) return;
            _evadeCooldown = 2.5f;
            Vector3 side = Vector3.Cross(Vector3.up, (transform.position - p.position).normalized) * (Random.value < 0.5f ? 1f : -1f);
            _enemy.Teleport(transform.position + side * 3f + Vector3.up * 0.1f);
            DamageNumbers.Label(transform.position + Vector3.up * 2f, "EVADE", _color);
        }

        IEnumerator Perform(RivalMove move, Transform p)
        {
            _busy = true;
            switch (move)
            {
                case RivalMove.Dash:
                {
                    Vector3 dir = Flat(p.position - transform.position).normalized;
                    GameObject mark = Mark(transform.position + dir * 4f, new Vector3(1.6f, 0.02f, 8f), Quaternion.LookRotation(dir));
                    yield return new WaitForSeconds(0.6f);
                    Destroy(mark);
                    Vector3 from = transform.position;
                    _enemy.Teleport(from + dir * 8f + Vector3.up * 0.1f);
                    HitLine(from, from + dir * 8f, 1.2f, 1.6f);
                    break;
                }
                case RivalMove.Nova:
                {
                    GameObject mark = Mark(transform.position, new Vector3(10f, 0.02f, 10f), Quaternion.identity);
                    yield return new WaitForSeconds(0.9f);
                    Destroy(mark);
                    HitCircle(transform.position, 5f, 1.8f);
                    break;
                }
                case RivalMove.Volley:
                {
                    yield return new WaitForSeconds(0.45f);
                    Vector3 aim = (p.position + Vector3.up - (transform.position + Vector3.up)).normalized;
                    for (int i = 0; i < 5; i++)
                    {
                        Vector3 dir = Quaternion.Euler(0f, -24f + 12f * i, 0f) * aim;
                        Projectile.Fire(transform.position + Vector3.up * 1.1f, dir, 20f, 24f, 0.35f, 0, Team.Enemy, _color, t =>
                        {
                            DamageInfo info = DamageCalculator.Resolve(_self.Stats, t.Stats, 0.9f, _element, new System.Random());
                            info.source = gameObject;
                            info.hitPosition = t.transform.position;
                            return info;
                        });
                    }
                    break;
                }
                case RivalMove.BlinkStrike:
                {
                    DamageNumbers.Label(transform.position + Vector3.up * 2f, "…", _color);
                    yield return new WaitForSeconds(0.5f);
                    Vector3 behind = p.position - p.forward * 2f;
                    _enemy.Teleport(behind + Vector3.up * 0.1f);
                    GameObject mark = Mark(p.position, new Vector3(3f, 0.02f, 3f), Quaternion.identity);
                    yield return new WaitForSeconds(0.35f);
                    Destroy(mark);
                    HitCircle(transform.position, 2.2f, 2.2f);
                    break;
                }
            }
            yield return new WaitForSeconds(0.3f);
            _busy = false;
        }

        void HitCircle(Vector3 at, float radius, float mult)
        {
            Transform p = PlayerTarget.Current;
            if (p == null || Flat(p.position - at).magnitude > radius) return;
            Hit(p, mult);
        }

        void HitLine(Vector3 a, Vector3 b, float width, float mult)
        {
            Transform p = PlayerTarget.Current;
            if (p == null) return;
            Vector3 ab = Flat(b - a), ap = Flat(p.position - a);
            float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / Mathf.Max(ab.sqrMagnitude, 0.01f));
            if ((ap - ab * t).magnitude <= width) Hit(p, mult);
        }

        void Hit(Transform p, float mult)
        {
            var target = p.GetComponent<Damageable>();
            if (target == null) return;
            DamageInfo info = DamageCalculator.Resolve(_self.Stats, target.Stats, mult, _element, new System.Random());
            info.stagger = 40f;
            info.source = gameObject;
            info.hitPosition = p.position;
            info.knockback = Flat(p.position - transform.position).normalized * 6f;
            target.Receive(info);
        }

        GameObject Mark(Vector3 at, Vector3 size, Quaternion rot)
        {
            var go = GameObject.CreatePrimitive(size.x == size.z ? PrimitiveType.Cylinder : PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetPositionAndRotation(new Vector3(at.x, 0.04f, at.z), rot);
            go.transform.localScale = size;
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, new Color(_color.r, _color.g * 0.4f, _color.b * 0.4f, 1f));
            go.GetComponent<Renderer>().SetPropertyBlock(block);
            return go;
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
