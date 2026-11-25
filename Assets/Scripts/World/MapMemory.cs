using System;
using System.Collections.Generic;
using UnityEngine;

namespace Aethvirse.World
{
    /// <summary>
    /// Fog of war (§12 map): which cells of each region you have seen. A grid over the 260 m region square, kept as
    /// a hex string per region in the save (pure and tested).
    /// </summary>
    [Serializable]
    public sealed class MapMemory
    {
        public const int Grid = 52;
        public const float Size = 260f;
        public const float CellSize = Size / Grid;

        public List<string> regions = new List<string>();
        public List<string> bits = new List<string>();

        readonly Dictionary<string, bool[]> _cache = new Dictionary<string, bool[]>();

        public static Vector2Int Cell(Vector3 world) =>
            new Vector2Int(Mathf.Clamp(Mathf.FloorToInt((world.x + Size * 0.5f) / CellSize), 0, Grid - 1),
                           Mathf.Clamp(Mathf.FloorToInt((world.z + Size * 0.5f) / CellSize), 0, Grid - 1));

        bool[] Cells(string region)
        {
            if (_cache.TryGetValue(region, out bool[] c)) return c;
            c = new bool[Grid * Grid];
            int i = regions.IndexOf(region);
            if (i >= 0) Unpack(bits[i], c);
            _cache[region] = c;
            return c;
        }

        public bool IsRevealed(string region, int x, int y) => x >= 0 && y >= 0 && x < Grid && y < Grid && Cells(region)[y * Grid + x];

        /// <summary>Reveals cells within <paramref name="radius"/> metres; returns how many were new.</summary>
        public int Reveal(string region, Vector3 at, float radius)
        {
            bool[] c = Cells(region);
            Vector2Int centre = Cell(at);
            int r = Mathf.CeilToInt(radius / CellSize), added = 0;
            for (int y = centre.y - r; y <= centre.y + r; y++)
                for (int x = centre.x - r; x <= centre.x + r; x++)
                {
                    if (x < 0 || y < 0 || x >= Grid || y >= Grid || (x - centre.x) * (x - centre.x) + (y - centre.y) * (y - centre.y) > r * r) continue;
                    if (c[y * Grid + x]) continue;
                    c[y * Grid + x] = true;
                    added++;
                }
            if (added > 0) Store(region, c);
            return added;
        }

        public float Explored(string region)
        {
            bool[] c = Cells(region);
            int n = 0;
            foreach (bool b in c) if (b) n++;
            return n / (float)c.Length;
        }

        void Store(string region, bool[] c)
        {
            int i = regions.IndexOf(region);
            string packed = Pack(c);
            if (i < 0) { regions.Add(region); bits.Add(packed); }
            else bits[i] = packed;
        }

        static string Pack(bool[] c)
        {
            var sb = new System.Text.StringBuilder(c.Length / 4 + 1);
            for (int i = 0; i < c.Length; i += 4)
            {
                int v = 0;
                for (int k = 0; k < 4 && i + k < c.Length; k++) if (c[i + k]) v |= 1 << k;
                sb.Append("0123456789abcdef"[v]);
            }
            return sb.ToString();
        }

        static void Unpack(string s, bool[] c)
        {
            for (int i = 0; i < s.Length; i++)
            {
                int v = "0123456789abcdef".IndexOf(s[i]);
                for (int k = 0; k < 4 && i * 4 + k < c.Length; k++) c[i * 4 + k] = v >= 0 && (v & (1 << k)) != 0;
            }
        }
    }
}
