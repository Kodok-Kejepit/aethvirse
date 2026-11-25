namespace Aethvirse.Endgame
{
    /// <summary>Boss phase by remaining health (pure, tested): 0 above 70 %, 1 above 40 %, 2 above 15 %, then 3.</summary>
    public static class BossPhases
    {
        public static int For(float healthRatio) =>
            healthRatio <= 0.15f ? 3 : healthRatio <= 0.4f ? 2 : healthRatio <= 0.7f ? 1 : 0;
    }
}
