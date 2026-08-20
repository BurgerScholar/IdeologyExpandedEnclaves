using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveExpeditionDevTools
    {
        private sealed class PlayerThingSnapshot
        {
            public Thing Thing;
            public int StackCount;
        }

        private sealed class ExpirationTestSnapshot
        {
            public PilgrimCamp SourceCamp;
            public int ExpeditionId;
            public int SiteWorldObjectId;
            public int SimulatedAtTick;
            public int SourcePopulation;
            public Pawn Leader;
            public Pawn Trader;
            public Pawn Recruiter;
            public Map OccupiedMap;
            public bool WasOccupied;
            public readonly List<Pawn> PlayerPawns =
                new List<Pawn>();
            public readonly List<PlayerThingSnapshot> PlayerThings =
                new List<PlayerThingSnapshot>();
        }

        private sealed class ColonyVisitTestSnapshot
        {
            public int SourceCampId;
            public int ExpeditionId;
            public int SourcePopulation;
            public string LeaderId;
            public string HomeTraderId;
            public string RecruiterId;
            public int DestinationMapId = -1;
            public int VisitStartedTick;
            public readonly List<ThingIdentitySnapshot>
                HomeTraderInventory =
                    new List<ThingIdentitySnapshot>();
        }

        private sealed class ThingIdentitySnapshot
        {
            public string LoadId;
            public int StackCount;
        }

        private static readonly Dictionary<int, ExpirationTestSnapshot>
            expirationTestSnapshots =
                new Dictionary<int, ExpirationTestSnapshot>();
        private static readonly Dictionary<int, ColonyVisitTestSnapshot>
            colonyVisitTestSnapshots =
                new Dictionary<int, ColonyVisitTestSnapshot>();

        public static void ShowMenu(PilgrimCamp camp)
        {
            if (!CanUse(camp))
            {
                return;
            }

            Find.WindowStack.Add(
                new FloatMenu(
                    new List<FloatMenuOption>
                    {
                        new FloatMenuOption(
                            "Show Expedition State",
                            delegate { ShowState(camp); }
                        ),
                        new FloatMenuOption(
                            "Force Expedition Generation",
                            delegate { ForceGeneration(camp); }
                        ),
                        new FloatMenuOption(
                            "Simulate Expedition Expiration Now",
                            delegate { ForceExpiration(camp); }
                        ),
                        new FloatMenuOption(
                            "Jump to Expedition Site",
                            delegate { JumpToSite(camp); }
                        ),
                        new FloatMenuOption(
                            "Validate Expedition Invariants",
                            delegate { ValidateInvariants(camp); }
                        ),
                        new FloatMenuOption(
                            "Show Colony Visit Eligibility",
                            delegate { ShowColonyVisitEligibility(camp); }
                        ),
                        new FloatMenuOption(
                            "Force Colony Visit",
                            delegate { ForceColonyVisit(camp); }
                        ),
                        new FloatMenuOption(
                            "Force Colony Visit Departure",
                            delegate { ForceColonyVisitDeparture(camp); }
                        ),
                        new FloatMenuOption(
                            "Show Colony Visit State",
                            delegate { ShowColonyVisitState(camp); }
                        ),
                        new FloatMenuOption(
                            "Validate Colony Visit Invariants",
                            delegate { ValidateColonyVisitInvariants(camp); }
                        ),
                        new FloatMenuOption(
                            "Simulate Colony Visit Departure Now",
                            delegate { ForceColonyVisitDeparture(camp); }
                        )
                    }
                )
            );
        }

        private static void ShowState(PilgrimCamp camp)
        {
            EnclaveExpeditionRecord record = camp.Data.Expedition;
            EnclaveExpeditionSite site =
                EnclaveExpeditionService.GetActiveSite(camp);
            EnclaveRegionalInfluenceSummary regional =
                EnclaveInfluenceUtility.CalculateRegionalSummary(camp);
            StringBuilder report = new StringBuilder();

            report.AppendLine("EXPEDITION STATE");
            report.AppendLine("Source: " + camp.Data.Name);
            report.AppendLine(
                "Archetype: " +
                EnclaveArchetypeUtility.GetDisplayName(camp.Data)
            );
            report.AppendLine(
                "Purpose: " +
                EnclaveExpeditionUtility.GetPurposeLabel(
                    EnclaveExpeditionUtility.GetPurpose(camp.Data)
                )
            );
            report.AppendLine(
                "Development: " +
                EnclaveDevelopmentUtility.GetDisplayName(camp.Data)
            );
            report.AppendLine(
                "Regional status: " + regional.StatusLabel
            );
            report.AppendLine(
                "Daily generation chance: " +
                EnclaveExpeditionUtility
                    .GetGenerationChancePercent(camp, regional.Status) +
                "%"
            );
            report.AppendLine(
                "Cooldown: " +
                EnclaveExpeditionUtility.GetCooldownTicks(camp.Data) /
                    60000 +
                " days; next eligible tick " +
                camp.Data.NextExpeditionEligibleTick +
                "; remaining " +
                FormatCooldownRemaining(camp.Data)
            );

            if (record == null)
            {
                report.AppendLine("Record: None");
            }
            else
            {
                report.AppendLine(
                    "Record: " +
                    record.State +
                    "; outcome " +
                    record.Outcome +
                    "; expedition " +
                    record.ExpeditionId +
                    "; site " +
                    record.SiteWorldObjectId +
                    "; destination " +
                    record.DestinationWorldObjectId +
                    "/map " +
                    record.DestinationMapId
                );
                report.AppendLine(
                    "Creation/expiration ticks: " +
                    record.CreationTick +
                    " / " +
                    record.ExpirationTick
                );
            }

            report.AppendLine(
                "Active site: " +
                (site == null
                    ? "None"
                    : site.Label +
                        " at tile " +
                        site.Tile +
                        "; map " +
                        (site.HasMap ? "generated" : "not generated") +
                        "; party " +
                        (site.ExpeditionMembers?.Members?.Count ?? 0) +
                        "; pending expiration " +
                        site.PendingExpiration)
            );

            ShowReport(report.ToString().TrimEnd());
        }

        private static void ForceGeneration(PilgrimCamp camp)
        {
            expirationTestSnapshots.Remove(camp.ID);

            EnclaveExpeditionSite site;
            string reason;
            EnclaveColonyVisitRecord ignoredVisit;
            bool generated =
                EnclaveExpeditionService.TryGenerateForDevelopment(
                camp,
                Find.TickManager?.TicksGame ?? 0,
                EnclaveExpeditionOutcome.TemporarySite,
                out site,
                out ignoredVisit,
                out reason
            );

            if (!generated)
            {
                Messages.Message(
                    reason ?? "Expedition generation failed.",
                    MessageTypeDefOf.RejectInput
                );
                return;
            }

            Find.WorldSelector.Select(site);
            CameraJumper.TryJump(site.Tile);
            Messages.Message(
                "Generated " +
                site.Label +
                " through the production expedition service. Source " +
                "population remains " +
                camp.Data.Population +
                ".",
                MessageTypeDefOf.PositiveEvent
            );
        }

        private static void ForceExpiration(PilgrimCamp camp)
        {
            EnclaveExpeditionSite site =
                EnclaveExpeditionService.GetActiveSite(camp);

            if (site == null)
            {
                Messages.Message(
                    "This enclave has no active expedition site.",
                    MessageTypeDefOf.RejectInput
                );
                return;
            }

            expirationTestSnapshots[camp.ID] =
                CaptureExpirationSnapshot(camp, site);

            string result;
            bool expired = EnclaveExpeditionService
                .TrySimulateExpirationNow(camp, out result);

            if (!expired)
            {
                expirationTestSnapshots.Remove(camp.ID);
            }

            Messages.Message(
                result,
                expired
                    ? MessageTypeDefOf.PositiveEvent
                    : MessageTypeDefOf.RejectInput
            );
        }

        private static void JumpToSite(PilgrimCamp camp)
        {
            EnclaveExpeditionSite site =
                EnclaveExpeditionService.GetActiveSite(camp);

            if (site == null)
            {
                Messages.Message(
                    "This enclave has no active expedition site.",
                    MessageTypeDefOf.RejectInput
                );
                return;
            }

            Find.WorldSelector.Select(site);
            CameraJumper.TryJump(site.Tile);
        }

        private static void ShowColonyVisitEligibility(
            PilgrimCamp camp
        )
        {
            Settlement destination;
            float distance;
            bool eligible = EnclaveColonyVisitService
                .TryFindEligibleDestination(
                    camp,
                    out destination,
                    out distance
                );
            int chance = eligible
                ? EnclaveExpeditionUtility
                    .GetColonyVisitChancePercent(
                        camp.Data,
                        distance
                    )
                : 0;
            StringBuilder report = new StringBuilder();

            report.AppendLine("COLONY VISIT ELIGIBILITY");
            report.AppendLine("Source: " + camp.Data.Name);
            report.AppendLine(
                "Reputation: " +
                camp.Data.Reputation +
                " — " +
                camp.Data.ReputationTierLabel
            );
            report.AppendLine(
                "Archetype: " +
                EnclaveArchetypeUtility.GetDisplayName(camp.Data)
            );
            report.AppendLine(
                "Visit type: " +
                EnclaveExpeditionUtility.GetColonyVisitTypeLabel(
                    EnclaveExpeditionUtility.GetPurpose(camp.Data)
                )
            );

            if (eligible)
            {
                report.AppendLine(
                    "Nearest colony: " + destination.Label
                );
                report.AppendLine(
                    "Distance: " +
                    distance.ToString("0.#") +
                    " tiles (" +
                    EnclaveProximityUtility.GetDistanceBand(
                        distance
                    ) +
                    ")"
                );
            }
            else
            {
                report.AppendLine(
                    "Nearest colony: None within 30 tiles"
                );
            }

            report.AppendLine(
                "Site vs visit chance: " + chance + "% visit"
            );
            ShowReport(report.ToString().TrimEnd());
        }

        private static void ForceColonyVisit(PilgrimCamp camp)
        {
            ColonyVisitTestSnapshot snapshot =
                CaptureColonyVisitSnapshot(camp);
            EnclaveExpeditionSite ignoredSite;
            EnclaveColonyVisitRecord visit;
            string reason;
            bool generated =
                EnclaveExpeditionService.TryGenerateForDevelopment(
                    camp,
                    Find.TickManager?.TicksGame ?? 0,
                    EnclaveExpeditionOutcome.ColonyVisit,
                    out ignoredSite,
                    out visit,
                    out reason
                );

            if (!generated || visit == null)
            {
                Messages.Message(
                    reason ?? "Colony visit generation failed.",
                    MessageTypeDefOf.RejectInput
                );
                return;
            }

            snapshot.ExpeditionId = visit.ExpeditionId;
            snapshot.DestinationMapId =
                visit.Destination?.Map?.uniqueID ?? -1;
            snapshot.VisitStartedTick = visit.StartTick;
            colonyVisitTestSnapshots[camp.ID] = snapshot;

            Map destinationMap = visit.Destination?.Map;

            if (destinationMap != null)
            {
                Pawn firstVisitor = visit.Visitors.Count > 0
                    ? visit.Visitors[0]
                    : null;

                CameraJumper.TryJump(
                    firstVisitor?.Position ??
                        destinationMap.Center,
                    destinationMap
                );
            }

            Messages.Message(
                "Generated " +
                EnclaveExpeditionUtility.GetColonyVisitTypeLabel(
                    visit.Purpose
                ) +
                " at " +
                (visit.Destination?.Label ?? "the colony") +
                " through the production expedition service.",
                MessageTypeDefOf.PositiveEvent
            );
        }

        private static void ForceColonyVisitDeparture(
            PilgrimCamp camp
        )
        {
            string result;
            bool started = EnclaveColonyVisitService
                .TryBeginDeparture(camp, out result);

            Messages.Message(
                result,
                started
                    ? MessageTypeDefOf.PositiveEvent
                    : MessageTypeDefOf.RejectInput
            );
        }

        private static void ShowColonyVisitState(PilgrimCamp camp)
        {
            EnclaveExpeditionRecord expedition = camp.Data.Expedition;
            EnclaveColonyVisitRecord visit =
                EnclaveExpeditionService.GetActiveColonyVisit(camp);
            StringBuilder report = new StringBuilder();

            report.AppendLine("COLONY VISIT STATE");
            report.AppendLine("Source: " + camp.Data.Name);

            if (visit == null)
            {
                report.AppendLine("Active visit: None");
                report.AppendLine(
                    "Expedition record: " +
                    (expedition == null
                        ? "None"
                        : expedition.State.ToString())
                );
                ShowReport(report.ToString().TrimEnd());
                return;
            }

            report.AppendLine(
                "Expedition ID: " + visit.ExpeditionId
            );
            report.AppendLine(
                "Destination: " +
                (visit.Destination?.Label ?? "Unavailable")
            );
            report.AppendLine(
                "Visit type: " +
                EnclaveExpeditionUtility.GetColonyVisitTypeLabel(
                    visit.Purpose
                )
            );
            report.AppendLine(
                "Visitors: " + visit.Visitors.Count
            );
            report.AppendLine(
                "Trader: " +
                (visit.Trader == null
                    ? "Unavailable"
                    : visit.Trader.LabelShort +
                        " (" +
                        visit.Trader.GetUniqueLoadID() +
                        ")")
            );
            report.AppendLine(
                "Departure tick: " + visit.DepartureTick
            );
            report.AppendLine("Lifecycle: " + visit.State);
            report.AppendLine(
                "Source population: " +
                visit.SourcePopulationAtStart +
                " before / " +
                camp.Data.Population +
                " current"
            );
            ShowReport(report.ToString().TrimEnd());
        }

        private static void ValidateColonyVisitInvariants(
            PilgrimCamp camp
        )
        {
            List<string> failures = new List<string>();
            int checks = 0;
            EnclaveExpeditionRecord expedition = camp.Data.Expedition;
            EnclaveColonyVisitRecord visit =
                EnclaveExpeditionService.GetActiveColonyVisit(camp);
            ColonyVisitTestSnapshot snapshot;

            colonyVisitTestSnapshots.TryGetValue(
                camp.ID,
                out snapshot
            );

            if (snapshot != null && snapshot.SourceCampId != camp.ID)
            {
                colonyVisitTestSnapshots.Remove(camp.ID);
                snapshot = null;
            }

            CheckColonyVisitFormulaInvariants(ref checks, failures);

            if (visit != null)
            {
                Ideo sourceIdeo =
                    EnclaveIdeologyUtility.GetActualIdeo(camp.Data);
                Check(
                    ++checks,
                    expedition?.IsColonyVisit == true &&
                        expedition.ExpeditionId == visit.ExpeditionId,
                    "source expedition outcome is Colony Visit",
                    failures
                );
                Check(
                    ++checks,
                    visit.Purpose ==
                        EnclaveExpeditionUtility.GetPurpose(camp.Data),
                    "archetype maps to the correct visit type",
                    failures
                );
                Check(
                    ++checks,
                    visit.Destination?.Map != null &&
                        visit.Destination.Map.IsPlayerHome &&
                        expedition.DestinationWorldObjectId ==
                            visit.Destination.ID &&
                        expedition.DestinationMapId ==
                            visit.Destination.Map.uniqueID,
                    "destination and persistent IDs correspond",
                    failures
                );
                Check(
                    ++checks,
                    AllVisitorsUseSourceIdentity(
                        camp,
                        visit,
                        sourceIdeo
                    ),
                    "temporary visitors link to the source and its Ideo",
                    failures
                );
                Check(
                    ++checks,
                    NoVisitorIsAHomeMember(camp, visit),
                    "temporary visitors are not home members",
                    failures
                );
                Check(
                    ++checks,
                    camp.Data.Population ==
                        visit.SourcePopulationAtStart,
                    "source population is unchanged",
                    failures
                );
                Check(
                    ++checks,
                    !visit.ContainsVisitor(
                        camp.PawnRoles?.GetPawn(
                            EnclavePawnRole.Leader
                        )
                    ) &&
                        !visit.ContainsVisitor(
                            camp.PawnRoles?.GetPawn(
                                EnclavePawnRole.Trader
                            )
                        ) &&
                        !visit.ContainsVisitor(
                            camp.PawnRoles?.GetPawn(
                                EnclavePawnRole.Recruiter
                            )
                        ),
                    "home role pawns were not reused by the visit",
                    failures
                );
                Check(
                    ++checks,
                    visit.Trader != null &&
                        CountVisitorReference(
                            visit,
                            visit.Trader
                        ) == 1 &&
                        visit.Trader.trader?.traderKind != null,
                    "exactly one initialized temporary Trader exists",
                    failures
                );
                Check(
                    ++checks,
                    visit.Trader?.inventory != null &&
                        visit.Trader.inventory.innerContainer.Count > 0 &&
                        visit.Trader !=
                            camp.PawnRoles?.GetPawn(
                                EnclavePawnRole.Trader
                            ),
                    "temporary Trader stock is independent",
                    failures
                );
                Check(
                    ++checks,
                    visit.DepartureTick - visit.StartTick ==
                        EnclaveExpeditionUtility
                            .GetColonyVisitDurationTicks(
                                visit.Purpose
                            ) &&
                        visit.DepartureTick > visit.StartTick,
                    "planned departure tick is valid and stable",
                    failures
                );
                Check(
                    ++checks,
                    expedition.IsActive &&
                        EnclaveExpeditionService
                            .GetActiveSite(camp) == null &&
                        EnclaveColonyVisitService
                            .FindVisitsForSource(camp).Count == 1,
                    "one-active-expedition guard spans both outcomes",
                    failures
                );
                ValidateVisitSnapshot(
                    camp,
                    snapshot,
                    ref checks,
                    failures,
                    active: true
                );
            }
            else if (snapshot != null)
            {
                Check(
                    ++checks,
                    expedition?.IsActive != true,
                    "expedition is inactive after departure",
                    failures
                );
                Check(
                    ++checks,
                    camp.Data.NextExpeditionEligibleTick >
                        snapshot.VisitStartedTick,
                    "normal expedition cooldown has started",
                    failures
                );
                ValidateVisitSnapshot(
                    camp,
                    snapshot,
                    ref checks,
                    failures,
                    active: false
                );
            }

            bool passed = failures.Count == 0;

            if (passed && visit == null && snapshot != null)
            {
                colonyVisitTestSnapshots.Remove(camp.ID);
            }

            StringBuilder report = new StringBuilder();
            report.AppendLine(
                "Colony Visit Invariants — " +
                (passed ? "PASS" : "FAIL")
            );
            report.AppendLine(
                passed
                    ? checks + "/" + checks + " checks passed"
                    : failures.Count +
                        " failure(s) across " +
                        checks +
                        " checks"
            );

            foreach (string failure in failures)
            {
                report.AppendLine("✗ " + failure);
            }

            ShowReport(report.ToString().TrimEnd());
        }

        private static void ValidateInvariants(PilgrimCamp camp)
        {
            EnclaveExpeditionRecord record = camp.Data.Expedition;
            EnclaveExpeditionSite site =
                EnclaveExpeditionService.GetActiveSite(camp);
            List<EnclaveExpeditionSite> allSites =
                GetSitesForSource(camp);
            List<string> failures = new List<string>();
            int checks = 0;
            bool completedExpirationSnapshot = false;

            Check(
                ++checks,
                allSites.Count <= 1,
                "maximum one active site",
                failures
            );

            if (record?.IsTemporarySite == true)
            {
                Check(
                    ++checks,
                    site != null && site.SourceCamp == camp,
                    "site references its source enclave",
                    failures
                );
                Check(
                    ++checks,
                    record.Purpose ==
                        EnclaveExpeditionUtility.GetPurpose(camp.Data) &&
                    site?.Purpose == record.Purpose,
                    "purpose matches the persistent archetype",
                    failures
                );
                Check(
                    ++checks,
                    site != null &&
                    site.ID == record.SiteWorldObjectId &&
                    site.ExpeditionId == record.ExpeditionId,
                    "site and record IDs correspond",
                    failures
                );
                Check(
                    ++checks,
                    record.ExpirationTick > record.CreationTick &&
                    record.ExpirationTick - record.CreationTick ==
                        EnclaveExpeditionUtility.GetDurationTicks(
                            record.Purpose
                        ),
                    "duration and expiration are valid",
                    failures
                );
                Check(
                    ++checks,
                    TemporaryMembersAreSeparate(camp, site),
                    "temporary pawns are not home-camp members",
                    failures
                );
                Check(
                    ++checks,
                    site?.ExpeditionMembers?.Members?.Count <=
                        EnclaveExpeditionUtility.MaximumPartySize,
                    "temporary party respects the hard cap",
                    failures
                );
            }
            else if (record?.IsColonyVisit == true)
            {
                Check(
                    ++checks,
                    site == null && allSites.Count == 0 &&
                        EnclaveExpeditionService
                            .GetActiveColonyVisit(camp) != null,
                    "colony visit is the sole active expedition outcome",
                    failures
                );
            }
            else
            {
                Check(
                    ++checks,
                    site == null && allSites.Count == 0,
                    "inactive record has no active site",
                    failures
                );
            }

            ExpirationTestSnapshot expirationSnapshot =
                GetExpirationTestSnapshot(camp);

            if (expirationSnapshot != null)
            {
                EnclaveExpeditionSite originalSite = allSites.Find(
                    candidate =>
                        candidate.ID ==
                            expirationSnapshot.SiteWorldObjectId &&
                        candidate.ExpeditionId ==
                            expirationSnapshot.ExpeditionId
                );

                if (originalSite != null)
                {
                    Check(
                        ++checks,
                        expirationSnapshot.WasOccupied &&
                            originalSite.HasMap &&
                            originalSite.HasPlayerPresence(),
                        "occupied expedition site still exists",
                        failures
                    );
                    Check(
                        ++checks,
                        originalSite.PendingExpiration,
                        "pending-expiration state is persisted on site",
                        failures
                    );
                    Check(
                        ++checks,
                        record?.IsActive == true &&
                            record.ExpeditionId ==
                                expirationSnapshot.ExpeditionId &&
                            record.SiteWorldObjectId ==
                                expirationSnapshot.SiteWorldObjectId,
                        "source record still points to pending site",
                        failures
                    );
                    Check(
                        ++checks,
                        PlayerStateIsUntouched(
                            expirationSnapshot,
                            originalSite
                        ),
                        "player pawns and snapshotted items are untouched",
                        failures
                    );
                    Check(
                        ++checks,
                        record?.IsActive == true &&
                            allSites.Count == 1,
                        "production generation guard blocks a second site",
                        failures
                    );
                    Check(
                        ++checks,
                        SourceStateIsUnchanged(
                            camp,
                            expirationSnapshot
                        ),
                        "source population and roles are unchanged while pending",
                        failures
                    );
                }
                else
                {
                    completedExpirationSnapshot = true;
                    Check(
                        ++checks,
                        allSites.TrueForAll(
                            candidate =>
                                candidate.ID !=
                                    expirationSnapshot.SiteWorldObjectId
                        ),
                        "expired site is gone",
                        failures
                    );
                    Check(
                        ++checks,
                        record?.IsActive != true && site == null,
                        "active expedition is cleared",
                        failures
                    );
                    Check(
                        ++checks,
                        camp.Data.NextExpeditionEligibleTick >
                            expirationSnapshot.SimulatedAtTick,
                        "expedition cooldown has begun",
                        failures
                    );
                    Check(
                        ++checks,
                        SourceStateIsUnchanged(
                            camp,
                            expirationSnapshot
                        ),
                        "source population and roles survived cleanup",
                        failures
                    );
                }
            }

            bool passed = failures.Count == 0;

            if (passed && completedExpirationSnapshot)
            {
                expirationTestSnapshots.Remove(camp.ID);
            }

            StringBuilder report = new StringBuilder();
            report.AppendLine(
                "Expedition Invariants — " +
                (passed ? "PASS" : "FAIL")
            );
            report.AppendLine(
                passed
                    ? checks + "/" + checks + " checks passed"
                    : failures.Count + " failure(s) across " +
                        checks + " checks"
            );

            foreach (string failure in failures)
            {
                report.AppendLine("✗ " + failure);
            }

            ShowReport(report.ToString().TrimEnd());
        }

        private static void CheckColonyVisitFormulaInvariants(
            ref int checks,
            List<string> failures
        )
        {
            Check(
                ++checks,
                GetFormulaChance(
                    EnclaveReputationTier.Wary,
                    EnclaveArchetype.TradeCompact,
                    5f
                ) == 0,
                "Wary colony-visit chance is 0%",
                failures
            );
            Check(
                ++checks,
                GetFormulaChance(
                    EnclaveReputationTier.Hostile,
                    EnclaveArchetype.TradeCompact,
                    5f
                ) == 0,
                "Hostile colony-visit chance is 0%",
                failures
            );
            Check(
                ++checks,
                GetFormulaChance(
                    EnclaveReputationTier.Neutral,
                    EnclaveArchetype.Hearthbound,
                    5f
                ) == 0,
                "Neutral Hearthbound colony-visit chance is 0%",
                failures
            );
            Check(
                ++checks,
                GetFormulaChance(
                    EnclaveReputationTier.Neutral,
                    EnclaveArchetype.WarriorCovenant,
                    5f
                ) == 0,
                "Neutral Warrior Covenant colony-visit chance is 0%",
                failures
            );
            Check(
                ++checks,
                GetFormulaChance(
                    EnclaveReputationTier.Neutral,
                    EnclaveArchetype.TradeCompact,
                    5f
                ) == 40,
                "Neutral Trade Compact Strong chance is 40%",
                failures
            );
            Check(
                ++checks,
                GetFormulaChance(
                    EnclaveReputationTier.Revered,
                    EnclaveArchetype.Hearthbound,
                    5f
                ) == 75,
                "Revered Hearthbound Strong chance clamps to 75%",
                failures
            );
        }

        private static int GetFormulaChance(
            EnclaveReputationTier tier,
            EnclaveArchetype archetype,
            float distance
        )
        {
            EnclaveData data = new EnclaveData
            {
                Archetype = archetype,
                Reputation = ReputationForTier(tier)
            };

            return EnclaveExpeditionUtility
                .GetColonyVisitChancePercent(data, distance);
        }

        private static int ReputationForTier(
            EnclaveReputationTier tier
        )
        {
            switch (tier)
            {
                case EnclaveReputationTier.Hostile:
                    return -50;
                case EnclaveReputationTier.Wary:
                    return -10;
                case EnclaveReputationTier.Friendly:
                    return 30;
                case EnclaveReputationTier.Trusted:
                    return 60;
                case EnclaveReputationTier.Revered:
                    return 90;
                default:
                    return 0;
            }
        }

        private static ColonyVisitTestSnapshot
            CaptureColonyVisitSnapshot(PilgrimCamp camp)
        {
            ColonyVisitTestSnapshot snapshot =
                new ColonyVisitTestSnapshot
                {
                    SourceCampId = camp.ID,
                    SourcePopulation = camp.Data.Population,
                    LeaderId = PawnId(
                        camp.PawnRoles?.GetPawn(
                            EnclavePawnRole.Leader
                        )
                    ),
                    HomeTraderId = PawnId(
                        camp.PawnRoles?.GetPawn(
                            EnclavePawnRole.Trader
                        )
                    ),
                    RecruiterId = PawnId(
                        camp.PawnRoles?.GetPawn(
                            EnclavePawnRole.Recruiter
                        )
                    )
                };
            Pawn homeTrader = camp.PawnRoles?.GetPawn(
                EnclavePawnRole.Trader
            );

            if (homeTrader?.inventory != null)
            {
                foreach (
                    Thing thing in
                    homeTrader.inventory.innerContainer
                )
                {
                    snapshot.HomeTraderInventory.Add(
                        new ThingIdentitySnapshot
                        {
                            LoadId = thing.GetUniqueLoadID(),
                            StackCount = thing.stackCount
                        }
                    );
                }
            }

            return snapshot;
        }

        private static void ValidateVisitSnapshot(
            PilgrimCamp camp,
            ColonyVisitTestSnapshot snapshot,
            ref int checks,
            List<string> failures,
            bool active
        )
        {
            if (snapshot == null)
            {
                return;
            }

            Check(
                ++checks,
                snapshot.SourceCampId == camp.ID &&
                    camp.Data.Population == snapshot.SourcePopulation,
                "source population matches its pre-visit snapshot",
                failures
            );
            Check(
                ++checks,
                PawnId(
                    camp.PawnRoles?.GetPawn(EnclavePawnRole.Leader)
                ) == snapshot.LeaderId &&
                PawnId(
                    camp.PawnRoles?.GetPawn(EnclavePawnRole.Trader)
                ) == snapshot.HomeTraderId &&
                PawnId(
                    camp.PawnRoles?.GetPawn(EnclavePawnRole.Recruiter)
                ) == snapshot.RecruiterId,
                "home Leader, Trader, and Recruiter are unchanged",
                failures
            );
            Check(
                ++checks,
                HomeTraderInventoryIsUnchanged(snapshot),
                "home Trader inventory is unchanged",
                failures
            );
            Check(
                ++checks,
                snapshot.DestinationMapId < 0 ||
                    Find.Maps.Exists(
                        map =>
                            map.uniqueID ==
                                snapshot.DestinationMapId
                    ),
                "destination map was not regenerated or removed",
                failures
            );

            if (active)
            {
                Check(
                    ++checks,
                    camp.Data.Expedition?.ExpeditionId ==
                        snapshot.ExpeditionId,
                    "active visit retains its generated expedition ID",
                    failures
                );
            }
        }

        private static bool HomeTraderInventoryIsUnchanged(
            ColonyVisitTestSnapshot snapshot
        )
        {
            Pawn trader = FindPawnById(
                snapshot.HomeTraderId,
                snapshot.SourceCampId
            );

            if (trader == null)
            {
                return snapshot.HomeTraderId.NullOrEmpty() &&
                    snapshot.HomeTraderInventory.Count == 0;
            }

            if (trader.inventory == null)
            {
                return snapshot.HomeTraderInventory.Count == 0;
            }

            if (
                trader.inventory.innerContainer.Count !=
                    snapshot.HomeTraderInventory.Count
            )
            {
                return false;
            }

            foreach (
                ThingIdentitySnapshot thingSnapshot in
                snapshot.HomeTraderInventory
            )
            {
                Thing match = null;

                foreach (Thing thing in trader.inventory.innerContainer)
                {
                    if (thing.GetUniqueLoadID() == thingSnapshot.LoadId)
                    {
                        match = thing;
                        break;
                    }
                }

                if (match == null || match.stackCount != thingSnapshot.StackCount)
                {
                    return false;
                }
            }

            return true;
        }

        private static Pawn FindPawnById(
            string pawnId,
            int sourceCampId
        )
        {
            if (pawnId.NullOrEmpty())
            {
                return null;
            }

            PilgrimCamp source = null;

            foreach (WorldObject worldObject in Find.WorldObjects.AllWorldObjects)
            {
                PilgrimCamp candidate = worldObject as PilgrimCamp;

                if (candidate?.ID == sourceCampId)
                {
                    source = candidate;
                    break;
                }
            }

            if (source?.PawnRoles == null)
            {
                return null;
            }

            foreach (EnclavePawnRole role in new[]
            {
                EnclavePawnRole.Leader,
                EnclavePawnRole.Trader,
                EnclavePawnRole.Recruiter
            })
            {
                Pawn pawn = source.PawnRoles.GetPawn(role);

                if (PawnId(pawn) == pawnId)
                {
                    return pawn;
                }
            }

            return null;
        }

        private static string PawnId(Pawn pawn)
        {
            return pawn?.GetUniqueLoadID();
        }

        private static bool AllVisitorsUseSourceIdentity(
            PilgrimCamp camp,
            EnclaveColonyVisitRecord visit,
            Ideo sourceIdeo
        )
        {
            if (
                visit?.SourceCamp != camp ||
                sourceIdeo == null ||
                visit.Visitors.Count == 0
            )
            {
                return false;
            }

            foreach (Pawn pawn in visit.Visitors)
            {
                if (
                    pawn == null ||
                    pawn.Destroyed ||
                    !EnclaveFactionUtility.IsEnclaveFaction(
                        pawn.Faction
                    ) ||
                    pawn.Ideo != sourceIdeo
                )
                {
                    return false;
                }
            }

            return true;
        }

        private static bool NoVisitorIsAHomeMember(
            PilgrimCamp camp,
            EnclaveColonyVisitRecord visit
        )
        {
            foreach (Pawn pawn in visit.Visitors)
            {
                if (camp.PawnMembers?.Contains(pawn) == true)
                {
                    return false;
                }
            }

            return true;
        }

        private static int CountVisitorReference(
            EnclaveColonyVisitRecord visit,
            Pawn pawn
        )
        {
            int count = 0;

            foreach (Pawn visitor in visit.Visitors)
            {
                if (visitor == pawn)
                {
                    count++;
                }
            }

            return count;
        }

        private static ExpirationTestSnapshot CaptureExpirationSnapshot(
            PilgrimCamp camp,
            EnclaveExpeditionSite site
        )
        {
            ExpirationTestSnapshot snapshot =
                new ExpirationTestSnapshot
                {
                    SourceCamp = camp,
                    ExpeditionId = site.ExpeditionId,
                    SiteWorldObjectId = site.ID,
                    SimulatedAtTick =
                        Find.TickManager?.TicksGame ?? 0,
                    SourcePopulation = camp.Data.Population,
                    Leader = camp.PawnRoles?.GetPawn(
                        EnclavePawnRole.Leader
                    ),
                    Trader = camp.PawnRoles?.GetPawn(
                        EnclavePawnRole.Trader
                    ),
                    Recruiter = camp.PawnRoles?.GetPawn(
                        EnclavePawnRole.Recruiter
                    ),
                    OccupiedMap = site.Map,
                    WasOccupied = site.HasPlayerPresence()
                };
            Map map = snapshot.OccupiedMap;

            if (map == null)
            {
                return snapshot;
            }

            HashSet<Thing> capturedThings = new HashSet<Thing>();

            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (!IsProtectedPlayerPawn(pawn))
                {
                    continue;
                }

                snapshot.PlayerPawns.Add(pawn);
                CapturePawnThings(
                    pawn,
                    capturedThings,
                    snapshot.PlayerThings
                );
            }

            foreach (Thing thing in map.listerThings.AllThings)
            {
                if (
                    !(thing is Pawn) &&
                    thing?.Faction == Faction.OfPlayer
                )
                {
                    CaptureThing(
                        thing,
                        capturedThings,
                        snapshot.PlayerThings
                    );
                }
            }

            return snapshot;
        }

        private static ExpirationTestSnapshot GetExpirationTestSnapshot(
            PilgrimCamp camp
        )
        {
            ExpirationTestSnapshot snapshot;

            if (
                camp == null ||
                !expirationTestSnapshots.TryGetValue(
                    camp.ID,
                    out snapshot
                )
            )
            {
                return null;
            }

            if (!ReferenceEquals(snapshot.SourceCamp, camp))
            {
                expirationTestSnapshots.Remove(camp.ID);
                return null;
            }

            return snapshot;
        }

        private static bool PlayerStateIsUntouched(
            ExpirationTestSnapshot snapshot,
            EnclaveExpeditionSite site
        )
        {
            if (
                snapshot == null ||
                site?.Map == null ||
                site.Map != snapshot.OccupiedMap
            )
            {
                return false;
            }

            foreach (Pawn pawn in snapshot.PlayerPawns)
            {
                if (
                    pawn == null ||
                    pawn.Destroyed ||
                    !pawn.Spawned ||
                    pawn.Map != snapshot.OccupiedMap
                )
                {
                    return false;
                }
            }

            foreach (
                PlayerThingSnapshot thingSnapshot in
                snapshot.PlayerThings
            )
            {
                if (
                    thingSnapshot.Thing == null ||
                    thingSnapshot.Thing.Destroyed ||
                    thingSnapshot.Thing.stackCount !=
                        thingSnapshot.StackCount
                )
                {
                    return false;
                }
            }

            return true;
        }

        private static bool SourceStateIsUnchanged(
            PilgrimCamp camp,
            ExpirationTestSnapshot snapshot
        )
        {
            return
                camp?.Data != null &&
                camp.PawnRoles != null &&
                camp.Data.Population == snapshot.SourcePopulation &&
                camp.PawnRoles.GetPawn(EnclavePawnRole.Leader) ==
                    snapshot.Leader &&
                camp.PawnRoles.GetPawn(EnclavePawnRole.Trader) ==
                    snapshot.Trader &&
                camp.PawnRoles.GetPawn(EnclavePawnRole.Recruiter) ==
                    snapshot.Recruiter;
        }

        private static void CapturePawnThings(
            Pawn pawn,
            HashSet<Thing> captured,
            List<PlayerThingSnapshot> snapshots
        )
        {
            if (pawn?.inventory != null)
            {
                foreach (Thing thing in pawn.inventory.innerContainer)
                {
                    CaptureThing(thing, captured, snapshots);
                }
            }

            if (pawn?.equipment != null)
            {
                foreach (
                    ThingWithComps thing in
                    pawn.equipment.AllEquipmentListForReading
                )
                {
                    CaptureThing(thing, captured, snapshots);
                }
            }

            if (pawn?.apparel != null)
            {
                foreach (Apparel apparel in pawn.apparel.WornApparel)
                {
                    CaptureThing(apparel, captured, snapshots);
                }
            }

            CaptureThing(
                pawn?.carryTracker?.CarriedThing,
                captured,
                snapshots
            );
        }

        private static void CaptureThing(
            Thing thing,
            HashSet<Thing> captured,
            List<PlayerThingSnapshot> snapshots
        )
        {
            if (
                thing == null ||
                thing.Destroyed ||
                !captured.Add(thing)
            )
            {
                return;
            }

            snapshots.Add(
                new PlayerThingSnapshot
                {
                    Thing = thing,
                    StackCount = thing.stackCount
                }
            );
        }

        private static bool IsProtectedPlayerPawn(Pawn pawn)
        {
            return
                pawn != null &&
                !pawn.Destroyed &&
                (
                    pawn.Faction == Faction.OfPlayer ||
                    pawn.IsPrisonerOfColony ||
                    pawn.HostFaction == Faction.OfPlayer
                );
        }

        private static List<EnclaveExpeditionSite> GetSitesForSource(
            PilgrimCamp camp
        )
        {
            List<EnclaveExpeditionSite> result =
                new List<EnclaveExpeditionSite>();

            if (Find.WorldObjects?.AllWorldObjects == null)
            {
                return result;
            }

            foreach (
                WorldObject worldObject in
                Find.WorldObjects.AllWorldObjects
            )
            {
                EnclaveExpeditionSite site =
                    worldObject as EnclaveExpeditionSite;

                if (
                    site != null &&
                    !site.Destroyed &&
                    site.SourceCamp == camp
                )
                {
                    result.Add(site);
                }
            }

            return result;
        }

        private static bool TemporaryMembersAreSeparate(
            PilgrimCamp camp,
            EnclaveExpeditionSite site
        )
        {
            if (site?.ExpeditionMembers?.Members == null)
            {
                return true;
            }

            foreach (Pawn pawn in site.ExpeditionMembers.Members)
            {
                if (camp.PawnMembers?.Contains(pawn) == true)
                {
                    return false;
                }
            }

            return true;
        }

        private static void Check(
            int ignoredNumber,
            bool passed,
            string label,
            List<string> failures
        )
        {
            if (!passed)
            {
                failures.Add(label);
            }
        }

        private static void ShowReport(string report)
        {
            Log.Message("[IEE] DEV expedition testing\n" + report);
            Find.WindowStack.Add(new Dialog_MessageBox(report));
        }

        private static string FormatCooldownRemaining(EnclaveData data)
        {
            int currentTick = Find.TickManager?.TicksGame ?? 0;
            int remaining = Math.Max(
                0,
                (data?.NextExpeditionEligibleTick ?? 0) - currentTick
            );

            return remaining == 0
                ? "eligible now"
                : (remaining / 60000f).ToString("0.#") + " days";
        }

        private static bool CanUse(PilgrimCamp camp)
        {
            return Prefs.DevMode && camp?.Data != null;
        }
    }
}
