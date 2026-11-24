using System;
using System.Collections.Generic;
using UnityEngine;

namespace Aethvirse.Gates
{
    public enum MapShape { Winding, Spiral, Ring, Fork, Hub }
    public enum RoomShape { Rect, Cross, LShape, Round, Pillared }
    public enum RoomKind { Start, Normal, Treasure, Boss, Secret }

    /// <summary>
    /// Abstract Gate dungeons (owner: rooms and corridors, all different): rooms on a coarse grid joined by bent
    /// corridors, in one of five map shapes; room count grows with rank. Same seed → same dungeon. Pure, tested.
    /// </summary>
    public sealed class DungeonLayout
    {
        public const float Cell = 34f;

        public sealed class Room
        {
            public int index;
            public Vector2Int cell;
            public Vector3 Center => new Vector3(cell.x * Cell, 0f, cell.y * Cell);
            public Vector2 size;
            public RoomShape shape;
            public RoomKind kind;
        }

        public sealed class Corridor
        {
            public int from, to;
            public List<Vector3> points = new List<Vector3>();
            public bool hidden;
        }

        public readonly List<Room> rooms = new List<Room>();
        public readonly List<Corridor> corridors = new List<Corridor>();
        public MapShape shape;

        public Room Start => rooms[0];
        public Room Boss => rooms.Find(r => r.kind == RoomKind.Boss);

        public static int RoomCount(int rank) => 5 + Mathf.Clamp(rank, 0, 5) * 2;

        public static DungeonLayout Generate(int seed, int rank, bool withSecret)
        {
            var rng = new System.Random(seed);
            var d = new DungeonLayout { shape = (MapShape)rng.Next(5) };
            int count = RoomCount(rank);
            List<Vector2Int> cells = d.shape switch
            {
                MapShape.Spiral => Spiral(count),
                MapShape.Ring => Ring(count),
                MapShape.Fork => Fork(count, rng),
                MapShape.Hub => Hub(count),
                _ => Winding(count, rng),
            };
            // Shapes can produce fewer cells than asked (a ring closes early): fall back to a winding run.
            if (cells == null || cells.Count < count) { d.shape = MapShape.Winding; cells = Winding(count, rng); }
            for (int i = 0; i < cells.Count; i++)
            {
                d.rooms.Add(new Room
                {
                    index = i, cell = cells[i],
                    size = new Vector2(14f + rng.Next(10), 14f + rng.Next(10)),
                    shape = i == 0 ? RoomShape.Rect : (RoomShape)rng.Next(5),
                    kind = i == 0 ? RoomKind.Start : i == cells.Count - 1 ? RoomKind.Boss : RoomKind.Normal,
                });
            }
            Room boss = d.rooms[d.rooms.Count - 1];
            boss.size = new Vector2(26f, 26f);
            boss.shape = RoomShape.Round;
            if (d.rooms.Count > 3) d.rooms[1 + rng.Next(d.rooms.Count - 3)].kind = RoomKind.Treasure;
            d.Connect(rng);
            if (withSecret) d.AddSecret(rng);
            return d;
        }

        /// <summary>Every room joins the nearest earlier room on the grid (ties: the earliest), so branches and hubs connect.</summary>
        void Connect(System.Random rng)
        {
            for (int i = 1; i < rooms.Count; i++)
            {
                int best = 0, bestDist = int.MaxValue;
                for (int j = 0; j < i; j++)
                {
                    Vector2Int d = rooms[i].cell - rooms[j].cell;
                    int dist = Math.Abs(d.x) + Math.Abs(d.y);
                    if (dist < bestDist) { best = j; bestDist = dist; }
                }
                AddCorridor(best, i, rng, false);
            }
            if (shape == MapShape.Ring && rooms.Count > 4) AddCorridor(rooms.Count - 2, 0, rng, false);
        }

        void AddSecret(System.Random rng)
        {
            var used = new HashSet<Vector2Int>();
            foreach (Room r in rooms) used.Add(r.cell);
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
            for (int attempt = 0; attempt < 40; attempt++)
            {
                Room host = rooms[1 + rng.Next(Math.Max(1, rooms.Count - 2))];
                Vector2Int c = host.cell + dirs[rng.Next(4)];
                if (used.Contains(c)) continue;
                rooms.Add(new Room { index = rooms.Count, cell = c, size = new Vector2(12f, 12f), shape = RoomShape.Pillared, kind = RoomKind.Secret });
                AddCorridor(host.index, rooms.Count - 1, rng, true);
                return;
            }
        }

