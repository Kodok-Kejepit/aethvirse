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
