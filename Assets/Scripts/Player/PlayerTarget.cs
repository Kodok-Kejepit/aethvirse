using UnityEngine;

namespace Aethvirse.Player
{
    /// <summary>What enemies chase: the living player (null while dead or absent).</summary>
    public static class PlayerTarget
    {
        public static Transform Current { get; set; }

        /// <summary>Stealth: until this time enemies lose track of the player (attacking ends it early).</summary>
        public static float HiddenUntil { get; set; }
        public static bool Hidden => Time.time < HiddenUntil;

        /// <summary>What enemies may chase right now.</summary>
        public static Transform Visible => Hidden ? null : Current;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearOnPlay()
        {
            Current = null;
            HiddenUntil = 0f;
        }
    }
}
