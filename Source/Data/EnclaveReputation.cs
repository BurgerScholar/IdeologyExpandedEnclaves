namespace IdeologyExpandedEnclaves
{
    public enum EnclaveReputationTier
    {
        Hostile,
        Distrusted,
        Neutral,
        Friendly,
        Trusted,
        Revered
    }

    public static class EnclaveReputation
    {
        public const int Minimum = -100;
        public const int Maximum = 100;
        public const int InitialValue = 0;

        public const int HostileMaximum = -51;
        public const int DistrustedMaximum = -11;
        public const int NeutralMaximum = 10;
        public const int FriendlyMaximum = 50;
        public const int TrustedMaximum = 80;

        public static int Clamp(long value)
        {
            if (value < Minimum)
            {
                return Minimum;
            }

            if (value > Maximum)
            {
                return Maximum;
            }

            return (int)value;
        }

        public static EnclaveReputationTier GetTier(int value)
        {
            int reputation = Clamp(value);

            if (reputation <= HostileMaximum)
            {
                return EnclaveReputationTier.Hostile;
            }

            if (reputation <= DistrustedMaximum)
            {
                return EnclaveReputationTier.Distrusted;
            }

            if (reputation <= NeutralMaximum)
            {
                return EnclaveReputationTier.Neutral;
            }

            if (reputation <= FriendlyMaximum)
            {
                return EnclaveReputationTier.Friendly;
            }

            if (reputation <= TrustedMaximum)
            {
                return EnclaveReputationTier.Trusted;
            }

            return EnclaveReputationTier.Revered;
        }

        public static string GetTierLabel(int value)
        {
            return GetTier(value).ToString();
        }
    }
}
