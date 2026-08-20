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

        private static readonly Dictionary<int, ExpirationTestSnapshot>
            expirationTestSnapshots =
                new Dictionary<int, ExpirationTestSnapshot>();

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
                    "; expedition " +
                    record.ExpeditionId +
                    "; site " +
                    record.SiteWorldObjectId
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
            bool generated = EnclaveExpeditionService.TryGenerate(
                camp,
                Find.TickManager?.TicksGame ?? 0,
                bypassCooldown: true,
                bypassChance: true,
                out site,
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

            if (record?.IsActive == true)
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
