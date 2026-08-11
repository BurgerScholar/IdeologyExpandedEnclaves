using System;
using RimWorld.Planet;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveProximityProfileUtility
    {
        public const int PulseIntervalTicks = 900000;

        public const int StrongPositiveTendency = 2;
        public const int MildPositiveTendency = 1;
        public const int MildNegativeTendency = -1;
        public const int StrongNegativeTendency = -2;

        public const int DriftScale = 8;
        public const int MaximumReputationDrift = 3;
        public const int MaximumRelationshipDrift = 3;

        public const int StronglyPressuredMaximum = -12;
        public const int PressuredMaximum = -4;
        public const int StableMaximum = 3;
        public const int SupportedMaximum = 11;

        public static int GetIdeologySettlementTendency(
            EnclaveIdeologyType ideologyType
        )
        {
            switch (ideologyType)
            {
                case EnclaveIdeologyType.Communal:
                case EnclaveIdeologyType.Mercantile:
                    return StrongPositiveTendency;
                case EnclaveIdeologyType.Spiritual:
                case EnclaveIdeologyType.Martial:
                case EnclaveIdeologyType.Transhumanist:
                    return MildPositiveTendency;
                case EnclaveIdeologyType.Nature:
                    return MildNegativeTendency;
                case EnclaveIdeologyType.Isolationist:
                    return StrongNegativeTendency;
                default:
                    return 0;
            }
        }

        public static int GetDevelopmentWeight(EnclaveData data)
        {
            return Math.Max(
                1,
                EnclaveDevelopmentUtility.GetNumericTier(data)
            );
        }

        public static int GetSharedDevelopmentWeight(
            EnclaveData first,
            EnclaveData second
        )
        {
            int firstWeight = GetDevelopmentWeight(first);
            int secondWeight = GetDevelopmentWeight(second);

            return (firstWeight + secondWeight + 1) / 2;
        }

        public static int CalculateRegionalPressure(
            PilgrimCamp source,
            WorldObject neighbor,
            EnclaveNeighborType neighborType,
            EnclaveDistanceBand distanceBand
        )
        {
            if (source?.Data == null || neighbor == null)
            {
                return 0;
            }

            int distanceWeight =
                EnclaveInfluenceUtility.GetDistanceWeight(distanceBand);

            if (distanceWeight <= 0)
            {
                return 0;
            }

            if (neighborType == EnclaveNeighborType.PlayerSettlement)
            {
                return
                    GetIdeologySettlementTendency(
                        EnclaveIdeologyUtility.GetIdeologyType(source.Data)
                    ) *
                    distanceWeight *
                    GetDevelopmentWeight(source.Data);
            }

            if (neighborType == EnclaveNeighborType.Enclave)
            {
                PilgrimCamp neighborCamp = neighbor as PilgrimCamp;

                if (neighborCamp?.Data == null)
                {
                    return 0;
                }

                return
                    (int)EnclaveIdeologyCompatibilityUtility
                        .GetCompatibility(
                            source.Data,
                            neighborCamp.Data
                        ) *
                    distanceWeight *
                    GetSharedDevelopmentWeight(
                        source.Data,
                        neighborCamp.Data
                    );
            }

            return
                EnclaveInfluenceUtility.GetNeighborTypeWeight(
                    neighborType
                ) *
                distanceWeight *
                GetDevelopmentWeight(source.Data);
        }

        public static int CalculatePlayerReputationDrift(
            PilgrimCamp camp,
            EnclaveNeighborInfo nearestPlayerSettlement
        )
        {
            if (
                camp?.Data == null ||
                nearestPlayerSettlement == null ||
                nearestPlayerSettlement.NeighborType !=
                    EnclaveNeighborType.PlayerSettlement
            )
            {
                return 0;
            }

            int rawPressure = CalculateRegionalPressure(
                camp,
                nearestPlayerSettlement.WorldObject,
                nearestPlayerSettlement.NeighborType,
                nearestPlayerSettlement.DistanceBand
            );

            return NormalizeDrift(
                rawPressure,
                MaximumReputationDrift
            );
        }

        public static int CalculateRelationshipDrift(
            PilgrimCamp first,
            PilgrimCamp second,
            EnclaveDistanceBand distanceBand
        )
        {
            if (first?.Data == null || second?.Data == null)
            {
                return 0;
            }

            int rawPressure =
                (int)EnclaveIdeologyCompatibilityUtility
                    .GetCompatibility(first.Data, second.Data) *
                EnclaveInfluenceUtility.GetDistanceWeight(distanceBand) *
                GetSharedDevelopmentWeight(first.Data, second.Data);

            return NormalizeDrift(
                rawPressure,
                MaximumRelationshipDrift
            );
        }

        public static EnclaveRegionalStatus GetRegionalStatus(
            int totalPressure
        )
        {
            if (totalPressure <= StronglyPressuredMaximum)
            {
                return EnclaveRegionalStatus.StronglyPressured;
            }

            if (totalPressure <= PressuredMaximum)
            {
                return EnclaveRegionalStatus.Pressured;
            }

            if (totalPressure <= StableMaximum)
            {
                return EnclaveRegionalStatus.Stable;
            }

            if (totalPressure <= SupportedMaximum)
            {
                return EnclaveRegionalStatus.Supported;
            }

            return EnclaveRegionalStatus.StronglySupported;
        }

        public static string GetRegionalStatusLabel(
            EnclaveRegionalStatus status
        )
        {
            switch (status)
            {
                case EnclaveRegionalStatus.StronglyPressured:
                    return "Strongly Pressured";
                case EnclaveRegionalStatus.Pressured:
                    return "Pressured";
                case EnclaveRegionalStatus.Supported:
                    return "Supported";
                case EnclaveRegionalStatus.StronglySupported:
                    return "Strongly Supported";
                default:
                    return "Stable";
            }
        }

        private static int NormalizeDrift(
            int rawPressure,
            int maximumMagnitude
        )
        {
            if (rawPressure == 0)
            {
                return 0;
            }

            int magnitude = Math.Min(
                maximumMagnitude,
                (Math.Abs(rawPressure) + DriftScale - 1) /
                    DriftScale
            );

            return rawPressure < 0 ? -magnitude : magnitude;
        }
    }
}
