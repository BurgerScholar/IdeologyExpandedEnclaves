using RimWorld;
using RimWorld.Planet;

namespace IdeologyExpandedEnclaves
{
    public enum EnclaveDistanceBand
    {
        None,
        Weak,
        Moderate,
        Strong
    }

    public enum EnclaveNeighborType
    {
        PlayerSettlement,
        Enclave,
        FriendlyFactionSettlement,
        NeutralFactionSettlement,
        HostileFactionSettlement
    }

    public sealed class EnclaveInfluenceScore
    {
        public int DistanceWeight { get; }
        public int DevelopmentStrength { get; }
        public int ReputationWeight { get; }
        public int IdeologyCompatibilityWeight { get; }
        public int NeighborTypeWeight { get; }

        public int Total =>
            DistanceWeight +
            DevelopmentStrength +
            ReputationWeight +
            IdeologyCompatibilityWeight +
            NeighborTypeWeight;

        public EnclaveInfluenceScore(
            int distanceWeight,
            int developmentStrength,
            int reputationWeight,
            int ideologyCompatibilityWeight,
            int neighborTypeWeight
        )
        {
            DistanceWeight = distanceWeight;
            DevelopmentStrength = developmentStrength;
            ReputationWeight = reputationWeight;
            IdeologyCompatibilityWeight =
                ideologyCompatibilityWeight;
            NeighborTypeWeight = neighborTypeWeight;
        }
    }

    public sealed class EnclaveNeighborInfo
    {
        public WorldObject WorldObject { get; }
        public EnclaveNeighborType NeighborType { get; }
        public float DistanceInTiles { get; }
        public EnclaveDistanceBand DistanceBand { get; }
        public EnclaveDevelopmentTier DevelopmentTier { get; }
        public int? Reputation { get; }
        public EnclaveIdeologyType IdeologyType { get; }
        public EnclaveIdeologyCompatibility IdeologyCompatibility
            { get; }
        public int? RelationshipScore { get; }
        public InterEnclaveRelationshipState? RelationshipState
            { get; }
        public FactionRelationKind? FactionRelationToPlayer { get; }
        public EnclaveInfluenceScore Influence { get; }
        public int RegionalPressure { get; }

        public int WorldObjectId => WorldObject?.ID ?? -1;
        public string Label =>
            (WorldObject as PilgrimCamp)?.Data?.Name ??
            WorldObject?.LabelCap ??
            "Unknown";

        public EnclaveNeighborInfo(
            WorldObject worldObject,
            EnclaveNeighborType neighborType,
            float distanceInTiles,
            EnclaveDistanceBand distanceBand,
            EnclaveDevelopmentTier developmentTier,
            int? reputation,
            EnclaveIdeologyType ideologyType,
            EnclaveIdeologyCompatibility ideologyCompatibility,
            int? interEnclaveRelationshipScore,
            InterEnclaveRelationshipState?
                interEnclaveRelationshipState,
            FactionRelationKind? factionRelationToPlayer,
            EnclaveInfluenceScore influence,
            int regionalPressure
        )
        {
            WorldObject = worldObject;
            NeighborType = neighborType;
            DistanceInTiles = distanceInTiles;
            DistanceBand = distanceBand;
            DevelopmentTier = developmentTier;
            Reputation = reputation;
            IdeologyType = ideologyType;
            IdeologyCompatibility = ideologyCompatibility;
            RelationshipScore =
                interEnclaveRelationshipScore;
            RelationshipState =
                interEnclaveRelationshipState;
            FactionRelationToPlayer = factionRelationToPlayer;
            Influence = influence;
            RegionalPressure = regionalPressure;
        }
    }
}
