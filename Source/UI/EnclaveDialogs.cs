using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveDialogs
    {
        public static void OpenRecruitmentCandidates(
            PilgrimCamp camp,
            Pawn recruiter
        )
        {
            if (camp == null || recruiter == null)
            {
                Messages.Message(
                    "The enclave recruitment contact is unavailable.",
                    MessageTypeDefOf.RejectInput
                );

                return;
            }

            int storedCount =
                camp.RecruitmentCandidates?.Candidates?.Count ?? 0;

            Log.Message(
                "[IEE] Opened recruitment candidate browser for " +
                (camp.Data?.Name ?? "an enclave") +
                " with Recruiter " +
                recruiter.LabelShort +
                " (" +
                recruiter.GetUniqueLoadID() +
                "); " +
                storedCount +
                " stored candidate(s)."
            );

            Find.WindowStack.Add(
                new Dialog_EnclaveRecruitmentCandidates(
                    camp,
                    recruiter
                )
            );
        }

        public static void OpenSilverDonation(
            PilgrimCamp camp,
            Pawn leader
        )
        {
            string failureReason;

            if (
                !EnclaveDonationService.DonationContactIsValid(
                    camp,
                    leader,
                    out failureReason
                )
            )
            {
                Messages.Message(
                    failureReason,
                    MessageTypeDefOf.RejectInput
                );
                return;
            }

            Log.Message(
                "[IEE] Opened silver donation dialog for " +
                (camp.Data?.Name ?? "an enclave") +
                " with Leader " +
                leader.LabelShort +
                " (" +
                leader.GetUniqueLoadID() +
                ")."
            );

            Find.WindowStack.Add(
                new Dialog_EnclaveSilverDonation(camp, leader)
            );
        }
    }

    public class Dialog_EnclaveSilverDonation : Window
    {
        private readonly PilgrimCamp camp;
        private readonly Pawn leader;

        public override Vector2 InitialSize => new Vector2(620f, 470f);

        public Dialog_EnclaveSilverDonation(
            PilgrimCamp camp,
            Pawn leader
        )
        {
            this.camp = camp;
            this.leader = leader;

            doCloseX = true;
            doCloseButton = true;
            closeOnAccept = false;
            closeOnCancel = true;
            absorbInputAroundWindow = true;
            onlyOneOfTypeAllowed = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(
                new Rect(inRect.x, inRect.y, inRect.width, 32f),
                "Donate Silver to Enclave"
            );

            Text.Font = GameFont.Small;

            string failureReason;
            bool contactValid =
                EnclaveDonationService.DonationContactIsValid(
                    camp,
                    leader,
                    out failureReason
                );
            int reputation = camp?.Data?.Reputation ?? 0;
            bool atMaximum =
                reputation >= EnclaveReputation.Maximum;
            int availableSilver = contactValid
                ? EnclaveDonationService.GetAvailableSilver(camp)
                : 0;

            Widgets.Label(
                new Rect(inRect.x, inRect.y + 38f, inRect.width, 24f),
                "Enclave: " +
                (camp?.Data?.Name ?? "Unavailable") +
                "    Leader: " +
                (leader?.LabelShort ?? "Unavailable")
            );
            Widgets.Label(
                new Rect(inRect.x, inRect.y + 64f, inRect.width, 24f),
                "Reputation: " +
                reputation +
                " — " +
                (camp?.Data?.ReputationTierLabel ?? "Unknown")
            );
            Widgets.Label(
                new Rect(inRect.x, inRect.y + 90f, inRect.width, 24f),
                "Visiting-group silver: " +
                availableSilver.ToString("N0")
            );

            string statusText = contactValid
                ? atMaximum
                    ? "Reputation is already at +100. No silver will " +
                        "be consumed."
                    : "Choose a donation from the visiting caravan's " +
                        "inventories."
                : failureReason;

            Widgets.Label(
                new Rect(inRect.x, inRect.y + 118f, inRect.width, 42f),
                statusText
            );

            float y = inRect.y + 166f;

            foreach (
                EnclaveDonationOption option in
                EnclaveDonationService.Options
            )
            {
                Rect buttonRect = new Rect(
                    inRect.x,
                    y,
                    inRect.width,
                    44f
                );
                bool canAfford =
                    availableSilver >= option.SilverAmount;
                bool enabled =
                    contactValid && !atMaximum && canAfford;
                int effectiveGain = Mathf.Min(
                    option.ReputationGain,
                    Mathf.Max(
                        0,
                        EnclaveReputation.Maximum - reputation
                    )
                );
                string label =
                    "Donate " +
                    option.SilverAmount.ToString("N0") +
                    " silver  —  +" +
                    effectiveGain +
                    " reputation" +
                    (effectiveGain < option.ReputationGain
                        ? " (normally +" +
                            option.ReputationGain +
                            "; capped)"
                        : string.Empty);

                if (
                    Widgets.ButtonText(
                        buttonRect,
                        label,
                        true,
                        true,
                        enabled
                    )
                )
                {
                    string resultMessage;
                    bool donated = EnclaveDonationService.TryDonate(
                        camp,
                        leader,
                        option.SilverAmount,
                        out resultMessage
                    );

                    Messages.Message(
                        resultMessage,
                        donated
                            ? MessageTypeDefOf.PositiveEvent
                            : MessageTypeDefOf.RejectInput
                    );
                }

                TooltipHandler.TipRegion(
                    buttonRect,
                    !contactValid
                        ? failureReason
                        : atMaximum
                            ? "Reputation is already at the +100 cap; " +
                                "no silver will be consumed."
                            : canAfford
                                ? "Consume exactly " +
                                    option.SilverAmount.ToString("N0") +
                                    " silver from the visiting group's " +
                                    "inventories for +" +
                                    effectiveGain +
                                    " reputation. Reputation gains are " +
                                    "clamped at +100."
                                : "Insufficient visiting-group silver: " +
                                    availableSilver.ToString("N0") +
                                    " of " +
                                    option.SilverAmount.ToString("N0") +
                                    " available."
                );

                y += 50f;
            }
        }
    }

    public class Dialog_EnclaveRecruitmentCandidates : Window
    {
        private const float HeaderHeight = 150f;
        private const float CandidateHeight = 190f;
        private const float CandidateSpacing = 10f;

        private readonly PilgrimCamp camp;
        private readonly Pawn recruiter;
        private Vector2 scrollPosition;

        public override Vector2 InitialSize => new Vector2(820f, 720f);

        public Dialog_EnclaveRecruitmentCandidates(
            PilgrimCamp camp,
            Pawn recruiter
        )
        {
            this.camp = camp;
            this.recruiter = recruiter;

            doCloseX = true;
            doCloseButton = true;
            closeOnAccept = false;
            closeOnCancel = true;
            absorbInputAroundWindow = true;
            onlyOneOfTypeAllowed = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(
                new Rect(inRect.x, inRect.y, inRect.width, 32f),
                "Enclave Recruitment Candidates"
            );

            Text.Font = GameFont.Small;

            string enclaveName = camp?.Data?.Name ?? "Unknown enclave";
            string recruiterName =
                recruiter?.LabelShort ?? "Unavailable Recruiter";
            List<Pawn> availableCandidates = GetAvailableCandidates();
            int storedCount =
                camp?.RecruitmentCandidates?.Candidates?.Count ?? 0;
            int unavailableCount =
                Mathf.Max(0, storedCount - availableCandidates.Count);
            string recruitmentUnavailableReason;
            bool recruitmentAvailable =
                EnclaveRecruitmentService.RecruitmentIsAvailable(
                    camp,
                    out recruitmentUnavailableReason
                );
            int discountPercent =
                EnclaveRecruitmentService
                    .GetReputationDiscountPercent(camp);

            Widgets.Label(
                new Rect(inRect.x, inRect.y + 34f, inRect.width, 24f),
                "Enclave: " +
                enclaveName +
                "    Recruiter: " +
                recruiterName
            );
            Widgets.Label(
                new Rect(inRect.x, inRect.y + 57f, inRect.width, 24f),
                "Reputation: " +
                (camp?.Data?.Reputation ?? 0) +
                " — " +
                (camp?.Data?.ReputationTierLabel ?? "Unknown")
            );
            Widgets.Label(
                new Rect(inRect.x, inRect.y + 80f, inRect.width, 42f),
                recruitmentAvailable
                    ? discountPercent > 0
                        ? camp.Data.ReputationTierLabel +
                            " reputation discount: " +
                            discountPercent +
                            "%"
                        : "Recruitment available at the standard cost."
                    : recruitmentUnavailableReason
            );
            Widgets.Label(
                new Rect(inRect.x, inRect.y + 123f, inRect.width, 24f),
                "Available candidates: " +
                availableCandidates.Count +
                (unavailableCount > 0
                    ? " (" + unavailableCount + " unavailable)"
                    : string.Empty)
            );

            Rect outRect = new Rect(
                inRect.x,
                inRect.y + HeaderHeight,
                inRect.width,
                inRect.height - HeaderHeight - 45f
            );

            if (availableCandidates.Count == 0)
            {
                Widgets.Label(
                    outRect,
                    "No recruitment candidates are currently available."
                );

                return;
            }

            float contentHeight =
                availableCandidates.Count *
                (CandidateHeight + CandidateSpacing);
            Rect viewRect = new Rect(
                0f,
                0f,
                outRect.width - 16f,
                Mathf.Max(outRect.height, contentHeight)
            );

            Widgets.BeginScrollView(
                outRect,
                ref scrollPosition,
                viewRect
            );

            float y = 0f;

            foreach (Pawn candidate in availableCandidates)
            {
                DrawCandidate(
                    new Rect(
                        0f,
                        y,
                        viewRect.width,
                        CandidateHeight
                    ),
                    candidate
                );
                y += CandidateHeight + CandidateSpacing;
            }

            Widgets.EndScrollView();
        }

        private List<Pawn> GetAvailableCandidates()
        {
            List<Pawn> available = new List<Pawn>();
            IReadOnlyList<Pawn> stored =
                camp?.RecruitmentCandidates?.Candidates;
            Map map = camp?.Map;

            if (stored == null || map == null)
            {
                return available;
            }

            foreach (Pawn candidate in stored)
            {
                if (
                    candidate != null &&
                    !candidate.Dead &&
                    candidate.Spawned &&
                    candidate.Map == map
                )
                {
                    available.Add(candidate);
                }
            }

            return available;
        }

        private void DrawCandidate(Rect rect, Pawn candidate)
        {
            Widgets.DrawMenuSection(rect);

            Rect content = rect.ContractedBy(10f);
            float buttonWidth = 185f;
            float textWidth = content.width - buttonWidth - 12f;

            Text.Font = GameFont.Medium;
            Widgets.Label(
                new Rect(content.x, content.y, textWidth, 30f),
                candidate.LabelShortCap
            );

            Text.Font = GameFont.Small;

            string health = HealthUtility.GetGeneralConditionLabel(
                candidate,
                true
            );

            if (health.NullOrEmpty())
            {
                health = "Healthy";
            }

            Widgets.Label(
                new Rect(
                    content.x,
                    content.y + 31f,
                    textWidth,
                    24f
                ),
                "Age " +
                candidate.ageTracker.AgeBiologicalYears +
                "    Gender: " +
                candidate.gender +
                "    Health: " +
                health
            );

            Widgets.Label(
                new Rect(
                    content.x,
                    content.y + 60f,
                    textWidth,
                    50f
                ),
                "Relevant skills: " + FormatSkills(candidate)
            );
            Widgets.Label(
                new Rect(
                    content.x,
                    content.y + 116f,
                    textWidth,
                    48f
                ),
                "Traits: " + FormatTraits(candidate)
            );

            Rect infoRect = new Rect(
                content.xMax - buttonWidth,
                content.y + 15f,
                buttonWidth,
                34f
            );

            if (Widgets.ButtonText(infoRect, "Open full pawn info"))
            {
                Find.WindowStack.Add(new Dialog_InfoCard(candidate));
            }

            Rect recruitRect = new Rect(
                content.xMax - buttonWidth,
                content.y + 62f,
                buttonWidth,
                54f
            );

            int cost =
                EnclaveRecruitmentService.GetEffectiveRecruitmentCost(
                    camp,
                    candidate
                );
            int discountPercent =
                EnclaveRecruitmentService
                    .GetReputationDiscountPercent(camp);
            string unavailableReason;
            bool recruitmentAvailable =
                EnclaveRecruitmentService.RecruitmentIsAvailable(
                    camp,
                    out unavailableReason
                );
            int availableSilver =
                EnclaveRecruitmentService.GetAvailableSilver(camp);
            bool canAfford = availableSilver >= cost;
            bool canRecruit = recruitmentAvailable && canAfford;
            string recruitLabel;

            if (!recruitmentAvailable)
            {
                recruitLabel =
                    "Recruitment unavailable\n" +
                    (camp?.Data?.ReputationTierLabel ?? "Unknown") +
                    " reputation";
            }
            else
            {
                recruitLabel =
                    "Recruit — " +
                    cost +
                    " silver" +
                    (!canAfford
                        ? "\nInsufficient silver"
                        : discountPercent > 0
                        ? "\n" +
                            camp.Data.ReputationTierLabel +
                            " discount: " +
                            discountPercent +
                            "%"
                        : string.Empty);
            }

            if (Widgets.ButtonText(
                recruitRect,
                recruitLabel,
                true,
                true,
                canRecruit
            ))
            {
                ConfirmRecruitment(candidate, cost);
            }

            TooltipHandler.TipRegion(
                recruitRect,
                !recruitmentAvailable
                    ? unavailableReason
                    : canAfford
                        ? "Recruit this candidate for " +
                        cost +
                        " silver from the visiting group's inventories." +
                        (discountPercent > 0
                            ? " " +
                                camp.Data.ReputationTierLabel +
                                " reputation discount: " +
                                discountPercent +
                                "%."
                            : string.Empty)
                        : "Insufficient silver: " +
                        availableSilver +
                        " of " +
                        cost +
                        " available in the visiting group's inventories."
            );
        }

        private void ConfirmRecruitment(Pawn candidate, int cost)
        {
            string enclaveName =
                camp?.Data?.Name ?? "the enclave";

            Find.WindowStack.Add(
                Dialog_MessageBox.CreateConfirmation(
                    "Recruit " +
                    candidate.LabelShort +
                    " from " +
                    enclaveName +
                    " for " +
                    cost +
                    " silver?",
                    delegate
                    {
                        string resultMessage;
                        bool recruited =
                            EnclaveRecruitmentService.TryRecruit(
                                camp,
                                candidate,
                                cost,
                                out resultMessage
                            );

                        Messages.Message(
                            resultMessage,
                            recruited
                                ? MessageTypeDefOf.PositiveEvent
                                : MessageTypeDefOf.RejectInput
                        );
                    }
                )
            );
        }

        private static string FormatSkills(Pawn pawn)
        {
            if (pawn?.skills?.skills == null)
            {
                return "Unavailable";
            }

            List<string> skills = pawn.skills.skills
                .Where(skill => skill != null && !skill.TotallyDisabled)
                .OrderByDescending(skill => skill.Level)
                .Take(6)
                .Select(
                    skill =>
                        skill.def.LabelCap +
                        " " +
                        skill.Level +
                        FormatPassion(skill.passion)
                )
                .ToList();

            return skills.Count > 0
                ? string.Join(", ", skills)
                : "None";
        }

        private static string FormatPassion(Passion passion)
        {
            return passion == Passion.None
                ? string.Empty
                : " (" + SkillUI.GetLabel(passion) + ")";
        }

        private static string FormatTraits(Pawn pawn)
        {
            if (pawn?.story?.traits?.allTraits == null)
            {
                return "None";
            }

            List<string> traits = pawn.story.traits.allTraits
                .Where(trait => trait != null && !trait.Suppressed)
                .Select(trait => trait.LabelCap)
                .ToList();

            return traits.Count > 0
                ? string.Join(", ", traits)
                : "None";
        }
    }
}
