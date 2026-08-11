using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveProximityEffectsService
    {
        private const string AutomaticDriftReason =
            "regional proximity drift";

        public static EnclaveProximityPulseResult PreviewPulse()
        {
            EnclaveProximityPulseResult result =
                new EnclaveProximityPulseResult();
            List<PilgrimCamp> camps = GetInitializedCamps();

            foreach (PilgrimCamp camp in camps)
            {
                List<EnclaveNeighborInfo> neighbors =
                    EnclaveProximityUtility.GetNearbyNeighbors(camp);
                EnclaveNeighborInfo nearestPlayerSettlement =
                    neighbors.Find(
                        neighbor =>
                            neighbor.NeighborType ==
                                EnclaveNeighborType.PlayerSettlement
                    );
                int reputationDelta =
                    EnclaveProximityProfileUtility
                        .CalculatePlayerReputationDrift(
                            camp,
                            nearestPlayerSettlement
                        );

                result.CampEffects.Add(
                    new EnclaveCampProximityEffect(
                        camp,
                        nearestPlayerSettlement,
                        reputationDelta,
                        EnclaveInfluenceUtility
                            .CalculateRegionalSummary(camp, neighbors)
                    )
                );

                foreach (EnclaveNeighborInfo neighbor in neighbors)
                {
                    PilgrimCamp neighborCamp =
                        neighbor.WorldObject as PilgrimCamp;

                    if (
                        neighbor.NeighborType !=
                            EnclaveNeighborType.Enclave ||
                        neighborCamp?.Data == null ||
                        camp.ID >= neighborCamp.ID
                    )
                    {
                        continue;
                    }

                    int relationshipDelta =
                        EnclaveProximityProfileUtility
                            .CalculateRelationshipDrift(
                                camp,
                                neighborCamp,
                                neighbor.DistanceBand
                            );
                    int startingRelationship =
                        InterEnclaveRelationshipUtility
                            .GetCurrentOrInitialScore(
                                camp,
                                neighborCamp
                            );

                    result.RelationshipEffects.Add(
                        new EnclaveRelationshipProximityEffect(
                            camp,
                            neighborCamp,
                            neighbor.DistanceBand,
                            neighbor.DistanceInTiles,
                            neighbor.IdeologyCompatibility,
                            relationshipDelta,
                            startingRelationship
                        )
                    );
                }
            }

            return result;
        }

        public static EnclaveProximityPulseResult ApplyPulse()
        {
            EnclaveProximityPulseResult result = PreviewPulse();

            foreach (EnclaveCampProximityEffect effect in result.CampEffects)
            {
                ApplyPlayerReputationDrift(effect);
            }

            foreach (
                EnclaveRelationshipProximityEffect effect in
                result.RelationshipEffects
            )
            {
                ApplyRelationshipDrift(effect);
            }

            if (Prefs.DevMode)
            {
                Log.Message(
                    "[IEE] Applied regional proximity pulse to " +
                    result.CampEffects.Count +
                    " initialized enclave(s) and " +
                    result.RelationshipEffects.Count +
                    " canonical enclave pair(s)."
                );
            }

            return result;
        }

        private static void ApplyPlayerReputationDrift(
            EnclaveCampProximityEffect effect
        )
        {
            PilgrimCamp camp = effect?.Camp;

            if (
                camp?.Data == null ||
                camp.Destroyed ||
                effect.PlayerReputationDelta == 0
            )
            {
                return;
            }

            int previousReputation = camp.Data.Reputation;
            EnclaveReputationTier previousTier =
                camp.Data.ReputationTier;
            int updatedReputation = camp.Data.ChangeReputation(
                effect.PlayerReputationDelta,
                AutomaticDriftReason
            );

            EnclaveLocalHostilityService.NotifyReputationChanged(
                camp,
                previousTier
            );

            EnclaveReputationTier updatedTier = camp.Data.ReputationTier;

            if (Prefs.DevMode)
            {
                Log.Message(
                    "[IEE] Proximity reputation drift for " +
                    camp.Data.Name +
                    ": " +
                    previousReputation +
                    " -> " +
                    updatedReputation +
                    " (target delta " +
                    FormatSigned(effect.PlayerReputationDelta) +
                    ", nearest colony " +
                    (effect.NearestPlayerSettlement?.Label ?? "none") +
                    ")."
                );
            }

            if (previousTier == updatedTier)
            {
                return;
            }

            string message = effect.PlayerReputationDelta > 0
                ? "Relations with " +
                    camp.Data.Name +
                    " have improved to " +
                    camp.Data.ReputationTierLabel +
                    " due to your neighboring settlements."
                : camp.Data.Name +
                    " has become " +
                    camp.Data.ReputationTierLabel +
                    " due to your nearby expansion.";

            Messages.Message(
                message,
                effect.PlayerReputationDelta > 0
                    ? MessageTypeDefOf.PositiveEvent
                    : MessageTypeDefOf.NegativeEvent
            );
        }

        private static void ApplyRelationshipDrift(
            EnclaveRelationshipProximityEffect effect
        )
        {
            if (
                effect?.FirstCamp?.Data == null ||
                effect.SecondCamp?.Data == null ||
                effect.FirstCamp.Destroyed ||
                effect.SecondCamp.Destroyed ||
                effect.RelationshipDelta == 0
            )
            {
                return;
            }

            InterEnclaveRelationshipUtility.AdjustRelationship(
                effect.FirstCamp,
                effect.SecondCamp,
                effect.RelationshipDelta,
                AutomaticDriftReason,
                logChange: Prefs.DevMode
            );
        }

        private static List<PilgrimCamp> GetInitializedCamps()
        {
            List<PilgrimCamp> camps = new List<PilgrimCamp>();

            if (Find.WorldObjects?.AllWorldObjects == null)
            {
                return camps;
            }

            foreach (
                WorldObject worldObject in
                Find.WorldObjects.AllWorldObjects
            )
            {
                PilgrimCamp camp = worldObject as PilgrimCamp;

                if (
                    camp?.Data == null ||
                    camp.Destroyed ||
                    !camp.Spawned ||
                    !camp.Tile.Valid
                )
                {
                    continue;
                }

                camps.Add(camp);
            }

            camps.Sort((first, second) => first.ID.CompareTo(second.ID));
            return camps;
        }

        private static string FormatSigned(int value)
        {
            return value >= 0 ? "+" + value : value.ToString();
        }
    }
}
