using UnityEngine;

namespace Aethvirse.Player
{
    /// <summary>Movement tuning for the player. Values match the Godot prototype's default_movement.</summary>
    [CreateAssetMenu(menuName = "AETHVIRSE/Player Movement Data", fileName = "PlayerMovementData")]
    public sealed class PlayerMovementData : ScriptableObject
    {
        [Header("Ground")]
        public float walkSpeed = 2.2f;
        public float runSpeed = 5.5f;
        public float sprintSpeed = 8.5f;
        [Tooltip("Analog stick magnitude below this walks, above it runs (keyboard always runs).")]
        [Range(0f, 1f)] public float walkThreshold = 0.55f;
        public float acceleration = 45f;
        public float deceleration = 55f;
        [Tooltip("How fast the body turns toward the move direction (exponential rate per second).")]
        public float turnSpeed = 14f;

        [Header("Air")]
        public float jumpHeight = 1.4f;
        public float fallGravityMultiplier = 1.8f;
        [Tooltip("Releasing jump while rising multiplies vertical speed by this (variable jump height).")]
        [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f;
        [Range(0f, 1f)] public float airControl = 0.35f;
        public float maxFallSpeed = 40f;
        public float coyoteTime = 0.12f;

        [Header("Dodge")]
        public float dodgeSpeed = 13f;
        public float dodgeEndSpeed = 4f;
        public float dodgeDuration = 0.38f;
        public float dodgeIFrames = 0.3f;
        [Tooltip("Fraction of the dodge after which a jump or a new dodge may cancel it.")]
        [Range(0f, 1f)] public float dodgeCancelRatio = 0.6f;
        public float dodgeStaminaCost = 20f;
        [Tooltip("An air dash barely falls: gravity is scaled by this.")]
        [Range(0f, 1f)] public float airDodgeGravityScale = 0.25f;

        [Header("Input")]
        public float inputBuffer = 0.2f;
    }
}
