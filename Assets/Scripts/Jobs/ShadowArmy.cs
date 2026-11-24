using System;
using System.Collections.Generic;

namespace Aethvirse.Jobs
{
    /// <summary>Shadow soldiers bound from fallen enemies (shadow job line). Pure and saved; ranks rise with their kills.</summary>
    [Serializable]
    public sealed class ShadowArmy
    {
        public static readonly string[] RankNames = { "Soldier", "Knight", "Elite", "Commander", "Marshal" };
        public static readonly int[] RankXp = { 0, 20, 60, 150, 400 };
        public static readonly float[] RankPower = { 1f, 1.3f, 1.65f, 2f, 2.4f };

        [Serializable]
        public sealed class Soldier
        {
            public int id;
            public string enemyType = "";
            public int level = 1;
            public int xp;
            public int rank;
        }

        public List<Soldier> soldiers = new List<Soldier>();
        public int nextId = 1;

        public Soldier Bind(string enemyType, int level)
        {
            var s = new Soldier { id = nextId++, enemyType = enemyType, level = level };
            soldiers.Add(s);
            return s;
        }

        /// <summary>XP from the soldier's own kills (1 + 4 × enemy tier). Returns true on a rank-up.</summary>
        public static bool GainXp(Soldier s, int amount)
        {
            s.xp += amount;
            int before = s.rank;
            while (s.rank < RankXp.Length - 1 && s.xp >= RankXp[s.rank + 1]) s.rank++;
            return s.rank > before;
        }

        public Soldier Get(int id) => soldiers.Find(x => x.id == id);
    }
}
