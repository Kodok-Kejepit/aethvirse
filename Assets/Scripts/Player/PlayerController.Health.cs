using Aethvirse.Combat;
using UnityEngine;

namespace Aethvirse.Player
{
    // Getting hit, staggered and killed (port of the Godot Hurt / Dead states and fall damage).
    public sealed partial class PlayerController
    {
        [Header("Health")]
        [Tooltip("Hits with at least this much stagger interrupt the player (Hurt state).")]
        [SerializeField] float poiseThreshold = 15f;
        [SerializeField] float hurtTime = 0.45f;
        [SerializeField] float respawnDelay = 2f;
        [Range(0f, 1f)] [SerializeField] float fallDamageShare = 0.1f;

        Vector3 _spawnPoint;
        DamageInfo _lastHit;

        public bool IsDead => State == PlayerState.Dead;

        void SetupHealth()
        {
            _spawnPoint = transform.position;
            PlayerTarget.Current = transform;
            if (damageable == null) return;
            damageable.Health.Damaged += OnDamaged;
            damageable.Health.Died += () => SetState(PlayerState.Dead);
        }

        void OnDamaged(DamageInfo info)
        {
            _lastHit = info;
            if (combat != null) combat.BreakCombo();
            DamageNumbers.Spawn(transform.position + Vector3.up * 2f, info.amount, info.isCrit, info.element, true);
            if (damageable.IsDead) return;
            // Heavy attacks have armour against normal hits; a throw (launch) breaks through it.
            bool armored = State == PlayerState.Attack && _attack != null && _attack.isHeavy;
            if (info.launch > 0f || (info.stagger >= poiseThreshold && !armored)) SetState(PlayerState.Hurt);
        }

        void EnterHurt()
        {
            IsSprinting = false;
            if (_lastHit == null) return;
            _velocity.x = _lastHit.knockback.x;
            _velocity.z = _lastHit.knockback.z;
            if (_lastHit.launch > 0f) _velocity.y = _lastHit.launch;
        }

        void UpdateHurt(float dt)
        {
            _velocity = MovementMath.ApproachHorizontal(_velocity, Vector3.zero, 10f, dt);
            ApplyGravity(dt);
            Move(dt);
            // A dash breaks out of a stagger (stamina permitting).
            if (_stateElapsed > 0.15f && ConsumeDodge()) { SetState(PlayerState.Dodge); return; }
            if (_stateElapsed >= hurtTime && Grounded) SetState(HasMoveInput ? PlayerState.Move : PlayerState.Idle);
        }

        void EnterDead()
        {
            IsSprinting = false;
            PlayerTarget.Current = null;
            if (lockOn != null) lockOn.SetTarget(null);
        }

        void UpdateDead(float dt)
        {
            _velocity = MovementMath.ApproachHorizontal(_velocity, Vector3.zero, 10f, dt);
            ApplyGravity(dt);
            Move(dt);
            visual.localRotation = Quaternion.Euler(Mathf.Lerp(0f, -80f, Mathf.Clamp01(_stateElapsed / 0.4f)), visual.localEulerAngles.y, 0f);
            if (_stateElapsed >= respawnDelay) Respawn();
        }

        void Respawn()
        {
            visual.localRotation = Quaternion.Euler(0f, visual.localEulerAngles.y, 0f);
            Teleport(_spawnPoint);
            damageable.Health.Reset();
            stamina.Reset();
            PlayerTarget.Current = transform;
            SetState(PlayerState.Idle);
        }

        /// <summary>Falling out of the world costs a share of max HP (never kills).</summary>
        void ApplyFallDamage()
        {
            if (damageable == null || damageable.IsDead) return;
            int amount = Mathf.Max(1, Mathf.RoundToInt(damageable.Health.maxHealth * fallDamageShare));
            amount = Mathf.Min(amount, damageable.Health.Current - 1);
            if (amount > 0) damageable.Receive(new DamageInfo { amount = amount });
        }
    }
}
