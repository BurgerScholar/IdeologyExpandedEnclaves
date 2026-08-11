using System.Collections.Generic;

namespace IdeologyExpandedEnclaves
{
    public enum EnclaveRegionalStatus
    {
        StronglyPressured,
        Pressured,
        Stable,
        Supported,
        StronglySupported
    }

    public sealed class EnclaveRegionalInfluenceSummary
    {
        public int TotalPressure { get; }
        public EnclaveRegionalStatus Status { get; }

        public string StatusLabel =>
            EnclaveProximityProfileUtility.GetRegionalStatusLabel(
                Status
            );

        public EnclaveRegionalInfluenceSummary(
            int totalPressure,
            EnclaveRegionalStatus status
        )
        {
            TotalPressure = totalPressure;
            Status = status;
        }
    }

    public sealed class EnclaveCampProximityEffect
    {
        public PilgrimCamp Camp { get; }
        public EnclaveNeighborInfo NearestPlayerSettlement { get; }
        public int PlayerReputationDelta { get; }
        public int StartingReputation { get; }
        public int ProjectedReputation { get; }
        public EnclaveRegionalInfluenceSummary RegionalInfluence
            { get; }

        public EnclaveCampProximityEffect(
            PilgrimCamp camp,
            EnclaveNeighborInfo nearestPlayerSettlement,
            int playerReputationDelta,
            EnclaveRegionalInfluenceSummary regionalInfluence
        )
        {
            Camp = camp;
            NearestPlayerSettlement = nearestPlayerSettlement;
            PlayerReputationDelta = playerReputationDelta;
            StartingReputation = camp?.Data?.Reputation ?? 0;
            ProjectedReputation = EnclaveReputation.Clamp(
                (long)StartingReputation + playerReputationDelta
            );
            RegionalInfluence = regionalInfluence;
        }
    }

    public sealed class EnclaveRelationshipProximityEffect
    {
        public PilgrimCamp FirstCamp { get; }
        public PilgrimCamp SecondCamp { get; }
        public EnclaveDistanceBand DistanceBand { get; }
        public float DistanceInTiles { get; }
        public EnclaveIdeologyCompatibility Compatibility { get; }
        public int RelationshipDelta { get; }
        public int StartingRelationship { get; }
        public int ProjectedRelationship { get; }

        public EnclaveRelationshipProximityEffect(
            PilgrimCamp firstCamp,
            PilgrimCamp secondCamp,
            EnclaveDistanceBand distanceBand,
            float distanceInTiles,
            EnclaveIdeologyCompatibility compatibility,
            int relationshipDelta,
            int startingRelationship
        )
        {
            FirstCamp = firstCamp;
            SecondCamp = secondCamp;
            DistanceBand = distanceBand;
            DistanceInTiles = distanceInTiles;
            Compatibility = compatibility;
            RelationshipDelta = relationshipDelta;
            StartingRelationship = startingRelationship;
            ProjectedRelationship =
                InterEnclaveRelationshipUtility.ClampScore(
                    (long)startingRelationship + relationshipDelta
                );
        }

        public bool Includes(PilgrimCamp camp)
        {
            return camp != null &&
                (FirstCamp == camp || SecondCamp == camp);
        }

        public PilgrimCamp GetOtherCamp(PilgrimCamp camp)
        {
            if (camp == FirstCamp)
            {
                return SecondCamp;
            }

            return camp == SecondCamp ? FirstCamp : null;
        }
    }

    public sealed class EnclaveProximityPulseResult
    {
        public List<EnclaveCampProximityEffect> CampEffects { get; } =
            new List<EnclaveCampProximityEffect>();
        public List<EnclaveRelationshipProximityEffect>
            RelationshipEffects { get; } =
                new List<EnclaveRelationshipProximityEffect>();

        public EnclaveCampProximityEffect GetCampEffect(
            PilgrimCamp camp
        )
        {
            return CampEffects.Find(effect => effect.Camp == camp);
        }
    }
}
