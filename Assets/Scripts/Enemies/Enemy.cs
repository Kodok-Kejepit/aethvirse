using Aethvirse.Combat;
using Aethvirse.Player;
using Aethvirse.Stats;
using UnityEngine;

namespace Aethvirse.Enemies
{
    public enum EnemyState { Idle, Chase, Attack, Hurt, Return, Dead, Airborne }

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
        public string DisplayName => $"{EliteAffixes.Prefix(Elite != null ? Elite.Affixes : null)}{data.displayName}  Lv {Level}";
        /// <summary>Set when this enemy is an elite (affixes).</summary>
        public EliteAffixes Elite { get; set; }
        public int CurrentLevel => Level;
        /// <summary>Damage this enemy dealt (vampiric elites heal from it).</summary>
        public event System.Action<int> DealtDamage;

        public void Teleport(Vector3 to)
        {
            _body.enabled = false;
            transform.position = to;
            _body.enabled = true;
            _velocity = Vector3.zero;
        }

        /// <summary>Smaller and weaker copy (splitter elites).</summary>
        public void Shrink(float factor)
        {
            visual.localScale *= factor;
            _damageable.Health.SetMax(Mathf.Max(1, Mathf.RoundToInt(_damageable.Health.maxHealth * factor * 0.5f)), refill: true);
        }
        /// <summary>Spawned waves remove themselves instead of respawning.</summary>
        public bool RespawnAtHome { get; set; } = true;
        public Vector3 LockPoint => transform.position + Vector3.up * 1f;
        Vector3 Facing => visual.forward;
        /// <summary>The player, or a minion that is clearly closer (summoner / shadow armies).</summary>
        Transform Target
        {
            get
            {
                Transform player = PlayerTarget.Visible;
                Jobs.Minion m = Jobs.Minion.Nearest(transform.position, AggroRadius);
                if (m == null) return player;
                if (player == null) return m.transform;
                return (m.transform.position - transform.position).sqrMagnitude + 4f < (player.position - transform.position).sqrMagnitude ? m.transform : player;
            }
        }

        /// <summary>Recently fallen enemies (shadow extraction reads these).</summary>
        public static readonly System.Collections.Generic.List<(string type, int level, Vector3 at, float time)> Fallen =
            new System.Collections.Generic.List<(string, int, Vector3, float)>();

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
        int _gold;

        /// <summary>Scaled numbers for the level and tier (Godot EnemyScaling), or the flat data.</summary>
        void ConfigureStats()
        {
            CombatStats stats = data.stats.Clone();
            int maxHp = data.maxHealth;
            _xp = data.xpReward;
            _gold = data.goldReward;
            if (data.scaleByLevel && scalingClass != null && tuning != null)
            {
                EnemyScaling.Result r = EnemyScaling.Compute(Level, data.tier, scalingClass, tuning);
                maxHp = Mathf.RoundToInt(r.maxHealth * data.hpScale);
                stats.attack = r.attack * data.attackScale;
                stats.defense = r.defense;
                _xp = r.xp;
                _gold = r.gold;
            }
            _damageable.Configure(maxHp, stats, side: Team.Enemy);
        }

        /// <summary>Turns this body into another enemy type (catalog spawns): stats, colour, size.</summary>
        public void SetData(EnemyData newData)
        {
            data = newData;
            visual.localScale = Vector3.one * data.size;
            if (bodyRenderer != null)
            {
                _block.SetColor(BaseColorId, data.bodyColor);
                bodyRenderer.SetPropertyBlock(_block);
            }
            ConfigureStats();
        }

        float _speedMult = 1f;
        float _cooldownMult = 1f;

        /// <summary>Gate bosses: more HP and damage, bigger body.</summary>
        public void MakeBoss(float hpMult, float atkMult, float sizeMult)
        {
            _damageable.Health.SetMax(Mathf.RoundToInt(_damageable.Health.maxHealth * hpMult), refill: true);
            _launchImmune = true;
            _damageable.Stats.attack *= atkMult;
            visual.localScale *= sizeMult;
            _body.radius *= sizeMult;
            _body.height *= sizeMult;
            _body.center *= sizeMult;
        }

        /// <summary>Boss phases: faster and attacking more often.</summary>
        public void Enrage(float speedMult, float cooldownMult)
        {
            _speedMult *= speedMult;
            _cooldownMult *= cooldownMult;
        }

        public float HealthRatio => _damageable.Health.Ratio;

