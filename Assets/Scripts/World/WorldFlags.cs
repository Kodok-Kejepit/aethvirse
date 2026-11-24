using System;
using System.Collections.Generic;

namespace Aethvirse.World
{
    /// <summary>Named world flags (story beats, opened doors, secrets) and counters. Saved as lists.</summary>
    [Serializable]
    public sealed class WorldFlags
    {
        public List<string> flags = new List<string>();
        public List<string> counterKeys = new List<string>();
        public List<int> counterValues = new List<int>();

        [NonSerialized] HashSet<string> _set;

        HashSet<string> Set => _set ??= new HashSet<string>(flags);

        public event Action<string> FlagSet;

        public bool Has(string flag) => !string.IsNullOrEmpty(flag) && Set.Contains(flag);

        public void SetFlag(string flag)
        {
            if (string.IsNullOrEmpty(flag) || !Set.Add(flag)) return;
            flags.Add(flag);
            FlagSet?.Invoke(flag);
        }

        public void Clear(string flag)
        {
            if (Set.Remove(flag)) flags.Remove(flag);
        }

        public int Counter(string key)
        {
            int i = counterKeys.IndexOf(key);
            return i >= 0 ? counterValues[i] : 0;
        }

        public int Add(string key, int amount = 1)
        {
            int i = counterKeys.IndexOf(key);
            if (i < 0)
            {
                counterKeys.Add(key);
                counterValues.Add(amount);
                return amount;
            }
            counterValues[i] += amount;
            return counterValues[i];
        }

        /// <summary>After JsonUtility loads the lists.</summary>
        public void Rebuild() => _set = new HashSet<string>(flags);
    }
}
