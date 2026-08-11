using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveProximityUtility
    {
        public const float StrongProximityMaximum = 10f;
        public const float ModerateProximityMaximum = 20f;
        public const float WeakProximityMaximum = 30f;

        public static List<EnclaveNeighborInfo> GetNearbyNeighbors(
            PilgrimCamp camp
        )
        {
            List<EnclaveNeighborInfo> neighbors =
                new List<EnclaveNeighborInfo>();

            if (
                camp == null ||
                camp.Destroyed ||
                !camp.Tile.Valid ||
                Find.WorldGrid == null ||
                Find.WorldObjects?.AllWorldObjects == null
            )
            {
                return neighbors;
            }

            foreach (
                WorldObject worldObject in
                Find.WorldObjects.AllWorldObjects
            )
            {
                if (
                    worldObject == null ||
                    worldObject == camp ||
                    worldObject.Destroyed ||
                    !worldObject.Spawned ||
                    !worldObject.Tile.Valid
                )
                {
                    continue;
                }

                EnclaveNeighborType neighborType;
                FactionRelationKind? relationToPlayer;

                if (
                    !TryClassifyNeighbor(
                        worldObject,
                        out neighborType,
                        out relationToPlayer
                    )
                )
                {
                    continue;
                }

                float distance = GetDistanceInTiles(
                    camp,
                    worldObject
                );
                EnclaveDistanceBand distanceBand =
                    GetDistanceBand(distance);

                if (distanceBand == EnclaveDistanceBand.None)
                {
                    continue;
                }

                neighbors.Add(
                    CreateNeighborInfo(
                        camp,
                        worldObject,
                        neighborType,
                        relationToPlayer,
                        distance,
                        distanceBand
                    )
                );
            }

            neighbors.Sort(
                (first, second) =>
                    first.DistanceInTiles.CompareTo(
                        second.DistanceInTiles
                    )
            );

            return neighbors;
        }

        public static List<EnclaveNeighborInfo>
            GetNearbyPlayerSettlements(PilgrimCamp camp)
        {
            return GetNeighborsOfType(
                camp,
                EnclaveNeighborType.PlayerSettlement
            );
        }

        public static List<EnclaveNeighborInfo> GetNearbyEnclaves(
            PilgrimCamp camp
        )
        {
            return GetNeighborsOfType(
                camp,
                EnclaveNeighborType.Enclave
            );
        }

        public static float GetDistanceInTiles(
            WorldObject first,
            WorldObject second
        )
        {
            if (
                first == null ||
                second == null ||
                !first.Tile.Valid ||
                !second.Tile.Valid ||
                first.Tile.Layer != second.Tile.Layer ||
                Find.WorldGrid == null
            )
            {
                return float.PositiveInfinity;
            }

            return Find.WorldGrid.ApproxDistanceInTiles(
                first.Tile,
                second.Tile
            );
        }

        public static EnclaveDistanceBand GetDistanceBand(
            float distanceInTiles
        )
        {
            if (
                float.IsNaN(distanceInTiles) ||
                float.IsInfinity(distanceInTiles) ||
                distanceInTiles < 0f
            )
            {
                return EnclaveDistanceBand.None;
            }

            if (distanceInTiles <= StrongProximityMaximum)
            {
                return EnclaveDistanceBand.Strong;
            }

            if (distanceInTiles <= ModerateProximityMaximum)
            {
                return EnclaveDistanceBand.Moderate;
            }

            if (distanceInTiles <= WeakProximityMaximum)
            {
                return EnclaveDistanceBand.Weak;
            }

            return EnclaveDistanceBand.None;
        }

        public static string GetDistanceBandDisplayName(
            EnclaveDistanceBand distanceBand
        )
        {
            switch (distanceBand)
            {
                case EnclaveDistanceBand.Strong:
                    return "Strong";
                case EnclaveDistanceBand.Moderate:
                    return "Moderate";
                case EnclaveDistanceBand.Weak:
                    return "Weak";
                default:
                    return "None";
            }
        }

        private static List<EnclaveNeighborInfo> GetNeighborsOfType(
            PilgrimCamp camp,
            EnclaveNeighborType neighborType
        )
        {
            List<EnclaveNeighborInfo> matches =
                new List<EnclaveNeighborInfo>();

            foreach (
                EnclaveNeighborInfo neighbor in
                GetNearbyNeighbors(camp)
            )
            {
                if (neighbor.NeighborType == neighborType)
                {
                    matches.Add(neighbor);
                }
            }

            return matches;
        }

        private static bool TryClassifyNeighbor(
            WorldObject worldObject,
            out EnclaveNeighborType neighborType,
            out FactionRelationKind? relationToPlayer
        )
        {
            relationToPlayer = null;

            if (worldObject is PilgrimCamp)
            {
                neighborType = EnclaveNeighborType.Enclave;
                return true;
            }

            Settlement settlement = worldObject as Settlement;

            if (settlement == null)
            {
                neighborType = default(EnclaveNeighborType);
                return false;
            }

            Faction playerFaction = Faction.OfPlayerSilentFail;

            if (
                playerFaction != null &&
                settlement.Faction == playerFaction
            )
            {
                neighborType =
                    EnclaveNeighborType.PlayerSettlement;
                relationToPlayer = FactionRelationKind.Ally;
                return true;
            }

            if (playerFaction == null || settlement.Faction == null)
            {
                neighborType = default(EnclaveNeighborType);
                return false;
            }

            relationToPlayer = settlement.Faction.RelationKindWith(
                playerFaction
            );

            switch (relationToPlayer.Value)
            {
                case FactionRelationKind.Ally:
                    neighborType =
                        EnclaveNeighborType.FriendlyFactionSettlement;
                    break;
                case FactionRelationKind.Hostile:
                    neighborType =
                        EnclaveNeighborType.HostileFactionSettlement;
                    break;
                default:
                    neighborType =
                        EnclaveNeighborType.NeutralFactionSettlement;
                    break;
            }

            return true;
        }

        private static EnclaveNeighborInfo CreateNeighborInfo(
            PilgrimCamp source,
            WorldObject worldObject,
            EnclaveNeighborType neighborType,
            FactionRelationKind? relationToPlayer,
            float distance,
            EnclaveDistanceBand distanceBand
        )
        {
            PilgrimCamp neighborCamp = worldObject as PilgrimCamp;
            EnclaveData neighborData = neighborCamp?.Data;
            EnclaveIdeologyCompatibility compatibility =
                neighborType == EnclaveNeighborType.Enclave
                    ? EnclaveIdeologyCompatibilityUtility
                        .GetCompatibility(
                            source.Data,
                            neighborData
                        )
                    : EnclaveIdeologyCompatibility.Neutral;
            InterEnclaveRelationshipRecord relationship =
                neighborType == EnclaveNeighborType.Enclave &&
                neighborCamp != null
                    ? InterEnclaveRelationshipUtility.GetRelationship(
                        source,
                        neighborCamp
                    )
                    : null;

            return new EnclaveNeighborInfo(
                worldObject,
                neighborType,
                distance,
                distanceBand,
                EnclaveDevelopmentUtility.GetTier(neighborData),
                neighborData == null
                    ? (int?)null
                    : neighborData.Reputation,
                EnclaveIdeologyUtility.GetIdeologyType(neighborData),
                compatibility,
                relationship?.Score,
                relationship == null
                    ? (InterEnclaveRelationshipState?)null
                    : InterEnclaveRelationshipUtility.GetState(
                        relationship.Score
                    ),
                relationToPlayer,
                EnclaveInfluenceUtility.CalculateInfluence(
                    source,
                    worldObject,
                    neighborType,
                    distanceBand
                )
            );
        }
    }
}
