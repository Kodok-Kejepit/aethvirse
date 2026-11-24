using UnityEngine;

namespace Aethvirse.Combat
{
    public enum ElementType { Physical, Fire, Ice, Wind, Earth, Lightning, Shadow, Light, Void }

    /// <summary>
    /// The 9 elements and their damage multipliers (master prompt §6.1).
    /// Natural cycle: Fire &gt; Ice &gt; Wind &gt; Earth &gt; Lightning &gt; Fire. Shadow ⇄ Light 1.5× both ways.
    /// Same element resists (0.75×) except Physical. Void deals 1.25× to all, 0.5× to Void.
    /// </summary>
    public static class Element
    {
        public const float Strong = 1.5f;
        public const float Weak = 0.75f;
        public const float ResistSame = 0.75f;
        public const float Neutral = 1f;
        public const float VoidVsAll = 1.25f;
        public const float VoidVsVoid = 0.5f;

        static readonly ElementType[] NaturalCycle =
            { ElementType.Fire, ElementType.Ice, ElementType.Wind, ElementType.Earth, ElementType.Lightning };

        public static bool IsNatural(ElementType e) => System.Array.IndexOf(NaturalCycle, e) >= 0;

        /// <summary>Multiplier for an attack of <paramref name="attack"/> against a target with <paramref name="defense"/> affinity.</summary>
        public static float Multiplier(ElementType attack, ElementType defense)
        {
            if (attack == ElementType.Void) return defense == ElementType.Void ? VoidVsVoid : VoidVsAll;
            if (attack == ElementType.Physical || defense == ElementType.Physical || defense == ElementType.Void) return Neutral;
            if (attack == defense) return ResistSame;
            if ((attack == ElementType.Shadow && defense == ElementType.Light) ||
                (attack == ElementType.Light && defense == ElementType.Shadow)) return Strong;
            if (IsNatural(attack) && IsNatural(defense))
            {
                int a = System.Array.IndexOf(NaturalCycle, attack);
                int d = System.Array.IndexOf(NaturalCycle, defense);
                int n = NaturalCycle.Length;
                if ((a + 1) % n == d) return Strong;
                if ((d + 1) % n == a) return Weak;
            }
            return Neutral;
        }

        public static Color ColorOf(ElementType e) => e switch
        {
            ElementType.Fire => new Color(1f, 0.55f, 0.15f),
            ElementType.Ice => new Color(0.55f, 0.85f, 1f),
            ElementType.Wind => new Color(0.6f, 1f, 0.7f),
            ElementType.Earth => new Color(0.75f, 0.55f, 0.3f),
            ElementType.Lightning => new Color(1f, 0.92f, 0.25f),
            ElementType.Shadow => new Color(0.65f, 0.35f, 0.95f),
            ElementType.Light => new Color(1f, 0.85f, 0.45f),
            ElementType.Void => new Color(0.15f, 0.15f, 0.15f),
            _ => new Color(0.92f, 0.92f, 0.95f),
        };
    }
}
