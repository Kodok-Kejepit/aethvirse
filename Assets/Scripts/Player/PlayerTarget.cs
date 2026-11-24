using UnityEngine;

namespace Aethvirse.Player
{
    /// <summary>What enemies chase: the living player (null while dead or absent).</summary>
    public static class PlayerTarget
    {
        public static Transform Current { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearOnPlay() => Current = null;
    }
}
