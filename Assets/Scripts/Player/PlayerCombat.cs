using System;
using Aethvirse.Combat;
using Aethvirse.Input;
using Aethvirse.Input.Touch;
using Unity.Cinemachine;
using UnityEngine;

namespace Aethvirse.Player
{
    /// <summary>
    /// Player-side combat (port of the Godot PlayerCombat): attack input buffers, combo chaining,
    /// damage building, hit feedback (hit-stop, numbers, shake), perfect dodge → counter, combo rank.
    /// Movement and the attack's timeline live in <see cref="PlayerController"/>.
    /// </summary>
    public sealed class PlayerCombat : MonoBehaviour
    {
        [SerializeField] ComboData combo;
        [SerializeField] CombatStats stats = new CombatStats { attack = 24f, defense = 6f, critChance = 0.15f, critDamage = 1.6f };
        [Tooltip("Graybox weapon pivot (child of the visual); swings per attack.")]
        [SerializeField] Transform weaponPivot;
        [SerializeField] CinemachineImpulseSource impulse;

        [Header("Tuning")]
        [SerializeField] float inputBuffer = 0.25f;
        [SerializeField] float perfectDodgeWindow = 0.15f;
        [SerializeField] float perfectDodgeTimeScale = 0.25f;
        [SerializeField] float perfectDodgeSlowMo = 0.5f;
        [SerializeField] float counterWindow = 1.5f;
        [SerializeField] float counterBonus = 1.5f;
        [SerializeField] float comboTimeout = 3f;
        [SerializeField] float heavyShake = 0.35f;

        static readonly Quaternion RestPose = Quaternion.Euler(35f, 30f, 0f);

        GameInput _input;
        HitQuery _hits;
        readonly System.Random _rng = new System.Random();
        float _lightBuffer;
        float _heavyBuffer;
        float _chainTimer;
        float _counterTimer;
        float _dodgeStartedAt = float.NegativeInfinity;
        bool _cursorWasLocked;
        AttackData _lastAttack;
        Transform _frame;

        public ComboData Combo => combo;
        public CombatStats Stats => stats;
        public AttackData Current { get; private set; }
        public AttackData Pending { get; private set; }
        public ComboTracker Tracker { get; private set; }
        public bool CounterReady => _counterTimer > 0f;

        /// <summary>(hit count, rank) whenever the combo changes.</summary>
        public event Action<int, ComboRank> ComboChanged;
        public event Action PerfectDodge;

        static float Now => Time.unscaledTime;

        void Awake()
        {
            _input = GameInput.Instance;
            _hits = new HitQuery(gameObject, Team.Player);
            Tracker = new ComboTracker(comboTimeout);
            ResetWeaponPose();
        }

        /// <summary>The transform attacks are measured from (the facing visual).</summary>
        public void SetFrame(Transform frame) => _frame = frame;

        void Update()
        {
            // A click on a free cursor captures it; it does not attack. Touch mode never uses the mouse.
            bool mouseAttacks = _cursorWasLocked && !TouchControls.IsActive;
            if (_input.LightAttack.WasPressedThisFrame() || (mouseAttacks && _input.LightAttackMouse.WasPressedThisFrame()))
                _lightBuffer = inputBuffer;
            if (_input.HeavyAttack.WasPressedThisFrame() || (mouseAttacks && _input.HeavyAttackMouse.WasPressedThisFrame()))
                _heavyBuffer = inputBuffer;
            _cursorWasLocked = Cursor.lockState == CursorLockMode.Locked;

            float dt = Time.deltaTime;
            _lightBuffer = Mathf.Max(_lightBuffer - dt, 0f);
            _heavyBuffer = Mathf.Max(_heavyBuffer - dt, 0f);
            _chainTimer = Mathf.Max(_chainTimer - dt, 0f);
            _counterTimer = Mathf.Max(_counterTimer - dt, 0f);
            if (Tracker.Count > 0 && !Tracker.IsActive(Now)) BreakCombo();
        }

        public bool HasAttackInput => _lightBuffer > 0f || _heavyBuffer > 0f;

        /// <summary>Picks the next attack from the buffered input (heavy wins) into <see cref="Pending"/>.</summary>
        public bool ConsumeAttack()
        {
            if (!HasAttackInput || combo == null) return false;
            bool heavy = _heavyBuffer > 0f;
            _lightBuffer = _heavyBuffer = 0f;
            AttackData previous = Current != null ? Current : (_chainTimer > 0f ? _lastAttack : null);
            Pending = combo.ResolveNext(previous, heavy);
            return Pending != null;
        }

        public AttackData BeginAttack()
        {
            Current = Pending;
            Pending = null;
            _hits.Begin();
            return Current;
        }

