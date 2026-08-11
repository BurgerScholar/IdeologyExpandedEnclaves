namespace IdeologyExpandedEnclaves
{
    public static class EnclaveRelationshipUtility
    {
        public static bool IsLocallyHostile(PilgrimCamp camp)
        {
            return IsLocallyHostile(camp?.Data);
        }

        public static bool IsLocallyHostile(EnclaveData data)
        {
            return
                data != null &&
                data.ReputationTier == EnclaveReputationTier.Hostile;
        }
    }
}
