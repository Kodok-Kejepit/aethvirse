using UnityEngine;

namespace Aethvirse.Player
{
    /// <summary>Pure movement helpers (unit tested). Unity axes: +Z forward, +X right, +Y up.</summary>
    public static class MovementMath
    {
        // Ease in / ease out: rate 45 m/s² closes the gap at ~7.5/s (run speed in ~0.3 s).
        const float EaseDivisor = 6f;
        // Small constant step so the speed settles exactly instead of creeping forever.
        const float SettleShare = 0.15f;

        /// <summary>Turns a 2D move input (x = right, y = forward) into a world direction relative to the camera yaw.</summary>
        public static Vector3 CameraRelative(Vector2 input, float cameraYawDegrees)
        {
            Vector3 world = Quaternion.Euler(0f, cameraYawDegrees, 0f) * new Vector3(input.x, 0f, input.y);
            return Vector3.ClampMagnitude(world, 1f);
        }

        /// <summary>Moves horizontal velocity toward the target at <paramref name="rate"/> m/s², keeping vertical velocity.</summary>
        public static Vector3 ApproachHorizontal(Vector3 velocity, Vector3 target, float rate, float deltaTime)
        {
            var horizontal = new Vector3(velocity.x, 0f, velocity.z);
            var flatTarget = new Vector3(target.x, 0f, target.z);
            horizontal = Vector3.Lerp(horizontal, flatTarget, 1f - Mathf.Exp(-rate / EaseDivisor * deltaTime));
            horizontal = Vector3.MoveTowards(horizontal, flatTarget, rate * SettleShare * deltaTime);
            return new Vector3(horizontal.x, velocity.y, horizontal.z);
        }

        /// <summary>Ground speed for an input magnitude: walk below the threshold, run above, sprint when asked.</summary>
        public static float GroundSpeed(float magnitude, PlayerMovementData data, bool sprinting)
        {
            if (magnitude <= 0f) return 0f;
            if (sprinting) return data.sprintSpeed;
            return magnitude < data.walkThreshold ? data.walkSpeed : data.runSpeed;
        }

        /// <summary>Initial upward velocity that reaches <paramref name="height"/> under <paramref name="gravity"/>.</summary>
        public static float JumpVelocity(float height, float gravity) => Mathf.Sqrt(2f * gravity * height);

        /// <summary>Yaw in degrees that makes a transform's +Z face <paramref name="direction"/>.</summary>
        public static float YawForDirection(Vector3 direction) => Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        /// <summary>Dodge speed over time: eases from start to end speed across the duration.</summary>
        public static float DodgeSpeedAt(float elapsed, PlayerMovementData data)
        {
            float t = Mathf.Clamp01(elapsed / data.dodgeDuration);
            return Mathf.Lerp(data.dodgeSpeed, data.dodgeEndSpeed, t * t);
        }

        /// <summary>Frame-rate independent smoothing weight for a rate per second.</summary>
        public static float SmoothWeight(float rate, float deltaTime) => 1f - Mathf.Exp(-rate * deltaTime);
    }
}
