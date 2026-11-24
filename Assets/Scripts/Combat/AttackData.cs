using UnityEngine;

namespace Aethvirse.Combat
{
    /// <summary>How the weapon moves (graybox; real clips come with the hero model). Append only: assets store the number.</summary>
    public enum SwingKind { Horizontal, Vertical, Spin, Thrust, Backhand, Rising, Diagonal, DoubleSpin, Slam }

    /// <summary>One attack in a combo graph. Timings are seconds from the start of the attack.</summary>
    [CreateAssetMenu(menuName = "AETHVIRSE/Combat/Attack", fileName = "Attack")]
    public sealed class AttackData : ScriptableObject
    {
        public string id = "";
        public string displayName = "";
        public float damageMultiplier = 1f;
        public bool isHeavy;
        public ElementType element = ElementType.Physical;

        [Header("Timing")]
        public float startup = 0.1f;
        public float active = 0.12f;
        public float recovery = 0.25f;
        [Tooltip("After this time the next combo input is accepted (and buffered input fires).")]
        public float comboWindow = 0.22f;
        [Tooltip("After this time a dodge can cancel the attack.")]
        public float dodgeCancel = 0.2f;

        [Header("Hit")]
        [Tooltip("Box size in the attacker's local space (x right, y up, z forward).")]
        public Vector3 hitboxSize = new Vector3(2.2f, 1.4f, 1.8f);
        [Tooltip("Box centre in the attacker's local space (+z = in front).")]
        public Vector3 hitboxOffset = new Vector3(0f, 1f, 1.1f);
        public float knockback = 2f;
        public float stagger = 10f;
        public float hitStop = 0.05f;
        public float lungeSpeed = 3f;
        public SwingKind swing = SwingKind.Horizontal;

        [Header("Combo graph")]
        public AttackData nextLight;
        public AttackData nextHeavy;

        public float Total => startup + active + recovery;
    }
}
