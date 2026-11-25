using UnityEngine;

namespace Aethvirse.Combat
{
    /// <summary>Entry points of a weapon's combo graph (see AttackData.nextLight / nextHeavy).</summary>
    [CreateAssetMenu(menuName = "AETHVIRSE/Combat/Combo", fileName = "Combo")]
    public sealed class ComboData : ScriptableObject
    {
        public AttackData lightStart;
        public AttackData heavyStart;
        [Tooltip("Time after an attack ends during which the chain continues.")]
        public float chainGrace = 0.35f;
        [Header("Aerial (§6: launcher → juggle → slam)")]
        [Tooltip("Heavy from neutral while locked on: throws the target up.")]
        public AttackData launcher;
        public AttackData airLightStart;
        [Tooltip("Heavy in the air: plunge.")]
        public AttackData airHeavyStart;

        /// <summary>Next attack in the air: continues an air chain, or starts one (light) / plunges (heavy).</summary>
        public AttackData ResolveAir(AttackData previous, bool heavy)
        {
            if (heavy) return airHeavyStart;
            if (previous != null && previous.hover && previous.nextLight != null) return previous.nextLight;
            return previous != null && previous.hover ? null : airLightStart;
        }

        /// <summary>Next attack given the attack that just ran (null = from neutral) and the pressed button.</summary>
        public AttackData ResolveNext(AttackData previous, bool heavy)
        {
            if (previous != null)
            {
                AttackData chained = heavy ? previous.nextHeavy : previous.nextLight;
                if (chained != null) return chained;
            }
            return heavy ? heavyStart : lightStart;
        }
    }
}
