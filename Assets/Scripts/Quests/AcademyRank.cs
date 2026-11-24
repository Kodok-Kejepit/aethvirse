namespace Aethvirse.Quests
{
    /// <summary>Academy rank E … S = Guild rank outside (§10, STORY §6.3): unlocks regions and Gate ranks.</summary>
    public static class AcademyRank
    {
        public static readonly int[] Thresholds = { 0, 100, 300, 700, 1500, 3000 };
        public static readonly string[] Names = { "E", "D", "C", "B", "A", "S" };

        public static int For(int points)
        {
            int r = 0;
            for (int i = 0; i < Thresholds.Length; i++) if (points >= Thresholds[i]) r = i;
            return r;
        }

        public static string Name(int rank) => Names[System.Math.Clamp(rank, 0, Names.Length - 1)];
    }
}
