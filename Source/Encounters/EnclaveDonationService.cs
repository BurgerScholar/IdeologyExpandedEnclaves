using System.Collections.Generic;
using RimWorld;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public sealed class EnclaveDonationOption
    {
        public readonly int SilverAmount;
        public readonly int ReputationGain;

        public EnclaveDonationOption(
            int silverAmount,
            int reputationGain
        )
        {
            SilverAmount = silverAmount;
            ReputationGain = reputationGain;
        }
    }

    public static class EnclaveDonationService
    {
        private static readonly List<EnclaveDonationOption>
            donationOptions = new List<EnclaveDonationOption>
            {
                new EnclaveDonationOption(100, 2),
                new EnclaveDonationOption(250, 5),
                new EnclaveDonationOption(500, 10),
                new EnclaveDonationOption(1000, 20)
            };

        public static IReadOnlyList<EnclaveDonationOption> Options =>
            donationOptions;

        public static int GetAvailableSilver(PilgrimCamp camp)
        {
            return camp?.VisitingGroup?.CountInventoryThing(
                camp,
                ThingDefOf.Silver
            ) ?? 0;
        }

        public static bool DonationContactIsValid(
            PilgrimCamp camp,
            Pawn leader,
            out string failureReason
        )
        {
            failureReason = null;

            if (
                camp == null ||
                camp.Destroyed ||
                !camp.Spawned ||
                camp.Data == null ||
                camp.Map == null
            )
            {
                failureReason =
                    "The enclave is no longer available.";
                return false;
            }

            if (EnclaveRelationshipUtility.IsLocallyHostile(camp))
            {
                failureReason =
                    "Donations unavailable. This enclave is hostile " +
                    "toward your visiting group.";
                return false;
            }

            if (
                leader == null ||
                leader.Destroyed ||
                leader.Dead ||
                !leader.Spawned ||
                leader.Map != camp.Map ||
                !leader.RaceProps.Humanlike ||
                leader.Faction == Faction.OfPlayer ||
                camp.PawnRoles?.GetPawn(EnclavePawnRole.Leader) !=
                    leader
            )
            {
                failureReason =
                    "The enclave Leader is no longer available.";
                return false;
            }

            if (
                camp.VisitingGroup == null ||
                !camp.VisitingGroup.HasStoredMembers ||
                !camp.VisitingGroup.HasActiveMembers(camp)
            )
            {
                failureReason =
                    "No active visiting caravan group is registered " +
                    "for this enclave.";
                return false;
            }

            return true;
        }

        public static bool TryDonate(
            PilgrimCamp camp,
            Pawn leader,
            int silverAmount,
            out string resultMessage
        )
        {
            if (
                !DonationContactIsValid(
                    camp,
                    leader,
                    out resultMessage
                )
            )
            {
                return false;
            }

            EnclaveDonationOption option = GetOption(silverAmount);

            if (option == null)
            {
                resultMessage = "That donation amount is invalid.";
                return false;
            }

            if (camp.Data.Reputation >= EnclaveReputation.Maximum)
            {
                resultMessage =
                    "This enclave's reputation is already at +" +
                    EnclaveReputation.Maximum +
                    ". No silver was consumed.";
                return false;
            }

            int availableSilver = GetAvailableSilver(camp);

            if (availableSilver < option.SilverAmount)
            {
                resultMessage =
                    "The visiting group has only " +
                    availableSilver.ToString("N0") +
                    " of the required " +
                    option.SilverAmount.ToString("N0") +
                    " silver. No silver was consumed.";
                return false;
            }

            int previousReputation = camp.Data.Reputation;
            EnclaveReputationTier previousTier =
                camp.Data.ReputationTier;

            if (
                !camp.VisitingGroup.TryConsumeInventoryThing(
                    camp,
                    ThingDefOf.Silver,
                    option.SilverAmount
                )
            )
            {
                resultMessage =
                    "The visiting group's silver changed before the " +
                    "donation could be completed. No silver was consumed.";
                return false;
            }

            int updatedReputation = camp.Data.ChangeReputation(
                option.ReputationGain,
                "silver donation"
            );
            EnclaveLocalHostilityService.NotifyReputationChanged(
                camp,
                previousTier
            );
            EnclaveReputationTier updatedTier =
                camp.Data.ReputationTier;
            int appliedGain =
                updatedReputation - previousReputation;

            resultMessage =
                "Donated " +
                option.SilverAmount.ToString("N0") +
                " silver to " +
                (camp.Data.Name ?? "the enclave") +
                ". Reputation increased by " +
                appliedGain +
                ": " +
                previousReputation +
                " -> " +
                updatedReputation +
                " (" +
                updatedTier +
                ").";

            if (previousTier != updatedTier)
            {
                resultMessage +=
                    " Reputation tier advanced from " +
                    previousTier +
                    " to " +
                    updatedTier +
                    ".";
            }

            if (appliedGain < option.ReputationGain)
            {
                resultMessage +=
                    " Reputation is capped at +" +
                    EnclaveReputation.Maximum +
                    ".";
            }

            Log.Message(
                "[IEE] Donated " +
                option.SilverAmount +
                " silver to " +
                (camp.Data.Name ?? "an enclave") +
                " through Leader " +
                leader.LabelShort +
                " (" +
                leader.GetUniqueLoadID() +
                "). Reputation: " +
                previousReputation +
                " -> " +
                updatedReputation +
                " (" +
                updatedTier +
                ")."
            );

            return true;
        }

        private static EnclaveDonationOption GetOption(
            int silverAmount
        )
        {
            foreach (EnclaveDonationOption option in donationOptions)
            {
                if (option.SilverAmount == silverAmount)
                {
                    return option;
                }
            }

            return null;
        }
    }
}
