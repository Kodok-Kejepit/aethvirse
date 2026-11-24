using System.Collections.Generic;
using Aethvirse.Combat;
using Aethvirse.Enemies;
using Aethvirse.Player;
using Aethvirse.UI;
using UnityEngine;

namespace Aethvirse.Jobs
{
    /// <summary>
    /// A summoned ally (familiar or shadow soldier): follows the player, fights the nearest enemy, vanishes when it falls.
    /// Built from an enemy body: its Enemy AI is switched off and it joins the player's team.
    /// </summary>
    public sealed class Minion : MonoBehaviour
    {
        const float FollowDistance = 3f;
        const float SeekRange = 16f;
        const float AttackRange = 2.2f;
        const float Windup = 0.35f;
        const float Cooldown = 1.1f;

        static readonly List<Minion> _all = new List<Minion>();
        public static IReadOnlyList<Minion> All => _all;

        Damageable _self;
        CharacterController _body;
        Transform _visual;
        HitQuery _hits;
        readonly System.Random _rng = new System.Random();
        Damageable _target;
        float _cooldown;
        float _windup = -1f;
        float _gravityVel;
        public ShadowArmy.Soldier Soldier { get; private set; }
        public float Power { get; private set; } = 1f;
        public bool Alive => _self != null && !_self.IsDead;

        /// <summary>Turns a spawned enemy body into an ally.</summary>
        public static Minion Convert(Enemy body, float hpShareOfPlayer, float power, ShadowArmy.Soldier soldier)
        {
            Damageable player = PlayerTarget.Current != null ? PlayerTarget.Current.GetComponent<Damageable>() : null;
            body.enabled = false;
            LockTargets.Unregister(body);
            var m = body.gameObject.AddComponent<Minion>();
            m._self = body.GetComponent<Damageable>();
            m._body = body.GetComponent<CharacterController>();
            m._visual = body.transform.Find("Visual");
            m.Soldier = soldier;
            m.Power = power;
            int hp = player != null ? Mathf.Max(10, Mathf.RoundToInt(player.Health.maxHealth * hpShareOfPlayer)) : 100;
            CombatStats stats = player != null ? player.Stats.Clone() : new CombatStats();
            stats.attack *= 0.5f * power;
            m._self.Configure(hp, stats, side: Team.Player);
            m._self.Health.Died += () => Destroy(m.gameObject, 0.5f);
            m._hits = new HitQuery(body.gameObject, Team.Player);
            return m;
        }

        void OnEnable() => _all.Add(this);
        void OnDisable() => _all.Remove(this);

        /// <summary>Nearest living minion to a point within range (enemies may pick it over the player).</summary>
        public static Minion Nearest(Vector3 p, float range)
        {
            Minion best = null;
            float bestD = range * range;
            foreach (Minion m in _all)
            {
                if (!m.Alive) continue;
                float d = (m.transform.position - p).sqrMagnitude;
                if (d < bestD) { bestD = d; best = m; }
            }
            return best;
        }

        void Update()
        {
            if (!Alive || _body == null) return;
            float dt = Time.deltaTime;
            _cooldown -= dt;
            if (_target == null || _target.IsDead || Dist(_target.transform) > SeekRange) _target = FindEnemy();
            Vector3 goal;
            if (_target != null) goal = _target.transform.position;
            else if (PlayerTarget.Current != null) goal = PlayerTarget.Current.position;
            else return;

            float dist = Dist(goal);
            Vector3 move = Vector3.zero;
            if (_windup >= 0f)
            {
                _windup += dt;
                if (_windup >= Windup)
                {
                    _hits.Begin();
                    _hits.Sweep(_visual, new Vector3(0f, 1f, 1.1f), new Vector3(2f, 1.6f, 2f), Strike);
                    _windup = -1f;
                    _cooldown = Cooldown / (Soldier != null && Soldier.rank >= 4 ? 1.25f : 1f);
                }
            }
            else if (_target != null && dist <= AttackRange)
            {
                if (_cooldown <= 0f) _windup = 0f;
            }
            else if (_target != null || dist > FollowDistance)
            {
                Vector3 to = goal - transform.position;
                to.y = 0f;
                move = to.normalized * 5.5f;
            }
            Vector3 face = goal - transform.position;
            face.y = 0f;
            if (face.sqrMagnitude > 0.01f && _visual != null)
                _visual.rotation = Quaternion.Slerp(_visual.rotation, Quaternion.LookRotation(face), 1f - Mathf.Exp(-10f * dt));
            _gravityVel = _body.isGrounded ? -2f : _gravityVel - 9.81f * dt;
            _body.Move((move + Vector3.up * _gravityVel) * dt);
        }

        void Strike(Damageable target)
        {
            DamageInfo info = DamageCalculator.Resolve(_self.Stats, target.Stats, 1f, _self.Stats.element, _rng);
            info.stagger = 15f;
            info.source = gameObject;
            Vector3 push = target.transform.position - transform.position;
            push.y = 0f;
            info.knockback = push.normalized * 3f;
            if (!target.Receive(info)) return;
            DamageNumbers.Spawn(target.transform.position + Vector3.up * 1.8f, info.amount, info.isCrit, info.element, false);
            if (target.IsDead && Soldier != null && ShadowArmy.GainXp(Soldier, 5))
                Codex.Notify($"Shadow rank up: {ShadowArmy.RankNames[Soldier.rank]}", CodexTone.Reward);
        }

        Damageable FindEnemy()
        {
            Damageable best = null;
            float bestD = SeekRange;
            foreach (ILockable l in LockTargets.All)
            {
                if (!(l is Enemy e) || e == null || !e.IsLockable) continue;
                float d = Dist(e.transform);
                if (d < bestD) { bestD = d; best = e.GetComponent<Damageable>(); }
            }
            return best;
        }

        float Dist(Transform t) => Dist(t.position);
        float Dist(Vector3 p) { Vector3 d = p - transform.position; d.y = 0f; return d.magnitude; }
    }
}
