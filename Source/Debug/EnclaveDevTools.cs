using System.Collections.Generic;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveDevTools
    {
        public const int TestPopulation = 12;
        public const int TestSilverAmount = 2000;

        public static void CreateQuickTestEnclave()
        {
            if (!Prefs.DevMode)
            {
                return;
            }

            WorldObjectDef def =
                DefDatabase<WorldObjectDef>.GetNamed("IEE_PilgrimCamp");
            PilgrimCamp camp =
                (PilgrimCamp)WorldObjectMaker.MakeWorldObject(def);

            camp.Data = EnclaveGenerator.Generate(TestPopulation);
            camp.Tile = TileFinder.RandomSettlementTileFor(
                Faction.OfPlayer,
                mustBeAutoChoosable: false
            );

            Find.WorldObjects.Add(camp);
            EnclaveWorldTestTools.RegisterCreatedWorldObject(camp);
            Find.WorldSelector.Select(camp);

            LongEventHandler.QueueLongEvent(
                delegate
                {
                    Map map =
                        EnclaveEncounterMapUtility.EnsureMapGenerated(camp);

                    if (map == null)
                    {
                        Messages.Message(
                            "The quick test enclave map could not be generated.",
                            MessageTypeDefOf.RejectInput
                        );
                        return;
                    }

                    CameraJumper.TryJump(map.Center, map);

                    Messages.Message(
                        "Created " +
                        camp.Data.Name +
                        " with " +
                        TestPopulation +
                        " pilgrims and generated its encounter map.",
                        MessageTypeDefOf.PositiveEvent
                    );

                    Log.Message(
                        "[IEE] DEV quick test enclave created: " +
                        camp.Data.Name +
                        " at tile " +
                        camp.Tile +
                        " with population " +
                        camp.Data.Population +
                        "."
                    );
                },
                "GeneratingMap",
                doAsynchronously: false,
                exceptionHandler: null
            );
        }

        public static void GiveTestSilver(PilgrimCamp camp)
        {
            if (!Prefs.DevMode)
            {
                return;
            }

            Map map = camp?.Map;

            if (map == null)
            {
                Messages.Message(
                    camp == null
                        ? "The current map is not a Pilgrim Camp."
                        : "Generate or visit this enclave map before giving test silver.",
                    MessageTypeDefOf.RejectInput
                );
                return;
            }

            Pawn recipient = null;

            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (
                    pawn?.Faction == Faction.OfPlayer &&
                    pawn.inventory != null
                )
                {
                    recipient = pawn;
                    break;
                }
            }

            int remaining = TestSilverAmount;
            int inventoryAmount = 0;
            int groundAmount = 0;

            while (remaining > 0)
            {
                Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
                silver.stackCount = System.Math.Min(
                    remaining,
                    ThingDefOf.Silver.stackLimit
                );

                int stackCount = silver.stackCount;

                if (
                    recipient != null &&
                    recipient.inventory.innerContainer.TryAdd(silver)
                )
                {
                    inventoryAmount += stackCount;
                }
                else
                {
                    IntVec3 dropCell =
                        recipient?.Position ?? map.Center;

                    GenPlace.TryPlaceThing(
                        silver,
                        dropCell,
                        map,
                        ThingPlaceMode.Near
                    );
                    groundAmount += stackCount;
                }

                remaining -= stackCount;
            }

            string result =
                "DEV: Added " +
                TestSilverAmount.ToString("N0") +
                " silver";

            if (inventoryAmount > 0)
            {
                result +=
                    " to " +
                    recipient.LabelShort +
                    "'s inventory";
            }

            if (groundAmount > 0)
            {
                result +=
                    (inventoryAmount > 0 ? " and placed " : "; placed ") +
                    groundAmount.ToString("N0") +
                    " on the enclave map";
            }

            if (recipient != null)
            {
                Messages.Message(
                    result + ".",
                    recipient,
                    MessageTypeDefOf.PositiveEvent
                );
            }
            else
            {
                Messages.Message(
                    result + ".",
                    MessageTypeDefOf.PositiveEvent
                );
            }

            Log.Message("[IEE] " + result + ".");
        }

        public static void ShowTestState(PilgrimCamp camp)
        {
            if (!Prefs.DevMode)
            {
                return;
            }

            if (camp?.Data == null)
            {
                Messages.Message(
                    "The selected Pilgrim Camp has no enclave data.",
                    MessageTypeDefOf.RejectInput
                );
                return;
            }

            StringBuilder report = new StringBuilder();

            report.AppendLine("IDENTITY");
            report.AppendLine("Enclave: " + camp.Data.Name);
            report.AppendLine("World object ID: " + camp.ID);
            report.AppendLine("Population: " + camp.Data.Population);
            report.AppendLine();
            report.AppendLine("IDEOLOGY");
            report.AppendLine(
                "Ideology type: " +
                EnclaveIdeologyUtility.GetTypeLabel(camp.Data)
            );
            report.AppendLine(
                "Actual Ideo: " +
                DescribeIdeo(camp.Data)
            );
            AppendIdeologyAlignment(report, camp);
            report.AppendLine();
            report.AppendLine("DEVELOPMENT");
            report.AppendLine(
                "Development: " +
                EnclaveDevelopmentUtility.GetDisplayName(camp.Data) +
                " (numeric tier " +
                EnclaveDevelopmentUtility.GetNumericTier(camp.Data) +
                ")"
            );
            report.AppendLine(
                "Development initial population: " +
                camp.Data.DevelopmentTierInitialPopulation
            );
            report.AppendLine();
            report.AppendLine("PLAYER RELATIONSHIP");
            report.AppendLine(
                "Reputation: " +
                camp.Data.Reputation +
                " — " +
                camp.Data.ReputationTierLabel
            );
            report.AppendLine(
                "Locally hostile: " +
                EnclaveRelationshipUtility.IsLocallyHostile(camp)
            );
            report.AppendLine(
                "Local combat: " +
                EnclaveLocalHostilityService.GetCombatStateLabel(camp)
            );
            Pawn factionPawn =
                camp.PawnRoles?.GetPawn(EnclavePawnRole.Leader) ??
                camp.PawnRoles?.GetPawn(EnclavePawnRole.Trader) ??
                camp.PawnRoles?.GetPawn(EnclavePawnRole.Recruiter);
            Faction mechanicalFaction = factionPawn?.Faction;
            report.AppendLine(
                "Mechanical faction: " +
                (mechanicalFaction == null
                    ? "Unavailable"
                    : mechanicalFaction.Name +
                        " [" +
                        mechanicalFaction.def.defName +
                        "]" +
                        ", hidden=" +
                        mechanicalFaction.Hidden +
                        ", goodwill=" +
                        mechanicalFaction.HasGoodwill +
                        ", player relation=" +
                        mechanicalFaction.PlayerRelationKind)
            );
            report.AppendLine(
                "Registered enclave pawns: " +
                (camp.PawnMembers?.Members?.Count ?? 0)
            );
            report.AppendLine();
            report.AppendLine("ROLES");
            report.AppendLine(
                "Leader: " +
                DescribePawn(camp.PawnRoles?.GetPawn(EnclavePawnRole.Leader))
            );
            report.AppendLine(
                "Trader: " +
                DescribePawn(camp.PawnRoles?.GetPawn(EnclavePawnRole.Trader))
            );
            report.AppendLine(
                "Recruiter: " +
                DescribePawn(camp.PawnRoles?.GetPawn(EnclavePawnRole.Recruiter))
            );
            report.AppendLine("Recruitment candidates:");

            if (
                camp.RecruitmentCandidates?.Candidates == null ||
                camp.RecruitmentCandidates.Candidates.Count == 0
            )
            {
                report.AppendLine("  None");
            }
            else
            {
                foreach (
                    Pawn candidate in
                    camp.RecruitmentCandidates.Candidates
                )
                {
                    report.AppendLine("  " + DescribePawn(candidate));
                }
            }

            report.AppendLine();
            report.AppendLine("PERSISTENT SYSTEMS");
            report.AppendLine(
                "Layout: " + camp.Data.DescribeLayoutAssignments()
            );
            report.AppendLine(
                "Trader stock grants: " +
                camp.Data.HighestTraderStockTierGranted
            );
            report.AppendLine("Visiting group:");

            if (
                camp.VisitingGroup?.Members == null ||
                camp.VisitingGroup.Members.Count == 0
            )
            {
                report.AppendLine("  None");
            }
            else
            {
                foreach (Pawn member in camp.VisitingGroup.Members)
                {
                    report.AppendLine("  " + DescribePawn(member));
                }
            }

            report.AppendLine();
            AppendNearbyInfluence(report, camp);

            string reportText = report.ToString().TrimEnd();

            Log.Message("[IEE] DEV enclave test state\n" + reportText);
            Find.WindowStack.Add(new Dialog_MessageBox(reportText));
        }

        private static void AppendNearbyInfluence(
            StringBuilder report,
            PilgrimCamp camp
        )
        {
            List<EnclaveNeighborInfo> neighbors =
                EnclaveProximityUtility.GetNearbyNeighbors(camp);
            EnclaveRegionalInfluenceSummary regionalInfluence =
                EnclaveInfluenceUtility.CalculateRegionalSummary(
                    camp,
                    neighbors
                );

            report.AppendLine("WORLD");
            report.AppendLine("Tile: " + camp.Tile);
            report.AppendLine(
                "Regional Status: " +
                regionalInfluence.StatusLabel +
                " (pressure " +
                FormatSignedScore(
                    regionalInfluence.TotalPressure
                ) +
                ")"
            );
            report.AppendLine("Qualifying neighbors:");

            if (neighbors.Count == 0)
            {
                report.AppendLine("  None within 30 tiles");
            }
            else
            {
                foreach (EnclaveNeighborInfo neighbor in neighbors)
                {
                    report.Append("  ");
                    report.Append(neighbor.Label);
                    report.Append(" \u2014 ");
                    report.Append(
                        neighbor.DistanceInTiles.ToString("0.#")
                    );
                    report.Append(" tiles \u2014 ");
                    report.Append(
                        EnclaveProximityUtility
                            .GetDistanceBandDisplayName(
                                neighbor.DistanceBand
                            )
                    );
                    report.Append(" \u2014 ");
                    report.Append(
                        GetNeighborDescription(camp, neighbor)
                    );
                    report.Append(" \u2014 influence ");
                    report.Append(
                        FormatSignedScore(neighbor.Influence.Total)
                    );
                    report.Append(" \u2014 regional pressure ");
                    report.Append(
                        FormatSignedScore(neighbor.RegionalPressure)
                    );
                    report.AppendLine();
                }
            }

            report.AppendLine();
            report.AppendLine("INTER-ENCLAVE");
            bool hasEnclaveNeighbor = false;

            foreach (EnclaveNeighborInfo neighbor in neighbors)
            {
                if (
                    neighbor.NeighborType !=
                        EnclaveNeighborType.Enclave
                )
                {
                    continue;
                }

                hasEnclaveNeighbor = true;
                report.AppendLine(
                    "  " +
                    neighbor.Label +
                    ": " +
                    EnclaveIdeologyCompatibilityUtility.GetDisplayName(
                        neighbor.IdeologyCompatibility
                    ) +
                    " compatibility; " +
                    (neighbor.RelationshipState?.ToString() ??
                        "relationship unavailable") +
                    (neighbor.RelationshipScore.HasValue
                        ? " (" +
                            FormatSignedScore(
                                neighbor.RelationshipScore.Value
                            ) +
                            ")"
                        : string.Empty)
                );
            }

            if (!hasEnclaveNeighbor)
            {
                report.AppendLine("  No nearby enclave relationships");
            }
        }

        private static string GetNeighborDescription(
            PilgrimCamp camp,
            EnclaveNeighborInfo neighbor
        )
        {
            switch (neighbor.NeighborType)
            {
                case EnclaveNeighborType.Enclave:
                    return
                        neighbor.IdeologyType +
                        " \u2014 " +
                        EnclaveIdeologyCompatibilityUtility
                            .GetDisplayName(
                                neighbor.IdeologyCompatibility
                            ) +
                        " \u2014 " +
                        (neighbor.RelationshipState?.ToString() ??
                            "Relationship unavailable") +
                        (neighbor.RelationshipScore.HasValue
                            ? " (" +
                                FormatSignedScore(
                                    neighbor.RelationshipScore.Value
                                ) +
                                ")"
                            : string.Empty);
                case EnclaveNeighborType.PlayerSettlement:
                    return
                        "Player Settlement \u2014 " +
                        (camp.Data?.ReputationTierLabel ?? "Neutral") +
                        " reputation (" +
                        FormatSignedScore(
                            neighbor.Influence.ReputationWeight
                        ) +
                        ")";
                case EnclaveNeighborType.FriendlyFactionSettlement:
                    return "Friendly Faction Settlement";
                case EnclaveNeighborType.HostileFactionSettlement:
                    return "Hostile Faction Settlement";
                default:
                    return "Neutral Faction Settlement";
            }
        }

        private static string FormatSignedScore(int score)
        {
            return score >= 0
                ? "+" + score
                : score.ToString();
        }

        private static void AppendIdeologyAlignment(
            StringBuilder report,
            PilgrimCamp camp
        )
        {
            if (camp.Map == null)
            {
                report.AppendLine(
                    "Pawn ideology alignment: not checked " +
                    "(map not generated)"
                );
                return;
            }

            Ideo intendedIdeo =
                EnclaveIdeologyUtility.GetActualIdeo(camp.Data);

            if (intendedIdeo == null)
            {
                report.AppendLine(
                    "Pawn ideology alignment: unavailable " +
                    "(actual Ideo not established)"
                );
                return;
            }

            int eligibleCount = 0;
            int alignedCount = 0;
            StringBuilder mismatches = new StringBuilder();

            if (camp.PawnMembers?.Members != null)
            {
                foreach (Pawn pawn in camp.PawnMembers.Members)
                {
                    if (
                        pawn == null ||
                        pawn.Destroyed ||
                        pawn.Dead ||
                        pawn.Faction == Faction.OfPlayer ||
                        pawn.RaceProps == null ||
                        !pawn.RaceProps.Humanlike ||
                        pawn.Map != camp.Map
                    )
                    {
                        continue;
                    }

                    eligibleCount++;

                    if (pawn.Ideo == intendedIdeo)
                    {
                        alignedCount++;
                    }
                    else
                    {
                        if (mismatches.Length > 0)
                        {
                            mismatches.Append(", ");
                        }

                        mismatches.Append(DescribePawn(pawn));
                    }
                }
            }

            report.AppendLine(
                "Pawn ideology alignment: " +
                alignedCount +
                "/" +
                eligibleCount +
                " active registered member(s) aligned"
            );

            if (mismatches.Length > 0)
            {
                report.AppendLine(
                    "  Mismatched: " + mismatches
                );
            }
        }

        private static string DescribeIdeo(EnclaveData data)
        {
            Ideo ideo = EnclaveIdeologyUtility.GetActualIdeo(data);

            return ideo == null
                ? "Not yet established"
                : EnclaveIdeologyUtility.GetActualIdeoLabel(data) +
                    " (" +
                    ideo.GetUniqueLoadID() +
                    ")";
        }

        private static string DescribePawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return "Unassigned";
            }

            return pawn.LabelShort + " (" + pawn.GetUniqueLoadID() + ")";
        }
    }
}
