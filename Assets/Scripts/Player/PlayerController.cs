using Aethvirse.Combat;
using Aethvirse.Input;
using UnityEngine;

namespace Aethvirse.Player
{
    public enum PlayerState { Idle, Move, Air, Dodge, Attack }

    /// <summary>
    /// Third-person player body (port of the Godot Player + Idle/Move/Air/Dodge states):
    /// camera-relative walk/run/sprint, buffered + coyote jump with variable height, and a dash
    /// with i-frames (once in mid-air) whose late part can be cancelled. Attacks: PlayerController.Attack.cs.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed partial class PlayerController : MonoBehaviour
    {
        [SerializeField] PlayerMovementData movement;
        [Tooltip("Turns to face the move direction. Defaults to this transform.")]
        [SerializeField] Transform visual;
        [Tooltip("Gives the camera yaw for camera-relative movement.")]
        [SerializeField] Cameras.PlayerCameraRig cameraRig;
        [SerializeField] StaminaPool stamina = new StaminaPool();
        [SerializeField] PlayerCombat combat;
        [SerializeField] LockOnTargeter lockOn;
        [SerializeField] Damageable damageable;
        [Tooltip("Below this height the player fell out of the world: back to the last safe ground.")]
        [SerializeField] float fallLimitY = -30f;

        const float SafeSampleInterval = 0.5f;
        const float GroundedStickVelocity = -2f;
        const float MinDirectionSqr = 0.0001f;

        CharacterController _body;
        GameInput _input;
        Vector3 _velocity;
        float _gravity;

        float _jumpBuffer;
        float _dodgeBuffer;
        float _coyote;
        bool _jumpCutDone;
        float _carriedSpeed;
        bool _airDashUsed;

        float _dodgeElapsed;
        Vector3 _dodgeDirection;
        bool _dodgeAirborne;

        float _safeTimer;
        Vector3 _lastSafePosition;

        public PlayerState State { get; private set; } = PlayerState.Idle;
        public Vector3 InputDirection { get; private set; }
        public float InputMagnitude { get; private set; }
        public bool IsSprinting { get; private set; }
        bool _invulnerable;
        public bool IsInvulnerable
        {
            get => _invulnerable;
            private set
            {
                _invulnerable = value;
                if (damageable != null) damageable.Health.Invulnerable = value;
            }
        }
        public bool DodgeCancelOpen { get; private set; }
        public StaminaPool Stamina => stamina;
        public Vector3 Velocity => _velocity;
        public float HorizontalSpeed => new Vector2(_velocity.x, _velocity.z).magnitude;
        public Vector3 Facing => visual.forward;
        bool Grounded => _body.isGrounded;
        bool HasMoveInput => InputMagnitude > 0f;

        void Awake()
        {
            _body = GetComponent<CharacterController>();
            if (movement == null) movement = ScriptableObject.CreateInstance<PlayerMovementData>();
            if (visual == null) visual = transform;
            _gravity = Mathf.Abs(Physics.gravity.y);
            _input = GameInput.Instance;
            stamina.Reset();
            _lastSafePosition = transform.position;
            if (combat == null) combat = GetComponent<PlayerCombat>();
            if (lockOn == null) lockOn = GetComponent<LockOnTargeter>();
            if (damageable == null) damageable = GetComponent<Damageable>();
            if (combat != null) combat.SetFrame(visual);
            if (damageable != null && combat != null) damageable.Health.Dodged += _ => combat.OnDodgedHit();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            ReadInput();
            TickTimers(dt);
            stamina.Tick(dt);
            switch (State)
            {
                case PlayerState.Idle: UpdateGround(dt, moving: false); break;
                case PlayerState.Move: UpdateGround(dt, moving: true); break;
                case PlayerState.Air: UpdateAir(dt); break;
                case PlayerState.Dodge: UpdateDodge(dt); break;
                case PlayerState.Attack: UpdateAttack(dt); break;
            }
            TrackSafeGround(dt);
            if (cameraRig != null) cameraRig.FovBoost = IsSprinting;
        }

        void ReadInput()
        {
            Vector2 raw = _input.Move.ReadValue<Vector2>();
            float yaw = cameraRig != null ? cameraRig.Yaw : 0f;
            InputDirection = MovementMath.CameraRelative(raw, yaw);
            InputMagnitude = InputDirection.magnitude;
            if (_input.Jump.WasPressedThisFrame()) _jumpBuffer = movement.inputBuffer;
            if (_input.Dodge.WasPressedThisFrame()) _dodgeBuffer = movement.inputBuffer;
        }

        void TickTimers(float dt)
        {
            _jumpBuffer = Mathf.Max(_jumpBuffer - dt, 0f);
            _dodgeBuffer = Mathf.Max(_dodgeBuffer - dt, 0f);
            _coyote = Grounded ? movement.coyoteTime : Mathf.Max(_coyote - dt, 0f);
            if (Grounded) _airDashUsed = false;
        }

        // --- States -------------------------------------------------------------------------

        void UpdateGround(float dt, bool moving)
        {
            // Sprint = hold while moving; unlimited (stamina is only spent on dashes).
            IsSprinting = moving && HasMoveInput && _input.Sprint.IsPressed();
            float speed = moving ? MovementMath.GroundSpeed(InputMagnitude, movement, IsSprinting) : 0f;
            ApplyHorizontal(speed, dt, 1f);
            ApplyGravity(dt);
            if (moving) FaceDirection(InputDirection, dt);
            Move(dt);

            if (TryGroundTransitions()) return;
            if (!moving && HasMoveInput) SetState(PlayerState.Move);
            else if (moving && !HasMoveInput && HorizontalSpeed < 0.1f) SetState(PlayerState.Idle);
        }

        bool TryGroundTransitions()
        {
            if (!Grounded && _coyote <= 0f) { SetState(PlayerState.Air); return true; }
            if (ConsumeJump()) { StartJump(); SetState(PlayerState.Air); return true; }
            if (ConsumeDodge()) { SetState(PlayerState.Dodge); return true; }
            if (ConsumeAttack()) { SetState(PlayerState.Attack); return true; }
            return false;
        }

        void UpdateAir(float dt)
        {
            if (!_jumpCutDone && _velocity.y > 0f && !_input.Jump.IsPressed())
            {
                _velocity.y *= movement.jumpCutMultiplier;
                _jumpCutDone = true;
            }
            if (ConsumeJump()) StartJump();
            if (ConsumeAttack())
            {
                SetState(PlayerState.Attack);
                return;
            }
            if (!_airDashUsed && ConsumeDodge())
            {
                _airDashUsed = true;
                SetState(PlayerState.Dodge);
                return;
            }
            ApplyHorizontal(_carriedSpeed, dt, movement.airControl);
            ApplyGravity(dt);
            FaceDirection(InputDirection, dt);
            Move(dt);
            if (Grounded && _velocity.y <= 0f) SetState(HasMoveInput ? PlayerState.Move : PlayerState.Idle);
        }

        void UpdateDodge(float dt)
        {
            _dodgeElapsed += dt;
            if (_dodgeElapsed >= movement.dodgeIFrames) IsInvulnerable = false;
            float speed = MovementMath.DodgeSpeedAt(_dodgeElapsed, movement);
            _velocity.x = _dodgeDirection.x * speed;
            _velocity.z = _dodgeDirection.z * speed;
            if (_dodgeAirborne) _velocity.y -= _gravity * movement.airDodgeGravityScale * dt;
            else ApplyGravity(dt);
            Move(dt);

            DodgeCancelOpen = _dodgeElapsed >= movement.dodgeDuration * movement.dodgeCancelRatio;
            if (DodgeCancelOpen && TryCancelDodge()) return;
            if (_dodgeElapsed >= movement.dodgeDuration)
            {
                if (!Grounded) SetState(PlayerState.Air);
                else SetState(HasMoveInput ? PlayerState.Move : PlayerState.Idle);
            }
        }

        bool TryCancelDodge()
        {
            if (Grounded && ConsumeJump()) { StartJump(); SetState(PlayerState.Air); return true; }
            if (ConsumeDodge()) { SetState(PlayerState.Dodge); return true; }
            // Dash attack: the swing comes straight out of the dash.
            if (ConsumeAttack()) { SetState(PlayerState.Attack); return true; }
            return false;
        }

        void SetState(PlayerState next)
        {
            ExitState(State);
            PlayerState previous = State;
            State = next;
            EnterState(next, previous);
        }

        void ExitState(PlayerState state)
        {
            if (state == PlayerState.Dodge)
            {
                IsInvulnerable = false;
                DodgeCancelOpen = false;
            }
            if (state == PlayerState.Move) IsSprinting = false;
            if (state == PlayerState.Attack) ExitAttack();
        }

        void EnterState(PlayerState state, PlayerState previous)
        {
            switch (state)
            {
                case PlayerState.Idle:
                    IsSprinting = false;
                    break;
                case PlayerState.Air:
                    _jumpCutDone = false;
                    // Keep sprint/run momentum through the jump instead of snapping to run speed.
                    _carriedSpeed = Mathf.Max(HorizontalSpeed, movement.runSpeed);
                    IsSprinting = false;
                    break;
                case PlayerState.Dodge:
                    StartDodge();
                    break;
                case PlayerState.Attack:
                    StartAttack();
                    break;
            }
        }

        void StartDodge()
        {
            _dodgeElapsed = 0f;
            _dodgeAirborne = !Grounded;
            if (_dodgeAirborne) _velocity.y = Mathf.Max(_velocity.y, 0f) * 0.3f;
            stamina.TrySpend(movement.dodgeStaminaCost);
            _dodgeDirection = HasMoveInput ? InputDirection : Facing;
            _dodgeDirection.y = 0f;
            _dodgeDirection = _dodgeDirection.sqrMagnitude > MinDirectionSqr ? _dodgeDirection.normalized : transform.forward;
            visual.rotation = Quaternion.Euler(0f, MovementMath.YawForDirection(_dodgeDirection), 0f);
            IsInvulnerable = true;
            IsSprinting = false;
            if (combat != null) combat.NotifyDodgeStarted();
        }

        // --- Shared helpers -----------------------------------------------------------------

        bool ConsumeJump()
        {
            if (_jumpBuffer <= 0f || _coyote <= 0f) return false;
            _jumpBuffer = 0f;
            _coyote = 0f;
            return true;
        }

        bool ConsumeDodge()
        {
            if (_dodgeBuffer <= 0f || !stamina.CanSpend) return false;
            _dodgeBuffer = 0f;
            return true;
        }

        void StartJump() => _velocity.y = MovementMath.JumpVelocity(movement.jumpHeight, _gravity);

        void ApplyGravity(float dt)
        {
            if (Grounded && _velocity.y <= 0f && State != PlayerState.Air)
            {
                // Keep the controller pressed to the ground so slopes and steps stay grounded.
                _velocity.y = GroundedStickVelocity;
                return;
            }
            float multiplier = _velocity.y < 0f ? movement.fallGravityMultiplier : 1f;
            _velocity.y = Mathf.Max(_velocity.y - _gravity * multiplier * dt, -movement.maxFallSpeed);
        }

        void ApplyHorizontal(float targetSpeed, float dt, float control)
        {
            Vector3 target = HasMoveInput ? InputDirection.normalized * targetSpeed : Vector3.zero;
            float rate = HasMoveInput ? movement.acceleration : movement.deceleration;
            _velocity = MovementMath.ApproachHorizontal(_velocity, target, rate * control, dt);
        }

        void FaceDirection(Vector3 direction, float dt)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < MinDirectionSqr) return;
            Quaternion target = Quaternion.Euler(0f, MovementMath.YawForDirection(direction), 0f);
            visual.rotation = Quaternion.Slerp(visual.rotation, target, MovementMath.SmoothWeight(movement.turnSpeed, dt));
        }

        void Move(float dt)
        {
            CollisionFlags flags = _body.Move(_velocity * dt);
            if ((flags & CollisionFlags.Above) != 0 && _velocity.y > 0f) _velocity.y = 0f;
        }

        void TrackSafeGround(float dt)
        {
            if (transform.position.y < fallLimitY)
            {
                RecoverFromFall();
                return;
            }
            _safeTimer += dt;
            if (Grounded && _safeTimer >= SafeSampleInterval)
            {
                _safeTimer = 0f;
                _lastSafePosition = transform.position;
            }
        }

        /// <summary>Fell out of the world: back to the last safe ground. (HP cost comes with the health system in M2.)</summary>
        public void RecoverFromFall()
        {
            Teleport(_lastSafePosition + Vector3.up * 0.5f);
            SetState(PlayerState.Idle);
        }

        public void Teleport(Vector3 position)
        {
            _body.enabled = false;
            transform.position = position;
            _body.enabled = true;
            _velocity = Vector3.zero;
        }

        /// <summary>Shown by the debug overlay.</summary>
        public string DebugState =>
            $"{State}{(IsInvulnerable ? " i-frame" : "")} | {HorizontalSpeed:0.0} m/s | stamina {Mathf.RoundToInt(stamina.Current)}";
    }
}
