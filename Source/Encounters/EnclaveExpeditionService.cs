using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveExpeditionService
    {
        public const int MinimumSiteDistance = 6;
        public const int MaximumSiteDistance = 18;

        public static void EvaluateScheduledCycle(
            int scheduledEvaluationTick,
            int currentTick
        )
        {
            List<WorldObject> worldObjects =
                Find.WorldObjects?.AllWorldObjects;

            if (worldObjects == null)
            {
                return;
            }

            List<PilgrimCamp> camps = new List<PilgrimCamp>();

            foreach (WorldObject worldObject in worldObjects)
            {
                PilgrimCamp camp = worldObject as PilgrimCamp;

                if (
                    camp != null &&
                    !camp.Destroyed &&
                    camp.Spawned &&
                    camp.Data != null
                )
                {
                    camps.Add(camp);
                }
            }

            foreach (PilgrimCamp camp in camps)
            {
                ReconcileCamp(camp, currentTick);

                if (
                    camp.Data.Expedition?.IsActive == true ||
                    currentTick <
                        camp.Data.NextExpeditionEligibleTick
                )
                {
                    continue;
                }

                EnclaveExpeditionSite ignoredSite;
                string ignoredReason;
                TryGenerate(
                    camp,
                    scheduledEvaluationTick,
                    bypassCooldown: false,
                    bypassChance: false,
                    out ignoredSite,
                    out ignoredReason
                );
            }
        }

        public static bool TryGenerate(
            PilgrimCamp source,
            int evaluationTick,
            bool bypassCooldown,
            bool bypassChance,
            out EnclaveExpeditionSite site,
            out string failureReason
        )
        {
            EnclaveColonyVisitRecord ignoredVisit;

            return TryGenerateInternal(
                source,
                evaluationTick,
                bypassCooldown,
                bypassChance,
                forcedOutcome: null,
                out site,
                out ignoredVisit,
                out failureReason
            );
        }

        public static bool TryGenerateForDevelopment(
            PilgrimCamp source,
            int evaluationTick,
            EnclaveExpeditionOutcome forcedOutcome,
            out EnclaveExpeditionSite site,
            out EnclaveColonyVisitRecord visit,
            out string failureReason
        )
        {
            return TryGenerateInternal(
                source,
                evaluationTick,
                bypassCooldown: true,
                bypassChance: true,
                forcedOutcome: forcedOutcome,
                out site,
                out visit,
                out failureReason
            );
        }

        private static bool TryGenerateInternal(
            PilgrimCamp source,
            int evaluationTick,
            bool bypassCooldown,
            bool bypassChance,
            EnclaveExpeditionOutcome? forcedOutcome,
            out EnclaveExpeditionSite site,
            out EnclaveColonyVisitRecord visit,
            out string failureReason
        )
        {
            site = null;
            visit = null;
            failureReason = null;
            int currentTick = Find.TickManager?.TicksGame ?? 0;

            if (
                source == null ||
                source.Destroyed ||
                !source.Spawned ||
                source.Data == null ||
                !source.Tile.Valid
            )
            {
                failureReason = "The source enclave is unavailable.";
                return false;
            }

            ReconcileCamp(source, currentTick);

            if (
                source.Data.Expedition?.IsActive == true ||
                FindSitesForSource(source).Count > 0 ||
                EnclaveColonyVisitService
                    .FindVisitsForSource(source).Count > 0
            )
            {
                failureReason =
                    "This enclave already has an active expedition.";
                return false;
            }

            if (
                !bypassCooldown &&
                currentTick < source.Data.NextExpeditionEligibleTick
            )
            {
                failureReason =
                    "The enclave's expedition cooldown is still active.";
                return false;
            }

            int chance =
                EnclaveExpeditionUtility.GetGenerationChancePercent(
                    source
                );
            float roll =
                EnclaveExpeditionUtility.GetStableEvaluationRoll(
                    source,
                    evaluationTick
                );

            if (!bypassChance && roll >= chance)
            {
                failureReason =
                    "The scheduled expedition generation roll did not " +
                    "succeed.";
                return false;
            }

            EnclaveExpeditionWorldComponent component =
                Find.World.GetComponent<
                    EnclaveExpeditionWorldComponent
                >();

            if (component == null)
            {
                failureReason =
                    "The expedition world scheduler is unavailable.";
                return false;
            }

            int expeditionId = component.AllocateExpeditionId();
            EnclaveExpeditionPurpose purpose =
                EnclaveExpeditionUtility.GetPurpose(source.Data);

            Settlement colonyDestination;
            float colonyDistance;
            bool hasColonyDestination =
                EnclaveColonyVisitService.TryFindEligibleDestination(
                    source,
                    out colonyDestination,
                    out colonyDistance
                );
            int visitChance = hasColonyDestination
                ? EnclaveExpeditionUtility
                    .GetColonyVisitChancePercent(
                        source.Data,
                        colonyDistance
                    )
                : 0;
            float visitRoll =
                EnclaveExpeditionUtility.GetStableColonyVisitRoll(
                    source,
                    evaluationTick
                );
            bool generateColonyVisit =
                forcedOutcome ==
                    EnclaveExpeditionOutcome.ColonyVisit ||
                (
                    !forcedOutcome.HasValue &&
                    hasColonyDestination &&
                    visitRoll < visitChance
                );

            if (generateColonyVisit)
            {
                if (!hasColonyDestination || visitChance <= 0)
                {
                    failureReason =
                        "No reputation-eligible player home colony is " +
                        "available within 30 tiles.";
                    return false;
                }

                int departureTick =
                    currentTick +
                    EnclaveExpeditionUtility
                        .GetColonyVisitDurationTicks(purpose);
                string visitFailure;

                if (
                    EnclaveColonyVisitService.TryStartVisit(
                        source,
                        colonyDestination,
                        expeditionId,
                        purpose,
                        currentTick,
                        departureTick,
                        out visit,
                        out visitFailure
                    )
                )
                {
                    source.Data.SetExpedition(
                        EnclaveExpeditionRecord.CreateColonyVisit(
                            expeditionId,
                            purpose,
                            colonyDestination.ID,
                            colonyDestination.Map.uniqueID,
                            currentTick,
                            departureTick
                        )
                    );

                    Log.Message(
                        "[IEE] Expedition outcome for " +
                        source.Data.Name +
                        ": colony visit to " +
                        colonyDestination.Label +
                        "; chance " +
                        visitChance +
                        "%, stable roll " +
                        visitRoll.ToString("0.00") +
                        "."
                    );
                    return true;
                }

                if (
                    forcedOutcome ==
                        EnclaveExpeditionOutcome.ColonyVisit
                )
                {
                    failureReason = visitFailure;
                    return false;
                }

                Log.Warning(
                    "[IEE] Colony visit initialization failed for " +
                    source.Data.Name +
                    "; falling back to a temporary expedition site. " +
                    (visitFailure ?? "No reason was reported.")
                );
            }

            PlanetTile destination;

            if (
                !TryFindDestination(
                    source,
                    evaluationTick,
                    out destination
                )
            )
            {
                source.Data.SetNextExpeditionEligibleTick(
                    currentTick +
                    EnclaveExpeditionUtility.ShortRetryTicks
                );
                failureReason =
                    "No safe regional destination tile was available.";
                return false;
            }

            WorldObjectDef def =
                DefDatabase<WorldObjectDef>.GetNamedSilentFail(
                    "IEE_EnclaveExpeditionSite"
                );

            if (def == null)
            {
                failureReason =
                    "The expedition world-object definition is missing.";
                return false;
            }

            Faction expeditionFaction =
                EnclaveFactionUtility.GetOrCreateFaction();

            if (expeditionFaction == null)
            {
                failureReason =
                    "The dedicated enclave faction is unavailable.";
                return false;
            }

            int expirationTick =
                currentTick +
                EnclaveExpeditionUtility.GetDurationTicks(purpose);

            site = (EnclaveExpeditionSite)
                WorldObjectMaker.MakeWorldObject(def);
            site.Initialize(
                source,
                expeditionId,
                purpose,
                currentTick,
                expirationTick
            );
            site.Tile = destination;
            site.SetFaction(expeditionFaction);
            Find.WorldObjects.Add(site);

            source.Data.SetExpedition(
                new EnclaveExpeditionRecord(
                    expeditionId,
                    purpose,
                    site.ID,
                    currentTick,
                    expirationTick
                )
            );

            SendCreationLetter(site);

            Log.Message(
                "[IEE] Generated " +
                EnclaveExpeditionUtility.GetPurposeLabel(purpose) +
                " " +
                expeditionId +
                " for " +
                source.Data.Name +
                " at tile " +
                destination +
                "; chance " +
                chance +
                "%, stable roll " +
                roll.ToString("0.00") +
                ", expiration tick " +
                expirationTick +
                "."
            );

            return true;
        }

        public static void ReconcileAll()
        {
            int currentTick = Find.TickManager?.TicksGame ?? 0;
            List<WorldObject> worldObjects =
                Find.WorldObjects?.AllWorldObjects;

            if (worldObjects == null)
            {
                return;
            }

            foreach (
                WorldObject worldObject in
                new List<WorldObject>(worldObjects)
            )
            {
                PilgrimCamp camp = worldObject as PilgrimCamp;

                if (camp?.Data != null && !camp.Destroyed)
                {
                    ReconcileCamp(camp, currentTick);
                }
            }
        }

        public static void ReconcileCamp(
            PilgrimCamp source,
            int currentTick
        )
        {
            if (source?.Data == null || source.Destroyed)
            {
                return;
            }

            EnclaveExpeditionRecord record = source.Data.Expedition;
            List<EnclaveExpeditionSite> sites =
                FindSitesForSource(source);
            List<EnclaveColonyVisitRecord> visits =
                EnclaveColonyVisitService.FindVisitsForSource(source);

            if (record?.IsTemporarySite == true)
            {
                EnclaveExpeditionSite matching = sites.Find(
                    candidate =>
                        candidate.ID == record.SiteWorldObjectId &&
                        candidate.ExpeditionId == record.ExpeditionId
                );

                if (matching != null)
                {
                    return;
                }

                record.MarkCompleted();
                source.Data.SetNextExpeditionEligibleTick(
                    currentTick +
                    EnclaveExpeditionUtility.GetCooldownTicks(
                        source.Data
                    )
                );

                Log.Warning(
                    "[IEE] Reconciled a missing expedition site for " +
                    source.Data.Name +
                    "; the expedition was completed safely."
                );
                return;
            }

            if (record?.IsColonyVisit == true)
            {
                EnclaveColonyVisitRecord matching = visits.Find(
                    candidate =>
                        candidate.ExpeditionId == record.ExpeditionId &&
                        candidate.Destination?.ID ==
                            record.DestinationWorldObjectId &&
                        candidate.Destination?.Map?.uniqueID ==
                            record.DestinationMapId
                );

                if (matching != null)
                {
                    return;
                }

                record.MarkCompleted();
                source.Data.SetNextExpeditionEligibleTick(
                    currentTick +
                    EnclaveExpeditionUtility.GetCooldownTicks(
                        source.Data
                    )
                );

                Log.Warning(
                    "[IEE] Reconciled a missing colony visit for " +
                    source.Data.Name +
                    "; the expedition was completed safely."
                );
                return;
            }

            if (visits.Count == 1 && sites.Count == 0)
            {
                EnclaveColonyVisitRecord existing = visits[0];
                Settlement destination = existing.Destination;

                if (destination?.Map != null)
                {
                    source.Data.SetExpedition(
                        EnclaveExpeditionRecord.CreateColonyVisit(
                            existing.ExpeditionId,
                            existing.Purpose,
                            destination.ID,
                            destination.Map.uniqueID,
                            existing.StartTick,
                            existing.DepartureTick
                        )
                    );

                    Log.Message(
                        "[IEE] Reconnected colony visit " +
                        existing.ExpeditionId +
                        " to " +
                        source.Data.Name +
                        " after load."
                    );
                    return;
                }
            }

            if (sites.Count == 1 && visits.Count == 0)
            {
                EnclaveExpeditionSite existing = sites[0];

                source.Data.SetExpedition(
                    new EnclaveExpeditionRecord(
                        existing.ExpeditionId,
                        existing.Purpose,
                        existing.ID,
                        existing.CreationTick,
                        existing.ExpirationTick
                    )
                );

                Log.Message(
                    "[IEE] Reconnected expedition " +
                    existing.ExpeditionId +
                    " to " +
                    source.Data.Name +
                    " after load."
                );
            }
            else if (sites.Count + visits.Count > 1)
            {
                Log.Error(
                    "[IEE] Multiple expedition outcomes reference " +
                    source.Data.Name +
                    ". No new expedition will be generated until " +
                    "the invalid state is resolved."
                );
            }
        }

        public static EnclaveExpeditionSite GetActiveSite(
            PilgrimCamp source
        )
        {
            EnclaveExpeditionRecord record = source?.Data?.Expedition;

            if (record?.IsTemporarySite != true)
            {
                return null;
            }

            EnclaveExpeditionSite site = FindSiteById(
                record.SiteWorldObjectId
            );

            return
                site != null &&
                site.SourceCamp == source &&
                site.ExpeditionId == record.ExpeditionId
                    ? site
                    : null;
        }

        public static EnclaveColonyVisitRecord GetActiveColonyVisit(
            PilgrimCamp source
        )
        {
            EnclaveExpeditionRecord record = source?.Data?.Expedition;

            if (record?.IsColonyVisit != true)
            {
                return null;
            }

            EnclaveColonyVisitRecord visit =
                EnclaveColonyVisitService.FindVisitForSource(source);

            return
                visit != null &&
                visit.ExpeditionId == record.ExpeditionId
                    ? visit
                    : null;
        }

        public static bool TrySimulateExpirationNow(
            PilgrimCamp source,
            out string result
        )
        {
            EnclaveExpeditionSite site = GetActiveSite(source);

            if (site == null)
            {
                result = "This enclave has no active expedition site.";
                return false;
            }

            bool occupied = site.HasPlayerPresence();
            site.BeginExpiration();

            if (!site.Destroyed && site.PendingExpiration)
            {
                result = occupied
                    ? site.Label +
                        " reached its simulated expiration. Its occupied " +
                        "map was preserved and cleanup is pending until " +
                        "the player leaves."
                    : site.Label +
                        " entered production pending-expiration cleanup.";
            }
            else
            {
                result =
                    "Expired and removed " +
                    site.Label +
                    " through production cleanup.";
            }

            return true;
        }

        public static void NotifySiteEnded(
            EnclaveExpeditionSite site
        )
        {
            PilgrimCamp source = site?.SourceCamp;

            if (
                source == null ||
                source.Destroyed ||
                source.Data == null
            )
            {
                return;
            }

            EnclaveExpeditionRecord record = source.Data.Expedition;

            if (
                record?.IsTemporarySite != true ||
                record.ExpeditionId != site.ExpeditionId ||
                record.SiteWorldObjectId != site.ID
            )
            {
                return;
            }

            record.MarkCompleted();
            int currentTick = Find.TickManager?.TicksGame ?? 0;
            source.Data.SetNextExpeditionEligibleTick(
                currentTick +
                EnclaveExpeditionUtility.GetCooldownTicks(source.Data)
            );

            Log.Message(
                "[IEE] Completed expedition " +
                record.ExpeditionId +
                " for " +
                source.Data.Name +
                "; next eligible tick " +
                source.Data.NextExpeditionEligibleTick +
                "."
            );
        }

        public static int GetNextSafeExpeditionId()
        {
            int highest = 0;
            List<WorldObject> worldObjects =
                Find.WorldObjects?.AllWorldObjects;

            if (worldObjects == null)
            {
                return 1;
            }

            foreach (WorldObject worldObject in worldObjects)
            {
                EnclaveExpeditionSite site =
                    worldObject as EnclaveExpeditionSite;

                if (site != null)
                {
                    highest = Math.Max(highest, site.ExpeditionId);
                }

                PilgrimCamp camp = worldObject as PilgrimCamp;

                if (camp?.Data?.Expedition != null)
                {
                    highest = Math.Max(
                        highest,
                        camp.Data.Expedition.ExpeditionId
                    );
                }
            }

            if (Find.Maps != null)
            {
                foreach (Map map in Find.Maps)
                {
                    EnclaveColonyVisitMapComponent visitComponent =
                        map.GetComponent<
                            EnclaveColonyVisitMapComponent
                        >();

                    if (visitComponent?.Visits == null)
                    {
                        continue;
                    }

                    foreach (
                        EnclaveColonyVisitRecord visit in
                        visitComponent.Visits
                    )
                    {
                        if (visit != null)
                        {
                            highest = Math.Max(
                                highest,
                                visit.ExpeditionId
                            );
                        }
                    }
                }
            }

            return highest + 1;
        }

        private static bool TryFindDestination(
            PilgrimCamp source,
            int evaluationTick,
            out PlanetTile destination
        )
        {
            destination = PlanetTile.Invalid;
            int seed = EnclaveExpeditionUtility.GetDestinationSeed(
                source,
                evaluationTick
            );

            Rand.PushState(seed);

            try
            {
                return TileFinder.TryFindNewSiteTile(
                    out destination,
                    source.Tile,
                    MinimumSiteDistance,
                    MaximumSiteDistance,
                    allowCaravans: false,
                    allowedLandmarks: null,
                    selectLandmarkChance: 0f,
                    canSelectComboLandmarks: false,
                    tileFinderMode: TileFinderMode.Random,
                    exitOnFirstTileFound: true,
                    canBeSpace: false,
                    layer: source.Tile.Layer,
                    validator: tile =>
                        tile.Valid &&
                        tile != source.Tile &&
                        tile.Layer == source.Tile.Layer &&
                        Find.WorldPathGrid.Passable(tile) &&
                        !Find.WorldObjects.AnyWorldObjectAt(tile)
                );
            }
            finally
            {
                Rand.PopState();
            }
        }

        private static EnclaveExpeditionSite FindSiteById(int id)
        {
            if (id < 0 || Find.WorldObjects?.AllWorldObjects == null)
            {
                return null;
            }

            foreach (
                WorldObject worldObject in
                Find.WorldObjects.AllWorldObjects
            )
            {
                EnclaveExpeditionSite site =
                    worldObject as EnclaveExpeditionSite;

                if (site != null && !site.Destroyed && site.ID == id)
                {
                    return site;
                }
            }

            return null;
        }

        private static List<EnclaveExpeditionSite> FindSitesForSource(
            PilgrimCamp source
        )
        {
            List<EnclaveExpeditionSite> sites =
                new List<EnclaveExpeditionSite>();

            if (
                source == null ||
                Find.WorldObjects?.AllWorldObjects == null
            )
            {
                return sites;
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
                    site.SourceCamp == source
                )
                {
                    sites.Add(site);
                }
            }

            return sites;
        }

        private static void SendCreationLetter(
            EnclaveExpeditionSite site
        )
        {
            PilgrimCamp source = site.SourceCamp;
            string siteType =
                EnclaveExpeditionUtility.GetSiteTypeLabel(site.Purpose);
            int durationDays =
                EnclaveExpeditionUtility.GetDurationTicks(site.Purpose) /
                60000;

            Find.LetterStack.ReceiveLetter(
                "Enclave Expedition: " + siteType,
                "The " +
                    source.Data.Name +
                    " has established a temporary " +
                    siteType.ToLowerInvariant() +
                    " in the region. It is expected to remain for " +
                    "about " +
                    durationDays +
                    " days.",
                LetterDefOf.PositiveEvent,
                site
            );
        }
    }
}
