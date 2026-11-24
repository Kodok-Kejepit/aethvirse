using System;
using System.Collections.Generic;

namespace Aethvirse.Gates
{
    /// <summary>One open Gate in the world (saved).</summary>
    [Serializable]
    public sealed class OpenGate
    {
        public int uid;
        public string region = "";
        public int spot;
        /// <summary>0 = E … 5 = S.</summary>
        public int rank;
        public bool red;
        public bool doubleGate;
        public float openedAt;
        public bool broken;
        public int seed;
    }

    /// <summary>
    /// Dynamic Gates (§7, Godot WorldGates): every 2 game hours a region may open a Gate on a free spot (max 2);
    /// a Gate left for a game day breaks (Dungeon Break). Pure and saved.
    /// </summary>
    [Serializable]
    public sealed class WorldGates
    {
        public const float RollEveryMinutes = 120f;
        public const float BreakAfterMinutes = 1440f;
        public const int MaxPerRegion = 2;
        public const float OpenChance = 0.5f;
        public static readonly string[] RankNames = { "E", "D", "C", "B", "A", "S" };

        public List<OpenGate> gates = new List<OpenGate>();
        public List<string> rollRegions = new List<string>();
        public List<float> rollTimes = new List<float>();
        public int nextUid = 1;

        public IEnumerable<OpenGate> In(string region)
        {
            foreach (OpenGate g in gates) if (g.region == region) yield return g;
        }

        public OpenGate At(string region, int spot) => gates.Find(g => g.region == region && g.spot == spot);
        public OpenGate Get(int uid) => gates.Find(g => g.uid == uid);

        /// <summary>Rolls for a new Gate if a roll is due. <paramref name="regionTier"/> 0…6 pushes ranks up in later regions.</summary>
        public OpenGate Roll(string region, int spotCount, int regionTier, float now, System.Random rng)
        {
            int i = rollRegions.IndexOf(region);
            if (i < 0) { rollRegions.Add(region); rollTimes.Add(now); return null; }
            if (now - rollTimes[i] < RollEveryMinutes) return null;
            rollTimes[i] = now;
            int open = 0;
            foreach (OpenGate _ in In(region)) open++;
            if (open >= MaxPerRegion || rng.NextDouble() >= OpenChance) return null;
            var free = new List<int>();
            for (int s = 0; s < spotCount; s++) if (At(region, s) == null) free.Add(s);
            if (free.Count == 0) return null;
            int rank = Math.Min(5, Math.Max(0, regionTier + (rng.NextDouble() < 0.25 ? 1 : 0) - (rng.NextDouble() < 0.2 ? 1 : 0)));
            var g = new OpenGate
            {
                uid = nextUid++, region = region, spot = free[rng.Next(free.Count)], rank = rank, openedAt = now,
                red = rng.NextDouble() < 0.1, doubleGate = rng.NextDouble() < 0.08, seed = rng.Next(),
            };
            gates.Add(g);
            return g;
        }

        /// <summary>Gates that just broke (left alone too long).</summary>
        public List<OpenGate> CheckBreaks(float now)
        {
            var broke = new List<OpenGate>();
            foreach (OpenGate g in gates)
            {
                if (g.broken || now - g.openedAt < BreakAfterMinutes) continue;
                g.broken = true;
                broke.Add(g);
            }
            return broke;
        }

        public void Close(int uid) => gates.RemoveAll(g => g.uid == uid);
    }
}
