namespace Aethvirse.Endgame
{
    /// <summary>Which ending the throne gives (pure, tested): B for a great shadow army, C for a full journal of the known world, else A.</summary>
    public static class Endings
    {
        public const int ShadowsForB = 20;
        public const int SecretsForC = 30;
        public const string LastSealFlag = "last_seal_opened";

        public static char Pick(int shadows, int secretsFound) =>
            secretsFound >= SecretsForC ? 'C' : shadows >= ShadowsForB ? 'B' : 'A';

        /// <summary>Hollow Throne → [ ??? ]: the three keys are the Frostveil, Archive and Fracture journal pages.</summary>
        public static readonly string[] Keys = { "journal_r4_frostveil_full", "journal_r5_archive_full", "journal_r6_fracture_full" };
    }
}
