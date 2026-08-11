using RimWorld;
using Verse;

namespace IdeologyExpandedEnclaves
{
    internal static class EnclaveHarmService
    {
        private const int FollowerDownedPenalty = 5;
        private const int FollowerKilledPenalty = 15;
        private const int RoleDownedPenalty = 10;
        private const int RoleKilledPenalty = 25;

        public static bool TryResolveEnclavePawn(
            Pawn pawn,
            out PilgrimCamp camp
        )
        {
            camp = pawn?.Map?.Parent as PilgrimCamp;

            if (
                camp == null ||
                pawn.Destroyed ||
                pawn.Faction == null ||
                pawn.Faction == Faction.OfPlayer ||
                pawn.RaceProps == null ||
                !pawn.RaceProps.Humanlike
            )
            {
                camp = null;
                return false;
            }

            if (!IsCurrentEnclavePawn(camp, pawn))
            {
                camp = null;
                return false;
            }

            return true;
        }

        public static bool IsPlayerAttributed(DamageInfo? damageInfo)
        {
            return
                damageInfo.HasValue &&
                damageInfo.Value.Instigator != null &&
                damageInfo.Value.Instigator.Faction == Faction.OfPlayer;
        }

        public static void HandleDowned(
            PilgrimCamp camp,
            Pawn pawn,
            bool playerAttributed
        )
        {
            if (!IsCurrentEnclavePawn(camp, pawn))
            {
                return;
            }

            EnsureTracker(camp);

            int penalty = playerAttributed
                ? (IsSpecialRolePawn(camp, pawn)
                    ? RoleDownedPenalty
                    : FollowerDownedPenalty)
                : 0;

            int amountToApply = camp.HarmPenalties.RecordDowning(
                pawn,
                penalty
            );

            if (amountToApply > 0)
            {
                ApplyPenalty(
                    camp,
                    pawn,
                    amountToApply,
                    "downed"
                );
            }
        }

        public static void HandleKilled(
            PilgrimCamp camp,
            Pawn pawn,
            bool wasDowned
        )
        {
            if (!IsCurrentEnclavePawn(camp, pawn))
            {
                return;
            }

            EnsureTracker(camp);

            int totalPenalty = IsSpecialRolePawn(camp, pawn)
                ? RoleKilledPenalty
                : FollowerKilledPenalty;
            int amountToApply = camp.HarmPenalties.RecordDeath(
                pawn,
                totalPenalty,
                wasDowned
            );

            if (amountToApply > 0)
            {
                ApplyPenalty(
                    camp,
                    pawn,
                    amountToApply,
                    "killed"
                );
            }
        }

        private static void ApplyPenalty(
            PilgrimCamp camp,
            Pawn pawn,
            int penalty,
            string eventLabel
        )
        {
            EnclaveData data = camp?.Data;

            if (data == null)
            {
                Log.Warning(
                    "[IEE] Could not apply enclave harm penalty " +
                    "because persistent enclave data was missing."
                );
                return;
            }

            int previousReputation = data.Reputation;
            EnclaveReputationTier previousTier = data.ReputationTier;
            int updatedReputation = data.ChangeReputation(
                -penalty,
                "player " + eventLabel + " enclave pilgrim"
            );
            EnclaveLocalHostilityService.NotifyReputationChanged(
                camp,
                previousTier
            );
            int appliedPenalty = previousReputation - updatedReputation;

            Log.Message(
                "[IEE] Player-attributed enclave pawn harm: " +
                pawn.LabelShort +
                " (" +
                pawn.GetUniqueLoadID() +
                ") was " +
                eventLabel +
                " at " +
                (data.Name ?? "an enclave") +
                ". Reputation penalty " +
                appliedPenalty +
                " (target " +
                penalty +
                ")."
            );

            if (appliedPenalty <= 0)
            {
                return;
            }

            EnclaveReputationTier updatedTier = data.ReputationTier;
            string message =
                "Pilgrim " +
                eventLabel +
                ": reputation with " +
                (data.Name ?? "the enclave") +
                " decreased by " +
                appliedPenalty +
                " (" +
                previousReputation +
                " -> " +
                updatedReputation +
                ").";

            if (previousTier != updatedTier)
            {
                message +=
                    " Reputation tier changed from " +
                    previousTier +
                    " to " +
                    updatedTier +
                    ".";
            }

            Messages.Message(
                message,
                pawn,
                MessageTypeDefOf.NegativeEvent
            );
        }

        private static bool IsCurrentEnclavePawn(
            PilgrimCamp camp,
            Pawn pawn
        )
        {
            if (
                camp == null ||
                pawn == null ||
                pawn.Faction == null ||
                pawn.Faction == Faction.OfPlayer ||
                pawn.RaceProps == null ||
                !pawn.RaceProps.Humanlike
            )
            {
                return false;
            }

            return
                EnclaveFactionUtility.IsEnclaveFaction(pawn.Faction) &&
                camp.PawnMembers != null &&
                camp.PawnMembers.Contains(pawn);
        }

        private static bool IsSpecialRolePawn(
            PilgrimCamp camp,
            Pawn pawn
        )
        {
            return
                camp?.PawnRoles != null &&
                (
                    camp.PawnRoles.GetPawn(EnclavePawnRole.Leader) == pawn ||
                    camp.PawnRoles.GetPawn(EnclavePawnRole.Trader) == pawn ||
                    camp.PawnRoles.GetPawn(EnclavePawnRole.Recruiter) == pawn
                );
        }

        private static void EnsureTracker(PilgrimCamp camp)
        {
            if (camp.HarmPenalties == null)
            {
                camp.HarmPenalties = new EnclaveHarmPenaltyTracker();
            }
        }
    }
}
