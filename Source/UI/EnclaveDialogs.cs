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
    }

    public class Dialog_EnclaveRecruitmentCandidates : Window
    {
        private const float HeaderHeight = 82f;
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

            Widgets.Label(
                new Rect(inRect.x, inRect.y + 34f, inRect.width, 24f),
                "Enclave: " +
                enclaveName +
                "    Recruiter: " +
                recruiterName
            );
            Widgets.Label(
                new Rect(inRect.x, inRect.y + 57f, inRect.width, 24f),
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

        private static void DrawCandidate(Rect rect, Pawn candidate)
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
                44f
            );

            Widgets.ButtonText(
                recruitRect,
                "Recruit\n(coming soon)",
                true,
                true,
                false
            );
            TooltipHandler.TipRegion(
                recruitRect,
                "Recruitment requirements will be added in the next milestone."
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
