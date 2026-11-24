using UnityEngine;

namespace Aethvirse.Combat
{
    /// <summary>One resolved hit, passed from hitbox to hurtbox to health.</summary>
    public sealed class DamageInfo
    {
        public int amount;
        public bool isCrit;
        public bool isHeavy;
        public ElementType element = ElementType.Physical;
        public Vector3 knockback;
        /// <summary>Upward speed on the target (heavy blows send it flying).</summary>
        public float launch;
        /// <summary>Poise damage; enough of it staggers the target.</summary>
        public float stagger;
        public float hitStop;
        public Vector3 hitPosition;
        public GameObject source;
        /// <summary>True damage ignores DEF (e.g. the Eclipse reaction, later).</summary>
        public bool ignoreDefense;
    }
}
