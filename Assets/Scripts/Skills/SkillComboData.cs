using System;

namespace Aethvirse.Skills
{
    /// <summary>Skill chain (§6): using <see cref="second"/> within <see cref="window"/> s after <see cref="first"/> gives a bonus.</summary>
    [Serializable]
    public sealed class SkillComboData
    {
        public string id = "";
        public string displayName = "";
        public string first = "";
        public string second = "";
        public float window = 2f;
        public float bonusMultiplier = 1f;
        public bool forceCrit;
        public float extraStagger;

        public bool Matches(string previous, float elapsed, string next) =>
            previous == first && next == second && elapsed <= window;
    }
}