        /// <summary>Checks the hitbox this frame (call only during the active window).</summary>
        public void SweepHitbox()
        {
            if (Current == null || _frame == null) return;
            _hits.Sweep(_frame, Current.hitboxOffset, Current.hitboxSize, Strike);
        }

        public void EndAttack()
        {
            _lastAttack = Current;
            _chainTimer = combo != null ? combo.chainGrace : 0f;
            Current = null;
            ResetWeaponPose();
        }

        void Strike(Damageable target)
        {
            AttackData a = Current;
            bool counter = CounterReady;
            DamageInfo info = DamageCalculator.Resolve(stats, target.Stats, a.damageMultiplier, a.element, _rng,
                forceCrit: counter, bonusMult: counter ? counterBonus : 1f);
            info.isHeavy = a.isHeavy;
            info.stagger = a.stagger;
            info.hitStop = a.hitStop;
            info.source = gameObject;
            info.hitPosition = target.transform.position;
            Vector3 push = target.transform.position - transform.position;
            push.y = 0f;
            info.knockback = push.normalized * a.knockback;

            if (!target.Receive(info)) return;
            if (counter) _counterTimer = 0f;
            Tracker.RegisterHit(Now);
            ComboChanged?.Invoke(Tracker.Count, Tracker.Rank(Now));
            DamageNumbers.Spawn(target.transform.position + Vector3.up * 1.8f, info.amount, info.isCrit, info.element, false);
            TimeFx.HitStop(info.hitStop);
            if (info.isHeavy && impulse != null) impulse.GenerateImpulse(heavyShake);
        }

        // --- Perfect dodge ----------------------------------------------------------------------

        public void NotifyDodgeStarted() => _dodgeStartedAt = Now;

        /// <summary>A hit landed while invulnerable: inside the first moments of a dodge it is a perfect dodge.</summary>
        public void OnDodgedHit()
        {
            if (Now - _dodgeStartedAt > perfectDodgeWindow) return;
            _counterTimer = counterWindow;
            TimeFx.SlowMotion(perfectDodgeTimeScale, perfectDodgeSlowMo);
            PerfectDodge?.Invoke();
        }

        public void BreakCombo()
        {
            Tracker.Break();
            ComboChanged?.Invoke(0, ComboRank.None);
        }

        // --- Graybox weapon -----------------------------------------------------------------------

        /// <summary><paramref name="t"/> goes 0..1 over startup + active.</summary>
        public void UpdateSwing(AttackData a, float t)
        {
            if (weaponPivot == null) return;
            float k = Mathf.Clamp01(t);
            weaponPivot.localPosition = Vector3.zero;
            switch (a.swing)
            {
                case SwingKind.Horizontal: // right → left
                    weaponPivot.localRotation = Quaternion.Euler(0f, Mathf.Lerp(100f, -100f, k), 0f);
                    break;
                case SwingKind.Backhand: // left → right
                    weaponPivot.localRotation = Quaternion.Euler(0f, Mathf.Lerp(-100f, 100f, k), 0f);
                    break;
                case SwingKind.Vertical: // overhead chop
                    weaponPivot.localRotation = Quaternion.Euler(Mathf.Lerp(-80f, 70f, k), 0f, 0f);
                    break;
                case SwingKind.Rising: // low → high
                    weaponPivot.localRotation = Quaternion.Euler(Mathf.Lerp(70f, -85f, k), 0f, 0f);
                    break;
                case SwingKind.Diagonal: // upper right → lower left
                    weaponPivot.localRotation = Quaternion.Euler(Mathf.Lerp(-60f, 60f, k), Mathf.Lerp(80f, -80f, k), 0f);
                    break;
                case SwingKind.Spin:
                    weaponPivot.localRotation = Quaternion.Euler(0f, Mathf.Lerp(0f, -360f, k), 0f);
                    break;
                case SwingKind.DoubleSpin:
                    weaponPivot.localRotation = Quaternion.Euler(0f, Mathf.Lerp(0f, -720f, k), 0f);
                    break;
                case SwingKind.Slam: // high, then driven into the ground
                    weaponPivot.localRotation = Quaternion.Euler(Mathf.Lerp(-90f, 100f, k * k), 0f, 0f);
                    weaponPivot.localPosition = new Vector3(0f, Mathf.Lerp(0.2f, -0.5f, k * k), 0f);
                    break;
                case SwingKind.Thrust:
                    weaponPivot.localRotation = Quaternion.identity;
                    weaponPivot.localPosition = new Vector3(0f, 0f, Mathf.Lerp(-0.6f, 0.8f, k));
                    break;
            }
        }

        void ResetWeaponPose()
        {
            if (weaponPivot == null) return;
            weaponPivot.localRotation = RestPose;
            weaponPivot.localPosition = Vector3.zero;
        }
    }
}
