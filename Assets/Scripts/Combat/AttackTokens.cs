using System.Collections.Generic;

namespace Aethvirse.Combat
{
    /// <summary>Limits how many enemies may attack the player at the same time (fairness, §6).</summary>
    public static class AttackTokens
    {
        public static int MaxTokens = 2;
        static readonly List<object> _holders = new List<object>();

        public static int ActiveCount => _holders.Count;

        public static bool Request(object holder)
        {
            _holders.RemoveAll(h => h == null || (h is UnityEngine.Object o && o == null));
            if (_holders.Contains(holder)) return true;
            if (_holders.Count >= MaxTokens) return false;
            _holders.Add(holder);
            return true;
        }

        public static void Release(object holder) => _holders.Remove(holder);

        public static void Clear() => _holders.Clear();

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearOnPlay() => Clear();
    }
}
