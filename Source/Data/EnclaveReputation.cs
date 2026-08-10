namespace IdeologyExpandedEnclaves
{
    public enum EnclaveReputationTier
    {
        Hostile,
        Wary,
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

        public const int HostileMaximum = -26;
        public const int WaryMaximum = -1;
        public const int NeutralMaximum = 24;
        public const int FriendlyMaximum = 49;
        public const int TrustedMaximum = 74;

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

            if (reputation <= WaryMaximum)
            {
                return EnclaveReputationTier.Wary;
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
