using RimWorld.Planet;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveInfluenceUtility
    {
        public const int StrongDistanceWeight = 3;
        public const int ModerateDistanceWeight = 2;
        public const int WeakDistanceWeight = 1;

        public const int HostileReputationWeight = -3;
        public const int WaryReputationWeight = -1;
        public const int NeutralReputationWeight = 0;
        public const int FriendlyReputationWeight = 1;
        public const int TrustedReputationWeight = 2;
        public const int ReveredReputationWeight = 3;

        public const int FriendlySettlementWeight = 1;
        public const int NeutralSettlementWeight = 0;
        public const int HostileSettlementWeight = -1;

        public static EnclaveInfluenceScore CalculateInfluence(
            PilgrimCamp source,
            WorldObject neighbor,
            EnclaveNeighborType neighborType,
            EnclaveDistanceBand distanceBand
        )
        {
            int distanceWeight = GetDistanceWeight(distanceBand);
            int developmentStrength =
                EnclaveDevelopmentUtility.GetNumericTier(
                    source?.Data
                );
            int reputationWeight = 0;
            int ideologyCompatibilityWeight = 0;

            if (neighborType == EnclaveNeighborType.PlayerSettlement)
            {
                reputationWeight = GetReputationWeight(
                    source?.Data?.ReputationTier ??
                    EnclaveReputationTier.Neutral
                );
            }
            else if (neighborType == EnclaveNeighborType.Enclave)
            {
                PilgrimCamp neighborCamp = neighbor as PilgrimCamp;

                ideologyCompatibilityWeight = (int)
                    EnclaveIdeologyCompatibilityUtility
                        .GetCompatibility(
                            source?.Data,
                            neighborCamp?.Data
                        );
            }

            return new EnclaveInfluenceScore(
                distanceWeight,
                developmentStrength,
                reputationWeight,
                ideologyCompatibilityWeight,
                GetNeighborTypeWeight(neighborType)
            );
        }

        public static int GetDistanceWeight(
            EnclaveDistanceBand distanceBand
        )
        {
            switch (distanceBand)
            {
                case EnclaveDistanceBand.Strong:
                    return StrongDistanceWeight;
                case EnclaveDistanceBand.Moderate:
                    return ModerateDistanceWeight;
                case EnclaveDistanceBand.Weak:
                    return WeakDistanceWeight;
                default:
                    return 0;
            }
        }

        public static int GetReputationWeight(
            EnclaveReputationTier reputationTier
        )
        {
            switch (reputationTier)
            {
                case EnclaveReputationTier.Hostile:
                    return HostileReputationWeight;
                case EnclaveReputationTier.Wary:
                    return WaryReputationWeight;
                case EnclaveReputationTier.Friendly:
                    return FriendlyReputationWeight;
                case EnclaveReputationTier.Trusted:
                    return TrustedReputationWeight;
                case EnclaveReputationTier.Revered:
                    return ReveredReputationWeight;
                default:
                    return NeutralReputationWeight;
            }
        }

        public static int GetNeighborTypeWeight(
            EnclaveNeighborType neighborType
        )
        {
            switch (neighborType)
            {
                case EnclaveNeighborType.FriendlyFactionSettlement:
                    return FriendlySettlementWeight;
                case EnclaveNeighborType.HostileFactionSettlement:
                    return HostileSettlementWeight;
                default:
                    return NeutralSettlementWeight;
            }
        }
    }
}
