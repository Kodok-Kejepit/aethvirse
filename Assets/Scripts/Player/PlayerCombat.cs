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
        bool _skillStrikeSent;
        bool _fired;
        /// <summary>Set by the lock-on targeter: homing projectiles curve toward it.</summary>
        public Transform HomingTarget { get; set; }

        /// <summary>The next hit is a guaranteed crit with the counter bonus (perfect dodge, stealth).</summary>
        public void GrantCounter() => _counterTimer = counterWindow;

        /// <summary>Trap skills: a ground trap that bursts with this skill's damage.</summary>
        public void PlaceTrap(Skills.SkillData skill, int level, Vector3 at)
        {
            AttackData a = skill.attack;
            if (a == null) return;
            Skills.Trap.Place(at, skill.trapRadius, skill.trapLifetime, skill.iconColor, t => BuildHit(t, a, skill, level, null), OnLanded);
        }

        void FireProjectiles()
        {
            _fired = true;
            AttackData a = Current;
            // Freeze what this volley is: it may land after the attack ended.
            Skills.SkillData skill = CurrentSkill;
            int level = _currentSkillLevel;
            Skills.SkillComboData skillCombo = _currentCombo;
            Vector3 from = _frame.position + Vector3.up * 1.2f + _frame.forward * 0.8f;
            Skills.SkillRuneData rune = RuneOf(skill);
            int count = a.projectileCount + Skills.RuneMath.ExtraProjectiles(rune);
            float spread = count > a.projectileCount && a.projectileSpread < 8f ? 8f + count * 2f : a.projectileSpread;
            int pierce = a.projectilePierce + Skills.RuneMath.ExtraPierce(rune);
            Color color = rune != null && rune.effect == Skills.RuneEffect.Infuse ? Element.ColorOf(rune.element) : a.projectileColor;
            for (int i = 0; i < count; i++)
            {
                float offset = count > 1 ? Mathf.Lerp(-spread, spread, i / (count - 1f)) : 0f;
                Vector3 dir = Quaternion.Euler(0f, offset, 0f) * _frame.forward;
                Projectile.Fire(from, dir, a.projectileSpeed, a.projectileRange, a.projectileRadius, pierce, Team.Player,
                    color, t => BuildHit(t, a, skill, level, skillCombo), (t, info) => { OnLanded(t, info); AfterRuneHit(skill, t, info); },
                    a.homing ? HomingTarget : null);
            }
        }
        AttackData _lastAttack;
        Transform _frame;

        public ComboData Combo => combo;
        /// <summary>Skill of the current attack (null for normal attacks).</summary>
        public Skills.SkillData CurrentSkill { get; private set; }
        public Skills.SkillData PendingSkill { get; private set; }
        int _pendingSkillLevel = 1, _currentSkillLevel = 1;
        Skills.SkillComboData _pendingCombo, _currentCombo;
        /// <summary>Raised when a skill's strike window opens (for its look: rings, arcs, pillars).</summary>
        public event Action<Skills.SkillData> SkillStrike;
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
            _self = GetComponent<Damageable>();
            Tracker = new ComboTracker(comboTimeout);
            ResetWeaponPose();
        }

        /// <summary>The transform attacks are measured from (the facing visual).</summary>
        public void SetFrame(Transform frame) => _frame = frame;

        void Update()
        {
            // A click on a free cursor captures it; it does not attack. Touch mode never uses the mouse.
            bool mouseAttacks = _cursorWasLocked && !TouchControls.IsActive;
            // With LB held the face buttons cast skills; do not buffer them as attacks.
            bool modifier = _input.SkillModifier.IsPressed();
            if (!modifier && (_input.LightAttack.WasPressedThisFrame() || (mouseAttacks && _input.LightAttackMouse.WasPressedThisFrame())))
                _lightBuffer = inputBuffer;
            if (!modifier && (_input.HeavyAttack.WasPressedThisFrame() || (mouseAttacks && _input.HeavyAttackMouse.WasPressedThisFrame())))
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

        public void SetStats(CombatStats newStats) => stats = newStats;

        /// <summary>The class decides the weapon combo and the graybox weapon's shape.</summary>
        public void SetClass(Stats.ClassData c)
        {
            if (c.combo != null) combo = c.combo;
            if (weaponPivot == null) return;
            Transform blade = weaponPivot.Find("Blade");
            if (blade == null) return;
            (Vector3 scale, Vector3 pos) look = c.id switch
            {
                "phantom" => (new Vector3(0.05f, 0.08f, 0.5f), new Vector3(0f, 0f, 0.3f)),
                "arcanist" => (new Vector3(0.06f, 0.06f, 1.8f), new Vector3(0f, 0f, 0.4f)),
                "ranger" => (new Vector3(0.05f, 1.3f, 0.08f), new Vector3(0f, 0f, 0.25f)),
                _ => (new Vector3(0.08f, 0.16f, 1.3f), new Vector3(0f, 0f, 0.75f)),
            };
            blade.localScale = look.scale;
            blade.localPosition = look.pos;
        }
        /// <summary>Share of damage healed back (set by PlayerStats from gear).</summary>
        public float Lifesteal { get; set; }
        Damageable _self;

        /// <summary>A damaging skill becomes the next attack (SkillCaster).</summary>
        public void QueueSkill(Skills.SkillData skill, int level, Skills.SkillComboData skillCombo)
        {
            Pending = skill.attack;
            PendingSkill = skill;
            _pendingSkillLevel = level;
            _pendingCombo = skillCombo;
            _lightBuffer = _heavyBuffer = 0f;
        }

        /// <summary>Skills may cut a normal attack short (not another skill).</summary>
        public bool CanCancelIntoSkill => Current != null && CurrentSkill == null;

        /// <summary>Picks the next attack from the buffered input (heavy wins) into <see cref="Pending"/>.</summary>
        public bool ConsumeAttack()
        {
            if (!HasAttackInput || combo == null || _input.SkillModifier.IsPressed()) return false;
            bool heavy = _heavyBuffer > 0f;
            _lightBuffer = _heavyBuffer = 0f;
            AttackData previous = Current != null && CurrentSkill == null ? Current : (_chainTimer > 0f ? _lastAttack : null);
            Pending = combo.ResolveNext(previous, heavy);
            PendingSkill = null;
            _pendingCombo = null;
            return Pending != null;
        }

        public AttackData BeginAttack()
        {
            Current = Pending;
            CurrentSkill = PendingSkill;
            _currentSkillLevel = _pendingSkillLevel;
            _currentCombo = _pendingCombo;
            Pending = null;
            PendingSkill = null;
            _pendingCombo = null;
            _hits.Begin();
            _skillStrikeSent = false;
            _fired = false;
            return Current;
        }

        /// <summary>Checks the hitbox this frame (call only during the active window).</summary>
        public void SweepHitbox()
        {
            if (Current == null || _frame == null) return;
            if (CurrentSkill != null && !_skillStrikeSent)
            {
                _skillStrikeSent = true;
                SkillStrike?.Invoke(CurrentSkill);
            }
            if (Current.ranged)
            {
                if (!_fired) FireProjectiles();
                return;
            }
            _hits.Sweep(_frame, Current.hitboxOffset, Current.hitboxSize * Skills.RuneMath.HitboxFactor(RuneOf(CurrentSkill)), Strike);
        }

        public void EndAttack()
        {
            // Skills do not continue the weapon combo chain.
            _lastAttack = CurrentSkill == null ? Current : null;
            _chainTimer = combo != null ? combo.chainGrace : 0f;
            Current = null;
            CurrentSkill = null;
            _currentCombo = null;
            ResetWeaponPose();
        }

        void Strike(Damageable target)
        {
            DamageInfo info = BuildHit(target, Current, CurrentSkill, _currentSkillLevel, _currentCombo);
            if (target.Receive(info))
            {
                OnLanded(target, info);
                AfterRuneHit(CurrentSkill, target, info);
            }
        }

        DamageInfo BuildHit(Damageable target, AttackData a, Skills.SkillData skill, int skillLevel, Skills.SkillComboData skillCombo)
        {
            bool counter = CounterReady;
            float multiplier = skill != null ? skill.MultiplierAt(skillLevel) : a.damageMultiplier;
            float bonus = counter ? counterBonus : 1f;
            bool forceCrit = counter;
            float extraStagger = 0f;
            if (skillCombo != null)
            {
                bonus *= skillCombo.bonusMultiplier;
                forceCrit |= skillCombo.forceCrit;
                extraStagger = skillCombo.extraStagger;
            }
            Skills.SkillRuneData rune = RuneOf(skill);
            multiplier *= Skills.RuneMath.DamageFactor(rune, target.Health.Ratio);
            forceCrit |= _rng.NextDouble() < Skills.RuneMath.CritChance(rune);
            DamageInfo info = DamageCalculator.Resolve(stats, target.Stats, multiplier, Skills.RuneMath.Element(rune, a.element), _rng,
                forceCrit: forceCrit, bonusMult: bonus);
            info.isHeavy = a.isHeavy;
            info.stagger = (a.stagger + extraStagger) * Skills.RuneMath.StaggerFactor(rune);
            info.hitStop = a.hitStop;
            info.source = gameObject;
            info.hitPosition = target.transform.position;
            Vector3 push = target.transform.position - transform.position;
            push.y = 0f;
            info.knockback = push.normalized * a.knockback;
            return info;
        }

        static Skills.SkillRuneData RuneOf(Skills.SkillData skill)
        {
            var ps = global::Aethvirse.Stats.PlayerStats.Current;
            return skill != null && ps != null ? ps.RuneFor(skill) : null;
        }

        /// <summary>Rune after-effects of a landed skill hit: leech and the echoing second hit.</summary>
        void AfterRuneHit(Skills.SkillData skill, Damageable target, DamageInfo info)
        {
            Skills.SkillRuneData rune = RuneOf(skill);
            if (rune == null) return;
            float leech = Skills.RuneMath.LeechShare(rune);
            if (leech > 0f && _self != null) _self.Health.Heal(Mathf.Max(1, Mathf.RoundToInt(info.amount * leech)));
            float echo = Skills.RuneMath.EchoShare(rune);
            if (echo > 0f) StartCoroutine(Echo(target, info, echo));
        }

        System.Collections.IEnumerator Echo(Damageable target, DamageInfo first, float share)
        {
            yield return new WaitForSeconds(0.22f);
            if (target == null || target.IsDead) yield break;
            var info = new DamageInfo
            {
                amount = Mathf.Max(1, Mathf.RoundToInt(first.amount * share)), element = first.element, isCrit = first.isCrit,
                source = gameObject, hitPosition = target.transform.position, stagger = first.stagger * 0.5f,
            };
            if (target.Receive(info)) DamageNumbers.Spawn(target.transform.position + Vector3.up * 2.1f, info.amount, info.isCrit, info.element, false);
        }

        void OnLanded(Damageable target, DamageInfo info)
        {
            if (Lifesteal > 0f && _self != null) _self.Health.Heal(Mathf.Max(1, Mathf.RoundToInt(info.amount * Lifesteal)));
            if (CounterReady) _counterTimer = 0f;
            Tracker.RegisterHit(Now);
            ComboChanged?.Invoke(Tracker.Count, Tracker.Rank(Now));
            DamageNumbers.Spawn(target.transform.position + Vector3.up * 1.8f, info.amount, info.isCrit, info.element, false);
            TimeFx.HitStop(info.hitStop);
            Core.Sfx.Hit();
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
