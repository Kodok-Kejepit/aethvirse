using System;
using System.Collections.Generic;
using UnityEngine;

namespace Aethvirse.World
{
    public enum WeatherKind { Clear, Rain, Fog, Storm, Snow, Sandstorm, Ashfall }

    /// <summary>Weights per weather and how long a spell lasts (game minutes). One per region.</summary>
    [Serializable]
    public sealed class WeatherTable
    {
        [Serializable]
        public struct Entry
        {
            public WeatherKind kind;
            public float weight;
        }

        public List<Entry> entries = new List<Entry> { new Entry { kind = WeatherKind.Clear, weight = 1f } };
        public float minDuration = 60f;
        public float maxDuration = 240f;

        /// <summary>Weighted pick; slightly favours staying the same so weather does not flicker.</summary>
        public WeatherKind Pick(System.Random rng, WeatherKind current)
        {
            float total = 0f;
            foreach (Entry e in entries) total += e.weight * (e.kind == current ? 1.2f : 1f);
            if (total <= 0f) return WeatherKind.Clear;
            double roll = rng.NextDouble() * total;
            foreach (Entry e in entries)
            {
                roll -= e.weight * (e.kind == current ? 1.2f : 1f);
                if (roll <= 0) return e.kind;
            }
            return entries[entries.Count - 1].kind;
        }

        public bool Has(WeatherKind kind) => entries.Exists(e => e.kind == kind && e.weight > 0f);
    }

    /// <summary>Current weather and when it changes next (game minutes). Pure, saved.</summary>
    [Serializable]
    public sealed class WeatherState
    {
        public WeatherKind current = WeatherKind.Clear;
        public float changeAt;
        /// <summary>Cheat: weather never changes by itself.</summary>
        public bool locked;

        [NonSerialized] System.Random _rng = new System.Random();

        public bool IsWet => current == WeatherKind.Rain || current == WeatherKind.Storm;

        /// <summary>Returns true if the weather changed.</summary>
        public bool Update(float nowMinutes, WeatherTable table)
        {
            if (locked || nowMinutes < changeAt) return false;
            _rng ??= new System.Random();
            WeatherKind next = table.Pick(_rng, current);
            changeAt = nowMinutes + Mathf.Lerp(table.minDuration, table.maxDuration, (float)_rng.NextDouble());
            if (next == current) return false;
            current = next;
            return true;
        }

        public void Force(WeatherKind kind, float nowMinutes, float holdMinutes, bool lockIt)
        {
            current = kind;
            locked = lockIt;
            changeAt = nowMinutes + holdMinutes;
        }

        /// <summary>Entering a region without this weather resets it to clear.</summary>
        public bool FitTo(WeatherTable table, float nowMinutes)
        {
            if (table.Has(current) || current == WeatherKind.Clear) return false;
            current = WeatherKind.Clear;
            changeAt = nowMinutes + table.minDuration;
            return true;
        }
    }
}
