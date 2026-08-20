using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public sealed class Dialog_EnclaveOverview : Window
    {
        private const float SectionSpacing = 10f;
        private const float SectionPadding = 10f;
        private const float SectionHeaderHeight = 30f;
        private const float MinimumLineHeight = 22f;

        private readonly PilgrimCamp camp;
        private Vector2 scrollPosition;

        public override Vector2 InitialSize => new Vector2(860f, 760f);

        public Dialog_EnclaveOverview(PilgrimCamp camp)
        {
            this.camp = camp;

            doCloseX = true;
            doCloseButton = true;
            closeOnAccept = false;
            closeOnCancel = true;
            absorbInputAroundWindow = true;
            onlyOneOfTypeAllowed = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (camp?.Data == null)
            {
                Widgets.Label(
                    inRect,
                    "This enclave's persistent data is unavailable."
                );
                return;
            }

            Text.Font = GameFont.Medium;
            Widgets.Label(
                new Rect(inRect.x, inRect.y, inRect.width, 34f),
                "Enclave Overview \u2014 " + camp.Data.Name
            );

            List<EnclaveNeighborInfo> neighbors =
                EnclaveProximityUtility.GetNearbyNeighbors(camp);
            List<string> identityLines = BuildIdentityLines();
            List<string> leadershipLines = BuildLeadershipLines();
            List<string> playerRelationshipLines =
                BuildPlayerRelationshipLines();
            List<string> localStatusLines = BuildLocalStatusLines();
            List<string> needsLines = BuildNeedsLines();
            List<string> nearbyInfluenceLines =
                BuildNearbyInfluenceLines(neighbors);

            float viewWidth = inRect.width - 16f;
            float contentHeight =
                GetSectionHeight(viewWidth, identityLines) +
                GetSectionHeight(viewWidth, leadershipLines) +
                GetSectionHeight(viewWidth, playerRelationshipLines) +
                GetSectionHeight(viewWidth, localStatusLines) +
                GetSectionHeight(viewWidth, needsLines) +
                GetSectionHeight(viewWidth, nearbyInfluenceLines) +
                SectionSpacing * 5f;
            Rect outRect = new Rect(
                inRect.x,
                inRect.y + 40f,
                inRect.width,
                inRect.height - 85f
            );
            Rect viewRect = new Rect(
                0f,
                0f,
                viewWidth,
                Mathf.Max(outRect.height, contentHeight)
            );

            Widgets.BeginScrollView(
                outRect,
                ref scrollPosition,
                viewRect
            );

            float y = 0f;

            DrawSection(
                ref y,
                viewWidth,
                "Identity",
                identityLines
            );
            DrawSection(
                ref y,
                viewWidth,
                "Leadership",
                leadershipLines
            );
            DrawSection(
                ref y,
                viewWidth,
                "Player Relationship",
                playerRelationshipLines
            );
            DrawSection(
                ref y,
                viewWidth,
                "Local Status",
                localStatusLines
            );
            DrawSection(
                ref y,
                viewWidth,
                "Needs",
                needsLines
            );
            DrawSection(
                ref y,
                viewWidth,
                "Nearby Influence",
                nearbyInfluenceLines,
                addTrailingSpacing: false
            );

            Widgets.EndScrollView();
            Text.Font = GameFont.Small;
        }

        private List<string> BuildIdentityLines()
        {
            return new List<string>
            {
                "Name: " + camp.Data.Name,
                "Population: " + camp.Data.Population,
                "Development: " +
                    EnclaveDevelopmentUtility.GetDisplayName(camp.Data),
                "Development profile: " +
                    EnclaveDevelopmentUtility.GetDescription(camp.Data),
                "Ideology: " +
                    EnclaveIdeologyUtility.GetActualIdeoLabel(camp.Data),
                "Ideology type: " +
                    EnclaveIdeologyUtility.GetTypeLabel(camp.Data)
            };
        }

        private List<string> BuildLeadershipLines()
        {
            return new List<string>
            {
                "Leader: " + DescribeLeader(),
                "Trader: " + DescribeRole(EnclavePawnRole.Trader),
                "Recruiter: " +
                    DescribeRole(EnclavePawnRole.Recruiter),
                "Recruitment candidates: " +
                    (camp.RecruitmentCandidates?.Candidates?.Count ?? 0)
            };
        }

        private List<string> BuildPlayerRelationshipLines()
        {
            string recruitmentUnavailableReason;
            bool recruitmentAvailable =
                EnclaveRecruitmentService.RecruitmentIsAvailable(
                    camp,
                    out recruitmentUnavailableReason
                );
            int recruitmentDiscount =
                EnclaveRecruitmentService
                    .GetReputationDiscountPercent(camp);
            string tradeUnavailableReason;
            bool tradingAvailable =
                EnclaveTradeService.TradingIsAvailable(
                    camp,
                    out tradeUnavailableReason
                );
            int tradeBonus =
                EnclaveTradeService.GetTradeBonusPercent(camp);
            EnclaveTraderStockGrantTier stockTier =
                EnclaveTraderStockService.GetGrantTierForReputation(
                    camp.Data.ReputationTier
                );

            return new List<string>
            {
                "Reputation: " +
                    camp.Data.Reputation +
                    " \u2014 " +
                    camp.Data.ReputationTierLabel,
                recruitmentAvailable
                    ? recruitmentDiscount > 0
                        ? "Recruitment: Available \u2014 " +
                            recruitmentDiscount +
                            "% reputation discount"
                        : "Recruitment: Available \u2014 normal cost"
                    : recruitmentUnavailableReason ??
                        "Recruitment is unavailable.",
                tradingAvailable
                    ? tradeBonus > 0
                        ? "Trading: Available \u2014 " +
                            tradeBonus +
                            "% player-favorable price benefit"
                        : "Trading: Available \u2014 normal prices"
                    : tradeUnavailableReason ??
                        "Trading is unavailable.",
                GetTraderStockBenefitText(
                    tradingAvailable,
                    stockTier
                )
            };
        }

        private List<string> BuildLocalStatusLines()
        {
            bool locallyHostile =
                EnclaveRelationshipUtility.IsLocallyHostile(camp);
            List<string> lines = new List<string>
            {
                locallyHostile
                    ? "Local Status: Hostile"
                    : "Local Status: Peaceful"
            };

            if (locallyHostile)
            {
                lines.Add(
                    "Members of this enclave will attack visiting " +
                    "colonists and caravan animals."
                );
            }

            return lines;
        }

        private List<string> BuildNeedsLines()
        {
            List<EnclaveNeedRecord> shortages =
                EnclaveNeedsUtility.GetShortages(camp);
            List<string> lines = new List<string>();

            if (shortages.Count == 0)
            {
                lines.Add("No significant shortages");
            }
            else
            {
                foreach (
                    EnclaveNeedRecord need in
                    EnclaveNeedsUtility.GetNeeds(camp)
                )
                {
                    lines.Add(
                        EnclaveNeedsUtility.GetNeedLabel(need.Type) +
                        " \u2014 " +
                        need.Severity
                    );
                }
            }

            EnclaveQuestRequest request =
                EnclaveQuestService.GetActiveSupplyRequest(camp);

            if (request != null)
            {
                lines.Add(
                    "Active Supply Request: " +
                    request.RequestedQuantity +
                    " " +
                    request.RequestedThingDef.label +
                    " \u2014 +" +
                    request.ReputationReward +
                    " reputation"
                );
            }

            return lines;
        }

        private List<string> BuildNearbyInfluenceLines(
            List<EnclaveNeighborInfo> neighbors
        )
        {
            EnclaveRegionalInfluenceSummary regionalInfluence =
                EnclaveInfluenceUtility.CalculateRegionalSummary(
                    camp,
                    neighbors
                );
            List<string> lines = new List<string>
            {
                "Regional Status: " +
                    regionalInfluence.StatusLabel +
                    " (pressure " +
                    FormatSignedScore(
                        regionalInfluence.TotalPressure
                    ) +
                    ")"
            };

            if (neighbors.Count == 0)
            {
                lines.Add("No qualifying neighbors within 30 tiles.");
                return lines;
            }

            foreach (EnclaveNeighborInfo neighbor in neighbors)
            {
                string line =
                    neighbor.Label +
                    " \u2014 " +
                    GetNeighborTypeDisplayName(neighbor.NeighborType) +
                    " \u2014 " +
                    neighbor.DistanceInTiles.ToString("0.#") +
                    " tiles \u2014 " +
                    EnclaveProximityUtility
                        .GetDistanceBandDisplayName(
                            neighbor.DistanceBand
                        ) +
                    " \u2014 Influence " +
                    FormatSignedScore(neighbor.Influence.Total) +
                    " \u2014 Regional pressure " +
                    FormatSignedScore(neighbor.RegionalPressure);

                if (neighbor.NeighborType == EnclaveNeighborType.Enclave)
                {
                    line +=
                        "\n    Ideology: " +
                        neighbor.IdeologyType +
                        " \u2014 " +
                        EnclaveIdeologyCompatibilityUtility
                            .GetDisplayName(
                                neighbor.IdeologyCompatibility
                            ) +
                        "; Relationship: " +
                        (neighbor.RelationshipState?.ToString() ??
                            "Unavailable") +
                        (neighbor.RelationshipScore.HasValue
                            ? " (" +
                                FormatSignedScore(
                                    neighbor.RelationshipScore.Value
                                ) +
                                ")"
                            : string.Empty);
                }

                lines.Add(line);
            }

            return lines;
        }

        private string DescribeLeader()
        {
            Pawn leader = camp.PawnRoles?.GetPawn(
                EnclavePawnRole.Leader
            );

            return leader != null
                ? leader.LabelShort
                : (camp.Data.Leader ?? "Unassigned") +
                    " (persistent identity; pawn not generated)";
        }

        private string DescribeRole(EnclavePawnRole role)
        {
            Pawn pawn = camp.PawnRoles?.GetPawn(role);

            return pawn?.LabelShort ?? "Not yet assigned";
        }

        private static string GetTraderStockBenefitText(
            bool tradingAvailable,
            EnclaveTraderStockGrantTier stockTier
        )
        {
            if (!tradingAvailable)
            {
                return "Trader stock access: Unavailable while trading " +
                    "is blocked";
            }

            return stockTier == EnclaveTraderStockGrantTier.None
                ? "Trader stock: Base stock only"
                : "Trader stock: " +
                    stockTier +
                    " reputation additions available";
        }

        private static string GetNeighborTypeDisplayName(
            EnclaveNeighborType neighborType
        )
        {
            switch (neighborType)
            {
                case EnclaveNeighborType.PlayerSettlement:
                    return "Player Colony";
                case EnclaveNeighborType.Enclave:
                    return "Enclave";
                case EnclaveNeighborType.FriendlyFactionSettlement:
                    return "Friendly Settlement";
                case EnclaveNeighborType.HostileFactionSettlement:
                    return "Hostile Settlement";
                default:
                    return "Neutral Settlement";
            }
        }

        private static string FormatSignedScore(int score)
        {
            return score >= 0
                ? "+" + score
                : score.ToString();
        }

        private static float GetSectionHeight(
            float width,
            List<string> lines
        )
        {
            Text.Font = GameFont.Small;
            float contentWidth = width - SectionPadding * 2f;
            float height =
                SectionPadding * 2f + SectionHeaderHeight;

            foreach (string line in lines)
            {
                height += Mathf.Max(
                    MinimumLineHeight,
                    Text.CalcHeight(line, contentWidth)
                );
            }

            return height;
        }

        private static void DrawSection(
            ref float y,
            float width,
            string title,
            List<string> lines,
            bool addTrailingSpacing = true
        )
        {
            float sectionHeight = GetSectionHeight(width, lines);
            Rect sectionRect = new Rect(
                0f,
                y,
                width,
                sectionHeight
            );

            Widgets.DrawMenuSection(sectionRect);

            Text.Font = GameFont.Medium;
            Widgets.Label(
                new Rect(
                    SectionPadding,
                    y + SectionPadding,
                    width - SectionPadding * 2f,
                    SectionHeaderHeight
                ),
                title
            );

            Text.Font = GameFont.Small;
            float lineY =
                y + SectionPadding + SectionHeaderHeight;
            float contentWidth = width - SectionPadding * 2f;

            foreach (string line in lines)
            {
                float lineHeight = Mathf.Max(
                    MinimumLineHeight,
                    Text.CalcHeight(line, contentWidth)
                );

                Widgets.Label(
                    new Rect(
                        SectionPadding,
                        lineY,
                        contentWidth,
                        lineHeight
                    ),
                    line
                );
                lineY += lineHeight;
            }

            y += sectionHeight;

            if (addTrailingSpacing)
            {
                y += SectionSpacing;
            }
        }
    }
}
