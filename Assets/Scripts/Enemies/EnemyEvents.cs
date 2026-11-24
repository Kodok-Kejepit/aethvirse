using System;
using UnityEngine;

namespace Aethvirse.Enemies
{
    /// <summary>World-wide enemy events (XP intake now; quests and the Codex hook in later).</summary>
    public static class EnemyEvents
    {
        /// <summary>An enemy died; argument = XP reward.</summary>
        public static event Action<int> Killed;

        public static void RaiseKilled(int xp) => Killed?.Invoke(xp);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearOnPlay() => Killed = null;
    }
}
