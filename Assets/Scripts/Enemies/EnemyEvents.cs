using System;
using UnityEngine;

namespace Aethvirse.Enemies
{
    /// <summary>World-wide enemy events (XP intake now; quests and the Codex hook in later).</summary>
    public static class EnemyEvents
    {
        /// <summary>An enemy died; argument = XP reward.</summary>
        public static event Action<int> Killed;
        /// <summary>An enemy died; which type (quests, evolving weapons, journal).</summary>
        public static event Action<EnemyData> KilledEnemy;

        public static void RaiseKilled(EnemyData data, int xp)
        {
            Killed?.Invoke(xp);
            KilledEnemy?.Invoke(data);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearOnPlay()
        {
            Killed = null;
            KilledEnemy = null;
        }
    }
}