        void AddCorridor(int a, int b, System.Random rng, bool hidden)
        {
            Vector3 p = rooms[a].Center, q = rooms[b].Center;
            var c = new Corridor { from = a, to = b, hidden = hidden };
            c.points.Add(p);
            // Bend once (winding feel); straight when aligned.
            if (!Mathf.Approximately(p.x, q.x) && !Mathf.Approximately(p.z, q.z))
                c.points.Add(rng.NextDouble() < 0.5 ? new Vector3(q.x, 0f, p.z) : new Vector3(p.x, 0f, q.z));
            c.points.Add(q);
            corridors.Add(c);
        }

        // --- Map shapes (grid cells) ------------------------------------------------------------------

        static List<Vector2Int> Winding(int count, System.Random rng)
        {
            var cells = new List<Vector2Int> { Vector2Int.zero };
            var used = new HashSet<Vector2Int> { Vector2Int.zero };
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.right, Vector2Int.left, Vector2Int.down };
            int guard = 0;
            while (cells.Count < count && guard++ < 1000)
            {
                Vector2Int cur = cells[cells.Count - 1];
                var options = new List<Vector2Int>();
                foreach (Vector2Int d in dirs) if (!used.Contains(cur + d)) options.Add(cur + d);
                if (options.Count == 0) { cells.RemoveAt(cells.Count - 1); if (cells.Count == 0) cells.Add(Vector2Int.zero); continue; }
                // Bias forward (up) so runs travel away from the entrance.
                Vector2Int next = options[rng.Next(options.Count)];
                if (options.Contains(cur + Vector2Int.up) && rng.NextDouble() < 0.4) next = cur + Vector2Int.up;
                cells.Add(next);
                used.Add(next);
            }
            return cells;
        }

        static List<Vector2Int> Spiral(int count)
        {
            var cells = new List<Vector2Int>();
            Vector2Int p = Vector2Int.zero, dir = Vector2Int.up;
            int leg = 1, steps = 0, turns = 0;
            while (cells.Count < count)
            {
                cells.Add(p);
                p += dir;
                if (++steps == leg)
                {
                    steps = 0;
                    dir = new Vector2Int(dir.y, -dir.x);
                    if (++turns % 2 == 0) leg++;
                }
            }
            return cells;
        }

        static List<Vector2Int> Ring(int count)
        {
            var cells = new List<Vector2Int>();
            int side = Mathf.Max(2, (count - 1) / 4 + 1);
            for (int x = 0; x < side && cells.Count < count - 1; x++) cells.Add(new Vector2Int(x, 0));
            for (int y = 1; y <= side && cells.Count < count - 1; y++) cells.Add(new Vector2Int(side - 1, y));
            for (int x = side - 2; x >= 0 && cells.Count < count - 1; x--) cells.Add(new Vector2Int(x, side));
            for (int y = side - 1; y >= 1 && cells.Count < count - 1; y--) cells.Add(new Vector2Int(0, y));
            // The boss sits outside the ring, past its far corner.
            cells.Add(new Vector2Int(side, side + 1));
            return cells.Count >= count ? cells : null;
        }

        static List<Vector2Int> Fork(int count, System.Random rng)
        {
            var cells = new List<Vector2Int> { Vector2Int.zero, new Vector2Int(0, 1) };
            int arm = Mathf.Max(1, (count - 4) / 2);
            for (int i = 1; i <= arm && cells.Count < count - 2; i++) cells.Add(new Vector2Int(-1, 1 + i));
            for (int i = 1; i <= arm && cells.Count < count - 2; i++) cells.Add(new Vector2Int(1, 1 + i));
            cells.Add(new Vector2Int(0, arm + 2));
            while (cells.Count < count) cells.Add(new Vector2Int(0, cells[cells.Count - 1].y + 1));
            return cells;
        }

        static List<Vector2Int> Hub(int count)
        {
            var cells = new List<Vector2Int> { new Vector2Int(0, -1), Vector2Int.zero };
            Vector2Int[] arms = { Vector2Int.left, Vector2Int.up, Vector2Int.right };
            int n = 2;
            for (int depth = 1; cells.Count < count; depth++)
                foreach (Vector2Int a in arms)
                {
                    if (cells.Count >= count) break;
                    cells.Add(a * depth);
                    n++;
                }
            return cells;
        }
    }
}
