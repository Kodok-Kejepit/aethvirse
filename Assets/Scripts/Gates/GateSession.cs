namespace Aethvirse.Gates
{
    /// <summary>The Gate being run right now (read by the dungeon scene).</summary>
    public static class GateSession
    {
        public static OpenGate Current { get; private set; }
        public static int Level { get; private set; }
        public static string[] EnemyTypes { get; private set; } = new string[0];

        public static void Begin(OpenGate gate, int level, string[] enemyTypes)
        {
            Current = gate;
            Level = level;
            EnemyTypes = enemyTypes ?? new string[0];
        }

        public static void End() => Current = null;
    }
}
