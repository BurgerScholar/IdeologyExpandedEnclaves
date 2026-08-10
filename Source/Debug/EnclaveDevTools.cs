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

            camp.Data = EnclaveGenerator.Generate();
            camp.Data.Population = TestPopulation;
            camp.Tile = TileFinder.RandomSettlementTileFor(
                Faction.OfPlayer,
                mustBeAutoChoosable: false
            );

            Find.WorldObjects.Add(camp);
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

            report.AppendLine("Enclave: " + camp.Data.Name);
            report.AppendLine("Population: " + camp.Data.Population);
            report.AppendLine(
                "Reputation: " +
                camp.Data.Reputation +
                " — " +
                camp.Data.ReputationTierLabel
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

            string reportText = report.ToString().TrimEnd();

            Log.Message("[IEE] DEV enclave test state\n" + reportText);
            Find.WindowStack.Add(new Dialog_MessageBox(reportText));
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
