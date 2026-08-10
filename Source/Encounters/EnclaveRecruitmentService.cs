using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveRecruitmentService
    {
        public const int BaseRecruitmentCost = 500;

        private class SilverPayment
        {
            public Pawn Holder;
            public int Count;
        }

        public static int GetRecruitmentCost(Pawn candidate)
        {
            return BaseRecruitmentCost;
        }

        public static int GetReputationDiscountPercent(
            PilgrimCamp camp
        )
        {
            if (camp?.Data == null)
            {
                return 0;
            }

            switch (camp.Data.ReputationTier)
            {
                case EnclaveReputationTier.Friendly:
                    return 10;
                case EnclaveReputationTier.Trusted:
                    return 20;
                case EnclaveReputationTier.Revered:
                    return 30;
                default:
                    return 0;
            }
        }

        public static int GetEffectiveRecruitmentCost(
            PilgrimCamp camp,
            Pawn candidate
        )
        {
            int baseCost = Math.Max(0, GetRecruitmentCost(candidate));
            int discountPercent =
                GetReputationDiscountPercent(camp);
            long discountedCost =
                (long)baseCost * (100 - discountPercent);

            return (int)((discountedCost + 99) / 100);
        }

        public static bool RecruitmentIsAvailable(
            PilgrimCamp camp,
            out string unavailableReason
        )
        {
            unavailableReason = null;

            if (camp?.Data == null)
            {
                unavailableReason =
                    "The enclave reputation data is unavailable.";
                return false;
            }

            EnclaveReputationTier tier = camp.Data.ReputationTier;

            if (
                tier == EnclaveReputationTier.Hostile ||
                tier == EnclaveReputationTier.Wary
            )
            {
                unavailableReason =
                    "Recruitment unavailable. This enclave does not " +
                    "trust you enough to recruit its members. " +
                    "Current reputation: " +
                    camp.Data.Reputation +
                    " — " +
                    tier +
                    ".";
                return false;
            }

            return true;
        }

        public static int GetAvailableSilver(PilgrimCamp camp)
        {
            Map map = camp?.Map;

            if (map == null)
            {
                return 0;
            }

            int total = 0;

            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (
                    pawn?.Faction == Faction.OfPlayer &&
                    pawn.inventory != null
                )
                {
                    total += pawn.inventory.Count(ThingDefOf.Silver);
                }
            }

            return total;
        }

        public static bool TryRecruit(
            PilgrimCamp camp,
            Pawn candidate,
            int confirmedCost,
            out string resultMessage
        )
        {
            if (!ValidateRecruitment(camp, candidate, out resultMessage))
            {
                return false;
            }

            if (!RecruitmentIsAvailable(camp, out resultMessage))
            {
                return false;
            }

            EnclaveReputationTier reputationTier =
                camp.Data.ReputationTier;
            int baseCost = GetRecruitmentCost(candidate);
            int cost = GetEffectiveRecruitmentCost(camp, candidate);

            if (confirmedCost != cost)
            {
                resultMessage =
                    "The recruitment cost changed with the enclave's " +
                    "reputation. The current cost is " +
                    cost +
                    " silver. Review and confirm the updated price.";
                return false;
            }

            List<SilverPayment> paymentPlan;

            if (!TryBuildPaymentPlan(camp.Map, cost, out paymentPlan))
            {
                resultMessage =
                    "The visiting group does not have " +
                    cost +
                    " silver available in its inventories.";

                return false;
            }

            if (!PaymentPlanIsStillValid(paymentPlan, cost))
            {
                resultMessage =
                    "The required silver is no longer available. " +
                    "No payment was taken.";

                return false;
            }

            ConsumePayment(paymentPlan);

            Lord lord = candidate.GetLord();

            lord?.RemovePawn(candidate);
            candidate.SetFaction(Faction.OfPlayer);
            camp.RecruitmentCandidates.RemoveCandidate(candidate);
            camp.PawnMembers?.Remove(candidate);
            camp.Data.Population = Math.Max(
                0,
                camp.Data.Population - 1
            );

            resultMessage =
                candidate.LabelShort +
                " has joined your colony for " +
                cost +
                " silver.";

            Log.Message(
                "[IEE] Recruited candidate " +
                candidate.LabelShort +
                " (" +
                candidate.GetUniqueLoadID() +
                ") from " +
                (camp.Data?.Name ?? "an enclave") +
                ". Reputation tier: " +
                reputationTier +
                ". Base price: " +
                baseCost +
                " silver. Final price: " +
                cost +
                " silver. Remaining enclave population: " +
                camp.Data.Population +
                "."
            );

            return true;
        }

        private static bool ValidateRecruitment(
            PilgrimCamp camp,
            Pawn candidate,
            out string failureReason
        )
        {
            failureReason = null;

            if (
                camp == null ||
                camp.Data == null ||
                camp.Map == null ||
                camp.RecruitmentCandidates == null
            )
            {
                failureReason =
                    "The enclave recruitment data is unavailable.";
                return false;
            }

            if (
                candidate == null ||
                candidate.Dead ||
                !candidate.Spawned ||
                candidate.Map != camp.Map ||
                !candidate.RaceProps.Humanlike
            )
            {
                failureReason =
                    "This candidate is no longer available.";
                return false;
            }

            if (
                candidate.Faction == null ||
                candidate.Faction == Faction.OfPlayer
            )
            {
                failureReason =
                    "This pawn is no longer an enclave candidate.";
                return false;
            }

            if (
                !camp.RecruitmentCandidates
                    .IsRecruitmentCandidate(candidate)
            )
            {
                failureReason =
                    "This candidate has already been recruited or removed.";
                return false;
            }

            Pawn leader = camp.PawnRoles?.GetPawn(
                EnclavePawnRole.Leader
            );
            Pawn trader = camp.PawnRoles?.GetPawn(
                EnclavePawnRole.Trader
            );
            Pawn recruiter = camp.PawnRoles?.GetPawn(
                EnclavePawnRole.Recruiter
            );

            if (
                candidate == leader ||
                candidate == trader ||
                candidate == recruiter
            )
            {
                failureReason =
                    "Enclave role holders cannot be recruited as candidates.";
                return false;
            }

            if (
                recruiter == null ||
                recruiter.Dead ||
                !recruiter.Spawned ||
                recruiter.Map != camp.Map ||
                recruiter.Faction != candidate.Faction
            )
            {
                failureReason =
                    "The enclave Recruiter is no longer available.";
                return false;
            }

            return true;
        }

        private static bool TryBuildPaymentPlan(
            Map map,
            int cost,
            out List<SilverPayment> paymentPlan
        )
        {
            paymentPlan = new List<SilverPayment>();

            if (map == null || cost <= 0)
            {
                return cost <= 0;
            }

            int remaining = cost;

            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (
                    pawn?.Faction != Faction.OfPlayer ||
                    pawn.inventory == null
                )
                {
                    continue;
                }

                int available = pawn.inventory.Count(
                    ThingDefOf.Silver
                );

                if (available <= 0)
                {
                    continue;
                }

                int take = Math.Min(available, remaining);

                paymentPlan.Add(
                    new SilverPayment
                    {
                        Holder = pawn,
                        Count = take
                    }
                );
                remaining -= take;

                if (remaining == 0)
                {
                    return true;
                }
            }

            paymentPlan.Clear();
            return false;
        }

        private static bool PaymentPlanIsStillValid(
            List<SilverPayment> paymentPlan,
            int expectedTotal
        )
        {
            int total = 0;

            foreach (SilverPayment payment in paymentPlan)
            {
                if (
                    payment?.Holder?.inventory == null ||
                    payment.Holder.Faction != Faction.OfPlayer ||
                    !payment.Holder.Spawned ||
                    payment.Holder.inventory.Count(ThingDefOf.Silver) <
                        payment.Count
                )
                {
                    return false;
                }

                total += payment.Count;
            }

            return total == expectedTotal;
        }

        private static void ConsumePayment(
            List<SilverPayment> paymentPlan
        )
        {
            foreach (SilverPayment payment in paymentPlan)
            {
                payment.Holder.inventory.RemoveCount(
                    ThingDefOf.Silver,
                    payment.Count,
                    destroy: true
                );
            }
        }
    }
}