        int _levelOverride;
        float _aggroOverride;

        /// <summary>Runtime spawns (waves, Gates): re-scale to a level and optionally widen the aggro radius.</summary>
        public void SetLevel(int level, float aggroRadius = 0f, bool dropsLoot = true)
        {
            _levelOverride = level;
            _aggroOverride = aggroRadius;
            _noLoot = !dropsLoot;
            ConfigureStats();
        }

        bool _noLoot;
        float AggroRadius => _aggroOverride > 0f ? _aggroOverride : data.aggroRadius;
        int Level => _levelOverride > 0 ? _levelOverride : data.level;

        void OnEnable() => LockTargets.Register(this);

        void OnDisable()
        {
            LockTargets.Unregister(this);
            AttackTokens.Release(this);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            // Frozen, stunned, petrified or rooted: the AI waits (the windup does not advance).
            if (State != EnemyState.Dead && _damageable.Status.Disabled)
            {
                ShowTelegraph(0f);
                Stop(dt);
                Move(dt);
                UpdateFlash(dt);
                return;
            }
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
                case EnemyState.Airborne: UpdateAirborne(dt); break;
            }
        }

        // --- States ---------------------------------------------------------------------------

        void UpdateIdle(float dt)
        {
            if (DistanceToTarget() <= AggroRadius)
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
            // Archers and casters keep their distance: step back while cooling down.
            if (data.IsRanged && dist < data.keepDistance && _cooldown > 0f)
            {
                Vector3 away = Flat(transform.position - target.position).normalized;
                MoveToward(transform.position + away * 3f, data.walkSpeed * 1.4f * _speedMult, dt);
                Face(target.position, dt);
            }
            else if (dist <= data.attackRange)
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
            else MoveToward(target.position, data.chaseSpeed * _speedMult, dt);
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
                if (data.IsRanged)
                {
                    if (!_fired) { _fired = true; FireShots(target); }
                    Stop(dt);
                }
                else
                {
                    _velocity.x = _attackDirection.x * data.attackLungeSpeed;
                    _velocity.z = _attackDirection.z * data.attackLungeSpeed;
                    _hits.Sweep(visual, data.attackHitboxOffset, data.attackHitboxSize, Strike);
                }
            }
            else Stop(dt);
            Move(dt);
            if (_stateTime >= activeEnd + data.attackRecovery) SetState(EnemyState.Chase);
        }

        // --- Aerial (launched / juggled) ----------------------------------------------------------

        /// <summary>Big bodies and bosses cannot be thrown.</summary>
        public bool CanBeLaunched => !_launchImmune && data.size < 1.6f;
        bool _launchImmune;
        const float JuggleFloat = 3.5f;
        const float JuggleGravity = 0.45f;

        void Launch(float speed)
        {
            if (State == EnemyState.Attack) SetState(EnemyState.Chase);
            ShowTelegraph(0f);
            _velocity.y = speed;
            SetState(EnemyState.Airborne);
        }

        void UpdateAirborne(float dt)
        {
            _velocity = MovementMath.ApproachHorizontal(_velocity, Vector3.zero, 4f, dt);
            // Recently hit: it hangs (the juggle); otherwise it falls normally.
            float g = _sinceHit < 0.6f ? _gravity * JuggleGravity : _gravity;
            _velocity.y -= g * dt;
            visual.localRotation = Quaternion.Euler(-35f, visual.localEulerAngles.y, 0f);
            if (_body.enabled) _body.Move(_velocity * dt);
            if (_stateTime > 0.15f && _body.isGrounded && _velocity.y <= 0f)
            {
                // Lands hard: knocked down for a moment.
                _velocity = Vector3.zero;
                SetState(EnemyState.Hurt);
            }
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
            if (!RespawnAtHome && _stateTime >= DeathTime) { Destroy(gameObject); return; }
            if (data.respawnDelay > 0f && _stateTime >= DeathTime + data.respawnDelay) Respawn();
        }

        void SetState(EnemyState next)
        {
            if (State == EnemyState.Attack)
            {
                ShowTelegraph(0f);
                AttackTokens.Release(this);
                _cooldown = data.attackCooldown * _cooldownMult;
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
                    _fired = false;
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

        bool _fired;

        /// <summary>Ranged attack: a fan of projectiles aimed where the hero stood at the end of the windup (dodgeable).</summary>
        void FireShots(Transform target)
        {
            Vector3 from = transform.position + Vector3.up * (1f * data.size) + Facing * 0.8f;
            Vector3 aim = target != null ? (target.position + Vector3.up - from) : Facing;
            aim.y = Mathf.Clamp(aim.y, -0.3f * aim.magnitude, 0.3f * aim.magnitude);
            for (int i = 0; i < data.shots; i++)
            {
                float yaw = data.shots == 1 ? 0f : Mathf.Lerp(-data.shotSpread, data.shotSpread, i / (float)(data.shots - 1));
                Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * aim;
                Projectile.Fire(from, dir, data.shotSpeed, data.attackRange * 1.6f, 0.35f, 0, Team.Enemy, data.shotColor, t =>
                {
                    DamageInfo info = DamageCalculator.Resolve(_damageable.Stats, t.Stats, data.attackMultiplier, _damageable.Stats.element, _rng);
                    info.stagger = data.attackStagger;
                    info.source = gameObject;
                    info.hitPosition = t.transform.position;
                    info.knockback = Flat(t.transform.position - transform.position).normalized * data.attackKnockback;
                    return info;
                }, (t, info) => DealtDamage?.Invoke(info.amount));
            }
        }

        void Strike(Damageable target)
        {
            // Blinded: half of its blows miss.
            if (_damageable.Status.Has(StatusType.Blind) && _rng.NextDouble() < 0.5) return;
            DamageInfo info = DamageCalculator.Resolve(_damageable.Stats, target.Stats, data.attackMultiplier,
                _damageable.Stats.element, _rng);
            info.stagger = data.attackStagger;
            info.launch = data.attackLaunch;
            info.source = gameObject;
            info.hitPosition = target.transform.position;
            Vector3 push = Flat(target.transform.position - transform.position);
            info.knockback = push.normalized * data.attackKnockback;
            if (target.Receive(info)) DealtDamage?.Invoke(info.amount);
        }

        void OnDamaged(DamageInfo info)
        {
            _lastHit = info;
            _sinceHit = 0f;
            _poise += info.stagger;
            _flash = FlashTime;
            _velocity += new Vector3(info.knockback.x, 0f, info.knockback.z);
            if (_damageable.IsDead) return;
            if (State == EnemyState.Airborne)
            {
                // Juggle: every hit keeps it up; a spike drives it down.
                _velocity.y = info.launch < 0f ? info.launch : Mathf.Max(_velocity.y, info.launch > 0f ? info.launch : JuggleFloat);
                return;
            }
            if (info.launch > 0f && CanBeLaunched)
            {
                _poise = 0f;
                Launch(info.launch);
                return;
            }
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
            EnemyEvents.RaiseKilled(data, _xp);
            Fallen.Add((data.id, Level, transform.position, Time.time));
            if (Fallen.Count > 32) Fallen.RemoveAt(0);
            DropLoot();
            if (State == EnemyState.Attack) SetState(EnemyState.Chase);
            SetState(EnemyState.Dead);
        }

        /// <summary>Gold and items fall where it died (Items.ItemActions.RollDrops).</summary>
        void DropLoot()
        {
            var inv = Items.PlayerInventory.Current;
            if (inv == null || !data.dropsLoot || _noLoot) return;
            Vector3 at = transform.position;
            if (_gold > 0) Items.WorldPickup.Spawn(at, new Items.ItemStack { itemId = "gold", count = _gold });
            // Each region's own crafting material.
            World.RegionData region = World.RegionController.Current;
            if (region != null && _rng.NextDouble() < 0.12 && inv.Database.Get("mat_" + region.id) != null)
                Items.WorldPickup.Spawn(at, new Items.ItemStack { itemId = "mat_" + region.id, count = 1 });
            foreach (Items.ItemStack drop in Items.ItemActions.RollDrops(Level, Elite != null && data.tier == Stats.EnemyTier.Normal ? Stats.EnemyTier.Elite : data.tier, inv.Database, inv.Rules, _rng))
                Items.WorldPickup.Spawn(at, drop);
        }

        void Respawn()
        {
            _body.enabled = false;
            transform.position = _home;
            _body.enabled = true;
            visual.rotation = _homeRotation;
            _velocity = Vector3.zero;
            _poise = 0f;
            _damageable.Status.ClearAll();
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
            Vector3 desired = to.magnitude > 0.2f ? to.normalized * speed * _damageable.Status.SpeedFactor : Vector3.zero;
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
