using Aethvirse.Combat;
using UnityEngine;

namespace Aethvirse.Player
{
    // Attack state (port of the Godot AttackState): runs one AttackData — lunge during startup,
    // hitbox during active, recovery. Chains into the next attack from buffered input after the
    // combo window; a dodge cancels after dodgeCancel.
    public sealed partial class PlayerController
    {
        // Share of run speed you keep steering while attacking (light / heavy).
        const float AttackSteer = 0.45f;
        const float HeavySteer = 0.2f;
        // Soft aim assist: an enemy inside this cone / range of the aim pulls the attack to it.
        const float AimAssistDegrees = 28f;
        const float AimAssistRange = 9f;

        AttackData _attack;
        float _attackElapsed;
        Vector3 _attackDirection;

        public AttackData CurrentAttack => _attack;

        bool ConsumeAttack() => combat != null && combat.ConsumeAttack();

        /// <summary>True when a skill may start now: free states, the late part of a dash, or during a normal attack.</summary>
        public bool CanCastSkill => State switch
        {
            PlayerState.Idle or PlayerState.Move or PlayerState.Air => true,
            PlayerState.Dodge => DodgeCancelOpen,
            PlayerState.Attack => combat != null && combat.CanCancelIntoSkill,
            _ => false,
        };

        /// <summary>Starts the attack queued by SkillCaster (restarts the Attack state if already attacking).</summary>
        public void StartQueuedSkill() => SetState(PlayerState.Attack);

        const float SlamSpeed = 30f;
        // Share of gravity while an air attack runs (the juggle hang).
        const float HoverGravity = 0.12f;
        bool _slamLanded;

        void StartAttack()
        {
            _slamLanded = false;
            _attack = combat.BeginAttack();
            if (combat.CurrentSkill == null) Core.GameEvents.RaiseAttacked();
            // Attacking breaks stealth.
            PlayerTarget.HiddenUntil = 0f;
            _attackElapsed = 0f;
            IsSprinting = false;
            _attackDirection = SnapFacing(AimDirection());
        }

        void ExitAttack()
        {
            if (combat != null) combat.EndAttack();
            _attack = null;
        }

        void UpdateAttack(float dt)
        {
            AttackData a = _attack;
            if (a == null)
            {
                SetState(PlayerState.Idle);
                return;
            }
            // Attack speed (AGI) scales normal attacks; skills keep their authored timing.
            _attackElapsed += combat.CurrentSkill == null ? dt * AttackSpeed : dt;
            float strikeEnd = a.startup + a.active;
            float lunge = _attackElapsed < strikeEnd ? a.lungeSpeed * (1f - _attackElapsed / strikeEnd) : 0f;
            // You can keep walking while attacking (less with heavies).
            Vector3 steer = InputDirection * (movement.runSpeed * SpeedMultiplier * (a.isHeavy ? HeavySteer : AttackSteer));
            _velocity.x = _attackDirection.x * lunge + steer.x;
            _velocity.z = _attackDirection.z * lunge + steer.z;
            if (a.slam && _attackElapsed >= a.startup && !_slamLanded)
            {
                // Plunge: dive and hold the strike open until the ground.
                _velocity.x *= 0.3f;
                _velocity.z *= 0.3f;
                _velocity.y = -SlamSpeed;
                if (!Grounded) _attackElapsed = Mathf.Min(_attackElapsed, strikeEnd - 0.001f);
            }
            else if ((a.hover || a.slam) && !Grounded) _velocity.y = Mathf.Max(_velocity.y - _gravity * HoverGravity * dt, -1.5f);
            else ApplyGravity(dt);
            Move(dt);
            if (a.slam && !_slamLanded && _attackElapsed >= a.startup && Grounded)
            {
                _slamLanded = true;
                combat.SlamImpact();
            }

            if (_attackElapsed >= a.startup && _attackElapsed < strikeEnd) combat.SweepHitbox();
            combat.UpdateSwing(a, _attackElapsed / strikeEnd);

            if (_attackElapsed >= a.dodgeCancel && ConsumeDodge()) { SetState(PlayerState.Dodge); return; }
            if (_attackElapsed >= a.comboWindow && Grounded && ConsumeJump()) { StartJump(); SetState(PlayerState.Air); return; }
            if (_attackElapsed >= a.comboWindow && ConsumeAttack()) { SetState(PlayerState.Attack); return; }
            if (_attackElapsed >= a.Total)
                SetState(!Grounded ? PlayerState.Air : HasMoveInput ? PlayerState.Move : PlayerState.Idle);
        }

        /// <summary>Where attacks go: the camera's view (flattened), pulled toward a nearby enemy.</summary>
        Vector3 AimDirection()
        {
            Vector3 dir = cameraRig != null ? cameraRig.transform.forward : Facing;
            dir.y = 0f;
            if (dir.sqrMagnitude < MinDirectionSqr) dir = Facing;
            return Assist(dir.normalized);
        }

        Vector3 Assist(Vector3 dir)
        {
            ILockable best = null;
            float bestScore = float.PositiveInfinity;
            float cosLimit = Mathf.Cos(AimAssistDegrees * Mathf.Deg2Rad);
            foreach (ILockable e in LockTargets.All)
            {
                if (e == null || (e is Object o && o == null) || !e.IsLockable) continue;
                Vector3 to = e.transform.position - transform.position;
                to.y = 0f;
                float d = to.magnitude;
                if (d < 0.1f || d > AimAssistRange) continue;
                float c = Vector3.Dot(to / d, dir);
                if (c < cosLimit) continue;
                float score = d * (2f - c);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = e;
                }
            }
            if (best == null) return dir;
            Vector3 aim = best.transform.position - transform.position;
            aim.y = 0f;
            return aim.normalized;
        }

        /// <summary>Faces the lock-on target if any, else <paramref name="fallback"/> (instantly).</summary>
        Vector3 SnapFacing(Vector3 fallback)
        {
            Vector3 direction = fallback;
            ILockable target = lockOn != null ? lockOn.Target : null;
            if (target != null && !(target is Object o && o == null)) direction = target.transform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < MinDirectionSqr) direction = Facing;
            direction.Normalize();
            visual.rotation = Quaternion.Euler(0f, MovementMath.YawForDirection(direction), 0f);
            return direction;
        }
    }
}
