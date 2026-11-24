using Aethvirse.Combat;
using Aethvirse.Player;
using Aethvirse.Stats;
using UnityEngine;

namespace Aethvirse.Enemies
{
    public enum EnemyState { Idle, Chase, Attack, Hurt, Return, Dead }

    /// <summary>
    /// Graybox melee enemy (port of the Godot Enemy + states): patrols home, chases inside the aggro
    /// radius, attacks with a red ground telegraph when in range and an attack token is free, is
    /// staggered by enough poise damage, walks home past the leash (healing), dies and respawns.
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(Damageable))]
    public sealed class Enemy : MonoBehaviour, ILockable
    {
        const float TurnSpeed = 10f;
        const float MoveRate = 30f;
        const float PoiseReset = 2f;
        const float StaggerTime = 0.6f;
        const float DeathTime = 1.6f;
        const float WaitRingFactor = 1.8f;
        const float PatrolWaitMin = 1.5f;
        const float PatrolWaitMax = 3.5f;
        const float FlashTime = 0.12f;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] EnemyData data;
        [SerializeField] Transform visual;
        [SerializeField] Renderer bodyRenderer;
        [SerializeField] Transform telegraph;
        [SerializeField] WorldHealthBar healthBar;
        [Tooltip("Reference hero + tuning for level scaling.")]
        [SerializeField] ClassData scalingClass;
        [SerializeField] StatTuning tuning;

        CharacterController _body;
        Damageable _damageable;
        HitQuery _hits;
        MaterialPropertyBlock _block;
        readonly System.Random _rng = new System.Random();
        Vector3 _home;
        Quaternion _homeRotation;
        Vector3 _velocity;
        float _gravity;
        float _stateTime;
        float _cooldown;
        float _poise;
        float _sinceHit;
        float _flash;
        float _patrolWait;
        Vector3 _patrolGoal;
        Vector3 _attackDirection;
        DamageInfo _lastHit;

        public EnemyData Data => data;
        public EnemyState State { get; private set; } = EnemyState.Idle;
        public bool IsLockable => State != EnemyState.Dead && isActiveAndEnabled;
        public string DisplayName => $"{data.displayName}  Lv {data.level}";
        public Vector3 LockPoint => transform.position + Vector3.up * 1f;
        Vector3 Facing => visual.forward;
        Transform Target => PlayerTarget.Current;

        void Awake()
        {
            _body = GetComponent<CharacterController>();
            _damageable = GetComponent<Damageable>();
            ConfigureStats();
            _damageable.Health.Damaged += OnDamaged;
            _damageable.Health.Died += OnDied;
            _damageable.Health.Changed += (current, max) => { if (healthBar != null) healthBar.Set(current, max); };
            _hits = new HitQuery(gameObject, Team.Enemy);
            _block = new MaterialPropertyBlock();
            _gravity = Mathf.Abs(Physics.gravity.y);
            _home = transform.position;
            _homeRotation = visual.rotation;
            _patrolGoal = _home;
            ShowTelegraph(0f);
        }

        int _xp;

        /// <summary>Scaled numbers for the level and tier (Godot EnemyScaling), or the flat data.</summary>
        void ConfigureStats()
        {
            CombatStats stats = data.stats.Clone();
            int maxHp = data.maxHealth;
            _xp = data.xpReward;
            if (data.scaleByLevel && scalingClass != null && tuning != null)
            {
                EnemyScaling.Result r = EnemyScaling.Compute(data.level, data.tier, scalingClass, tuning);
                maxHp = Mathf.RoundToInt(r.maxHealth * data.hpScale);
                stats.attack = r.attack * data.attackScale;
                stats.defense = r.defense;
                _xp = r.xp;
            }
            _damageable.Configure(maxHp, stats, side: Team.Enemy);
        }

        void OnEnable() => LockTargets.Register(this);

        void OnDisable()
        {
            LockTargets.Unregister(this);
            AttackTokens.Release(this);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _stateTime += dt;
            _cooldown = Mathf.Max(_cooldown - dt, 0f);
            _sinceHit += dt;
            if (_sinceHit > PoiseReset) _poise = 0f;
            UpdateFlash(dt);
            switch (State)
            {
                case EnemyState.Idle: UpdateIdle(dt); break;
                case EnemyState.Chase: UpdateChase(dt); break;
                case EnemyState.Attack: UpdateAttack(dt); break;
                case EnemyState.Hurt: UpdateHurt(dt); break;
                case EnemyState.Return: UpdateReturn(dt); break;
                case EnemyState.Dead: UpdateDead(dt); break;
            }
        }

        // --- States ---------------------------------------------------------------------------

        void UpdateIdle(float dt)
        {
            if (DistanceToTarget() <= data.aggroRadius)
            {
                SetState(EnemyState.Chase);
                return;
            }
            if (Flat(_patrolGoal - transform.position).magnitude > 0.4f) MoveToward(_patrolGoal, data.walkSpeed, dt);
            else
            {
                Stop(dt);
                _patrolWait -= dt;
                if (_patrolWait <= 0f) PickPatrolGoal();
            }
            Move(dt);
        }

        void UpdateChase(float dt)
        {
            Transform target = Target;
            if (target == null || Flat(transform.position - _home).magnitude > data.leashRadius)
            {
                SetState(EnemyState.Return);
                return;
            }
            float dist = DistanceToTarget();
            if (dist <= data.attackRange)
            {
                Stop(dt);
                Face(target.position, dt);
                if (_cooldown <= 0f && AttackTokens.Request(this))
                {
                    SetState(EnemyState.Attack);
                    return;
                }
            }
            // No token / cooling down: hold a ring around the hero instead of crowding it.
            else if (dist <= data.attackRange * WaitRingFactor && _cooldown > 0f)
            {
                Stop(dt);
                Face(target.position, dt);
            }
            else MoveToward(target.position, data.chaseSpeed, dt);
            Move(dt);
        }

        void UpdateAttack(float dt)
        {
            float windupEnd = data.attackWindup;
            float activeEnd = windupEnd + data.attackActive;
            Transform target = Target;
            if (_stateTime < windupEnd)
            {
                // Tracks the hero during the first half of the windup only, so dodging works.
                if (target != null && _stateTime < windupEnd * 0.5f) Face(target.position, dt);
                _attackDirection = Facing;
                ShowTelegraph(_stateTime / windupEnd);
                Stop(dt);
            }
            else if (_stateTime < activeEnd)
            {
                ShowTelegraph(0f);
                _velocity.x = _attackDirection.x * data.attackLungeSpeed;
                _velocity.z = _attackDirection.z * data.attackLungeSpeed;
                _hits.Sweep(visual, data.attackHitboxOffset, data.attackHitboxSize, Strike);
            }
            else Stop(dt);
            Move(dt);
            if (_stateTime >= activeEnd + data.attackRecovery) SetState(EnemyState.Chase);
        }

        void UpdateHurt(float dt)
        {
            _velocity = MovementMath.ApproachHorizontal(_velocity, Vector3.zero, 12f, dt);
            // Rocks back and settles.
            visual.localRotation = Quaternion.Euler(-15f * (1f - _stateTime / StaggerTime), visual.localEulerAngles.y, 0f);
            Move(dt);
            if (_stateTime >= StaggerTime) SetState(EnemyState.Chase);
        }

        void UpdateReturn(float dt)
        {
            MoveToward(_home, data.chaseSpeed, dt);
            Move(dt);
            if (Flat(transform.position - _home).magnitude < 0.6f)
            {
                _damageable.Health.Reset();
                SetState(EnemyState.Idle);
            }
        }

        void UpdateDead(float dt)
        {
            Stop(dt);
            Move(dt);
            // Tip over, then wait for the respawn.
            float k = Mathf.Clamp01(_stateTime / 0.5f);
            visual.localRotation = Quaternion.Euler(Mathf.Lerp(0f, -85f, k), visual.localEulerAngles.y, 0f);
            if (_stateTime >= DeathTime) SetVisible(false);
            if (data.respawnDelay > 0f && _stateTime >= DeathTime + data.respawnDelay) Respawn();
        }

        void SetState(EnemyState next)
        {
            if (State == EnemyState.Attack)
            {
                ShowTelegraph(0f);
                AttackTokens.Release(this);
                _cooldown = data.attackCooldown;
            }
            if (State == EnemyState.Hurt) visual.localRotation = Quaternion.Euler(0f, visual.localEulerAngles.y, 0f);
            State = next;
            _stateTime = 0f;
            switch (next)
            {
                case EnemyState.Idle:
                    _patrolGoal = transform.position;
                    _patrolWait = RandomRange(PatrolWaitMin, PatrolWaitMax);
                    break;
                case EnemyState.Attack:
                    _hits.Begin();
                    break;
                case EnemyState.Hurt:
                    if (_lastHit != null)
                    {
                        _velocity.x = _lastHit.knockback.x * 1.5f;
                        _velocity.z = _lastHit.knockback.z * 1.5f;
                    }
                    break;
                case EnemyState.Dead:
                    _body.enabled = false;
                    if (healthBar != null) healthBar.gameObject.SetActive(false);
                    break;
            }
        }

        // --- Hits -----------------------------------------------------------------------------

        void Strike(Damageable target)
        {
            DamageInfo info = DamageCalculator.Resolve(_damageable.Stats, target.Stats, data.attackMultiplier,
                _damageable.Stats.element, _rng);
            info.stagger = data.attackStagger;
            info.launch = data.attackLaunch;
            info.source = gameObject;
            info.hitPosition = target.transform.position;
            Vector3 push = Flat(target.transform.position - transform.position);
            info.knockback = push.normalized * data.attackKnockback;
            target.Receive(info);
        }

        void OnDamaged(DamageInfo info)
        {
            _lastHit = info;
            _sinceHit = 0f;
            _poise += info.stagger;
            _flash = FlashTime;
            _velocity += new Vector3(info.knockback.x, 0f, info.knockback.z);
            if (_damageable.IsDead) return;
            if (_poise >= data.poise)
            {
                _poise = 0f;
                if (State == EnemyState.Attack) SetState(EnemyState.Chase);
                SetState(EnemyState.Hurt);
            }
            else if (State == EnemyState.Idle || State == EnemyState.Return) SetState(EnemyState.Chase);
        }

        void OnDied()
        {
            EnemyEvents.RaiseKilled(_xp);
            if (State == EnemyState.Attack) SetState(EnemyState.Chase);
            SetState(EnemyState.Dead);
        }

        void Respawn()
        {
            _body.enabled = false;
            transform.position = _home;
            _body.enabled = true;
            visual.rotation = _homeRotation;
            _velocity = Vector3.zero;
            _poise = 0f;
            _damageable.Health.Reset();
            if (healthBar != null) healthBar.gameObject.SetActive(true);
            SetVisible(true);
            SetState(EnemyState.Idle);
        }

        // --- Helpers --------------------------------------------------------------------------

        float DistanceToTarget()
        {
            Transform t = Target;
            return t == null ? float.PositiveInfinity : Flat(t.position - transform.position).magnitude;
        }

        void MoveToward(Vector3 point, float speed, float dt)
        {
            Vector3 to = Flat(point - transform.position);
            Vector3 desired = to.magnitude > 0.2f ? to.normalized * speed : Vector3.zero;
            _velocity = MovementMath.ApproachHorizontal(_velocity, desired, MoveRate, dt);
            Face(point, dt);
        }

        void Stop(float dt) => _velocity = MovementMath.ApproachHorizontal(_velocity, Vector3.zero, MoveRate, dt);

        void Face(Vector3 point, float dt)
        {
            Vector3 to = Flat(point - transform.position);
            if (to.sqrMagnitude < 0.0001f) return;
            Quaternion goal = Quaternion.Euler(0f, MovementMath.YawForDirection(to), 0f);
            visual.rotation = Quaternion.Slerp(visual.rotation, goal, MovementMath.SmoothWeight(TurnSpeed, dt));
        }

        void Move(float dt)
        {
            if (!_body.enabled) return;
            _velocity.y = _body.isGrounded && _velocity.y < 0f ? -2f : _velocity.y - _gravity * dt;
            _body.Move(_velocity * dt);
        }

        void PickPatrolGoal()
        {
            float angle = (float)_rng.NextDouble() * Mathf.PI * 2f;
            float dist = (float)_rng.NextDouble() * data.patrolRadius;
            _patrolGoal = _home + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
            _patrolWait = RandomRange(PatrolWaitMin, PatrolWaitMax);
        }

        void ShowTelegraph(float progress)
        {
            if (telegraph == null) return;
            bool on = progress > 0f;
            if (telegraph.gameObject.activeSelf != on) telegraph.gameObject.SetActive(on);
            if (!on) return;
            float r = data.telegraphRadius * Mathf.Clamp(progress, 0.15f, 1f) * 2f;
            telegraph.localScale = new Vector3(r, 0.02f, r);
            Vector3 ahead = Facing * (data.attackHitboxOffset.magnitude * 0.8f);
            telegraph.position = transform.position + new Vector3(ahead.x, 0.03f, ahead.z);
        }

        void UpdateFlash(float dt)
        {
            if (_flash <= 0f || bodyRenderer == null) return;
            _flash = Mathf.Max(_flash - dt, 0f);
            _block.SetColor(BaseColorId, Color.Lerp(data.bodyColor, Color.white, _flash / FlashTime));
            bodyRenderer.SetPropertyBlock(_block);
        }

        void SetVisible(bool on)
        {
            if (visual.gameObject.activeSelf != on) visual.gameObject.SetActive(on);
        }

        float RandomRange(float min, float max) => min + (float)_rng.NextDouble() * (max - min);

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
