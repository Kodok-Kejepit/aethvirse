using Aethvirse.Combat;

namespace Aethvirse.Skills
{
    /// <summary>How a socketed rune changes a skill (pure and tested). A null rune changes nothing.</summary>
    public static class RuneMath
    {
        public const float ExecuteThreshold = 0.3f;

        public static float DamageFactor(SkillRuneData r, float targetHealthRatio) =>
            r == null ? 1f :
            r.effect == RuneEffect.Overcharge ? 1f + r.value :
            r.effect == RuneEffect.Executioner && targetHealthRatio <= ExecuteThreshold ? 1f + r.value : 1f;

        public static float MpFactor(SkillRuneData r) =>
            r == null ? 1f : r.effect == RuneEffect.Thrift ? 1f - r.value : r.effect == RuneEffect.Overcharge ? 1f + r.value2 : 1f;

        public static float CooldownFactor(SkillRuneData r) => r != null && r.effect == RuneEffect.Haste ? 1f - r.value : 1f;
        public static int ExtraProjectiles(SkillRuneData r) => r != null && r.effect == RuneEffect.Split ? (int)r.value : 0;
        public static int ExtraPierce(SkillRuneData r) => r != null && r.effect == RuneEffect.Pierce ? (int)r.value : 0;
        public static float EchoShare(SkillRuneData r) => r != null && r.effect == RuneEffect.Echo ? r.value : 0f;
        public static float LeechShare(SkillRuneData r) => r != null && r.effect == RuneEffect.Leech ? r.value : 0f;
        public static float CritChance(SkillRuneData r) => r != null && r.effect == RuneEffect.Keen ? r.value : 0f;
        public static float HitboxFactor(SkillRuneData r) => r != null && r.effect == RuneEffect.Vast ? 1f + r.value : 1f;
        public static float DurationFactor(SkillRuneData r) => r != null && r.effect == RuneEffect.Lingering ? 1f + r.value : 1f;
        public static float HealFactor(SkillRuneData r) => r != null && r.effect == RuneEffect.Mending ? 1f + r.value : 1f;
        public static float StaggerFactor(SkillRuneData r) => r != null && r.effect == RuneEffect.Thunderclap ? 1f + r.value : 1f;

        public static ElementType Element(SkillRuneData r, ElementType original) =>
            r != null && r.effect == RuneEffect.Infuse ? r.element : original;
    }
}
