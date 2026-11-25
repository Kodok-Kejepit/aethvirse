using System;
using UnityEngine;

namespace Aethvirse.Combat
{
    /// <summary>Hits only land on the other side (enemies never hit each other or dummies).</summary>
    public enum Team { Player, Enemy }

    /// <summary>
    /// Anything that can be hit: holds the health pool and defensive stats (the Godot Hurtbox + Health).
    /// Hits are found by <see cref="HitQuery"/> through any collider on this object or its children.
    /// </summary>
    public sealed class Damageable : MonoBehaviour
    {
        [SerializeField] HealthPool health = new HealthPool();
        [SerializeField] CombatStats stats = new CombatStats();
        [SerializeField] Team team = Team.Enemy;

        public HealthPool Health => health;
        public CombatStats Stats => stats;
        public bool IsDead => health.IsDead;
        public Team Team => team;

        /// <summary>Raised for every hit that reaches this object (applied or dodged).</summary>
        public event Action<DamageInfo, bool> HitReceived;
        /// <summary>Raised when a hit triggers an elemental reaction on this object.</summary>
        public event Action<ReactionType, DamageInfo> Reacted;
        /// <summary>Raised for every reaction anywhere (combo heat, Codex, sound).</summary>
        public static event Action<ReactionType> AnyReaction;

        readonly StatusSet _status = new StatusSet();
        public StatusSet Status => _status;
        /// <summary>The element left by the last elemental hit (Physical = none) and how long it stays.</summary>
        public ElementType Aura { get; private set; } = ElementType.Physical;
        float _auraTime;
        /// <summary>Elite shields and similar: scales every incoming hit.</summary>
        public float DamageTakenMultiplier { get; set; } = 1f;

        public const float AuraTime = 8f;
        static readonly Collider[] Splash = new Collider[24];

        void Awake() => health.Reset();

        void Update()
        {
            float dt = Time.deltaTime;
            health.Tick(dt);
            _auraTime -= dt;
            if (_auraTime <= 0f) Aura = ElementType.Physical;
            int dot = _status.Tick(dt, health.maxHealth);
            if (dot > 0 && !health.IsDead && !health.Invulnerable)
            {
                health.TakeDamage(new DamageInfo { amount = dot, element = ElementType.Fire, hitPosition = transform.position });
                DamageNumbers.Spawn(transform.position + Vector3.up * 1.6f, dot, false, ElementType.Fire, team == Team.Player);
            }
        }

        public void Configure(int maxHealth, CombatStats newStats, bool immortal = false, Team side = Team.Enemy)
        {
            team = side;
            health.maxHealth = maxHealth;
            health.immortal = immortal;
            if (newStats != null) stats = newStats;
            health.Reset();
        }

        public void SetStats(CombatStats newStats) => stats = newStats;

        /// <summary>Returns true if the damage was applied.</summary>
        public bool Receive(DamageInfo info)
        {
            ReactionType reaction = ReactionType.None;
            if (!health.IsDead && !health.Invulnerable)
            {
                bool frozen = _status.Has(StatusType.Freeze);
                if (_auraTime > 0f || frozen) reaction = Reactions.Resolve(Aura, info.element, frozen, info.isHeavy);
                float mult = Reactions.Multiplier(reaction) * _status.DamageTakenFactor * DamageTakenMultiplier;
                info.amount = Mathf.Max(1, Mathf.RoundToInt(info.amount * mult));
                if (reaction == ReactionType.Erase) info.amount += Mathf.RoundToInt(health.maxHealth * (health.maxHealth > 20000 ? 0.02f : 0.08f));
            }
            bool applied = health.TakeDamage(info);
            if (applied)
            {
                if (reaction != ReactionType.None) React(reaction, info);
                else if (info.element != ElementType.Physical) { Aura = info.element; _auraTime = AuraTime; }
                var st = StatusSet.FromElement(info.element);
                if (st.HasValue && reaction != ReactionType.Melt) _status.Apply(st.Value.type, st.Value.time);
            }
            HitReceived?.Invoke(info, applied);
            return applied;
        }

        /// <summary>Applies a reaction's after-effects and clears the aura.</summary>
        void React(ReactionType r, DamageInfo info)
        {
            Aura = ElementType.Physical;
            _auraTime = 0f;
            switch (r)
            {
                case ReactionType.Melt: _status.Clear(StatusType.Freeze); _status.Clear(StatusType.Chill); break;
                case ReactionType.Shatter: _status.Clear(StatusType.Freeze); break;
                case ReactionType.Magma: _status.Apply(StatusType.Burn, 4f, 2); _status.Apply(StatusType.Slow, 4f); break;
                case ReactionType.Blizzard: _status.Apply(StatusType.Slow, 4f); _status.Apply(StatusType.Chill, 4f, 2); break;
                case ReactionType.Permafrost: _status.Apply(StatusType.Root, 2f); _status.Apply(StatusType.Corrode, 5f); break;
                case ReactionType.Superconduct: _status.Apply(StatusType.Exposed, 6f); break;
                case ReactionType.Sandstorm: _status.Apply(StatusType.Blind, 4f); break;
                case ReactionType.Corrode: _status.Apply(StatusType.Corrode, 6f); break;
                case ReactionType.Purge: _status.Clear(StatusType.Corrode); break;
                case ReactionType.Overload: _status.Apply(StatusType.Stun, 0.6f); break;
                case ReactionType.Erase: _status.ClearAll(); break;
            }
            DamageNumbers.Label(transform.position + Vector3.up * 1.8f, Reactions.Name(r), Element.ColorOf(info.element));
            Reacted?.Invoke(r, info);
            AnyReaction?.Invoke(r);
            float radius = Reactions.SplashRadius(r);
            if (radius > 0f) SplashNearby(r, info, radius);
        }

        /// <summary>Firestorm, Overload, Thunderstorm, Magnetize …: a weaker copy (or a pull) for others on this side.</summary>
        void SplashNearby(ReactionType r, DamageInfo info, float radius)
        {
            int n = Physics.OverlapSphereNonAlloc(transform.position, radius, Splash, ~0, QueryTriggerInteraction.Collide);
            int chained = 0;
            for (int i = 0; i < n; i++)
            {
                Damageable other = Splash[i].GetComponentInParent<Damageable>();
                if (other == null || other == this || other.team != team || other.IsDead) continue;
                if (r == ReactionType.Thunderstorm && ++chained > 4) break;
                if (r == ReactionType.Magnetize)
                {
                    other.Status.Apply(StatusType.Stun, 0.5f);
                    var cc = other.GetComponent<CharacterController>();
                    if (cc != null && cc.enabled) cc.Move(Vector3.ClampMagnitude(transform.position - other.transform.position, 3f) * 0.6f);
                    continue;
                }
                // The copy carries no element, so splashes never chain into new reactions.
                other.health.TakeDamage(new DamageInfo { amount = Mathf.Max(1, info.amount / 3), source = info.source, hitPosition = other.transform.position });
                DamageNumbers.Spawn(other.transform.position + Vector3.up * 1.4f, Mathf.Max(1, info.amount / 3), false, info.element, other.team == Team.Player);
                if (r == ReactionType.Firestorm) other.Status.Apply(StatusType.Burn, 3f);
                if (r == ReactionType.Blizzard) other.Status.Apply(StatusType.Slow, 3f);
                if (r == ReactionType.Sandstorm) other.Status.Apply(StatusType.Blind, 3f);
            }
        }
    }
}
