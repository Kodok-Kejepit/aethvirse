namespace Aethvirse.Combat
{
    public enum ReactionType
    {
        None, Melt, Firestorm, Magma, Overload, Blizzard, Permafrost, Shatter, Superconduct, Sandstorm, Thunderstorm,
        Magnetize, Eclipse, Corrode, Purge, Erase,
    }

    /// <summary>
    /// Elemental reactions (§6.1, pure and tested): an element hitting a target that carries another element's aura
    /// triggers a reaction and clears the aura. Heavy physical / earth blows shatter a frozen target.
    /// </summary>
    public static class Reactions
    {
        public static ReactionType Resolve(ElementType aura, ElementType incoming, bool frozen, bool heavy)
        {
            if (frozen && heavy && (incoming == ElementType.Physical || incoming == ElementType.Earth)) return ReactionType.Shatter;
            if (aura == ElementType.Physical || incoming == ElementType.Physical || aura == incoming) return ReactionType.None;
            if (incoming == ElementType.Void || aura == ElementType.Void) return ReactionType.Erase;
            if (Pair(aura, incoming, ElementType.Shadow, ElementType.Light)) return ReactionType.Eclipse;
            if (aura == ElementType.Shadow || incoming == ElementType.Shadow) return ReactionType.Corrode;
            if (aura == ElementType.Light || incoming == ElementType.Light) return ReactionType.Purge;
            if (Pair(aura, incoming, ElementType.Fire, ElementType.Ice)) return ReactionType.Melt;
            if (Pair(aura, incoming, ElementType.Fire, ElementType.Wind)) return ReactionType.Firestorm;
            if (Pair(aura, incoming, ElementType.Fire, ElementType.Earth)) return ReactionType.Magma;
            if (Pair(aura, incoming, ElementType.Fire, ElementType.Lightning)) return ReactionType.Overload;
            if (Pair(aura, incoming, ElementType.Ice, ElementType.Wind)) return ReactionType.Blizzard;
            if (Pair(aura, incoming, ElementType.Ice, ElementType.Earth)) return ReactionType.Permafrost;
            if (Pair(aura, incoming, ElementType.Ice, ElementType.Lightning)) return ReactionType.Superconduct;
            if (Pair(aura, incoming, ElementType.Wind, ElementType.Earth)) return ReactionType.Sandstorm;
            if (Pair(aura, incoming, ElementType.Wind, ElementType.Lightning)) return ReactionType.Thunderstorm;
            if (Pair(aura, incoming, ElementType.Earth, ElementType.Lightning)) return ReactionType.Magnetize;
            return ReactionType.None;
        }

        /// <summary>Damage multiplier of the triggering hit.</summary>
        public static float Multiplier(ReactionType r) => r switch
        {
            ReactionType.Melt => 2f,
            ReactionType.Shatter => 2.5f,
            ReactionType.Overload => 1.5f,
            ReactionType.Eclipse => 1.6f,
            ReactionType.Thunderstorm => 1.3f,
            ReactionType.Firestorm => 1.3f,
            ReactionType.Magma => 1.2f,
            ReactionType.None => 1f,
            _ => 1.15f,
        };

        /// <summary>Reactions that also hit others nearby (radius in metres; 0 = single target).</summary>
        public static float SplashRadius(ReactionType r) => r switch
        {
            ReactionType.Firestorm => 5f,
            ReactionType.Overload => 4f,
            ReactionType.Thunderstorm => 7f,
            ReactionType.Magnetize => 6f,
            ReactionType.Blizzard => 4f,
            ReactionType.Sandstorm => 4f,
            _ => 0f,
        };

        public static string Name(ReactionType r) => r.ToString().ToUpperInvariant();

        static bool Pair(ElementType a, ElementType b, ElementType x, ElementType y) => (a == x && b == y) || (a == y && b == x);
    }
}
