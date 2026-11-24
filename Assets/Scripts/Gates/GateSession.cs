namespace Aethvirse.Gates
{
    /// <summary>The Gate being run right now (read by the dungeon scene).</summary>
    public static class GateSession
    {
        public static OpenGate Current { get; private set; }
        public static int Level { get; private set; }
        public static string[] EnemyTypes { get; private set; } = new string[0];

        /// <summary>Job trial in progress (clearing the Gate grants this job).</summary>
        public static string TrialJob { get; private set; } = "";

        public static void BeginTrial(string jobId, int tier, int level, string[] enemyTypes)
        {
            Begin(new OpenGate { uid = -1, rank = UnityEngine.Mathf.Clamp(tier + 1, 1, 5), seed = jobId.GetHashCode() }, level, enemyTypes);
            TrialJob = jobId;
        }

        public static void Begin(OpenGate gate, int level, string[] enemyTypes)
        {
            TrialJob = "";
            Current = gate;
            Level = level;
            EnemyTypes = enemyTypes ?? new string[0];
        }

        public static void End()
        {
            Current = null;
            TrialJob = "";
        }
    }
}
