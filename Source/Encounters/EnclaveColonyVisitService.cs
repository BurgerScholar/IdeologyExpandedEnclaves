using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveColonyVisitService
    {
        private const int DownedDepartureGraceTicks = 60000;

        public static bool TryFindEligibleDestination(
            PilgrimCamp source,
            out Settlement destination,
            out float distanceInTiles
        )
        {
            destination = null;
            distanceInTiles = float.PositiveInfinity;

            if (
                source == null ||
                source.Destroyed ||
                !source.Tile.Valid ||
                Find.WorldObjects?.AllWorldObjects == null
            )
            {
                return false;
            }

            foreach (
                WorldObject worldObject in
                Find.WorldObjects.AllWorldObjects
            )
            {
                Settlement settlement = worldObject as Settlement;

                if (!IsEligibleDestination(source, settlement))
                {
                    continue;
                }

                float distance =
                    EnclaveProximityUtility.GetDistanceInTiles(
                        source,
                        settlement
                    );

                if (
                    distance >
                        EnclaveExpeditionUtility
                            .MaximumColonyVisitDistance ||
                    (
                        destination != null &&
                        (
                            distance > distanceInTiles ||
                            (
                                Math.Abs(distance - distanceInTiles) <
                                    0.001f &&
                                settlement.ID >= destination.ID
                            )
                        )
                    )
                )
                {
                    continue;
                }

                destination = settlement;
                distanceInTiles = distance;
            }

            return destination != null;
        }

        public static bool TryStartVisit(
            PilgrimCamp source,
            Settlement destination,
            int expeditionId,
            EnclaveExpeditionPurpose purpose,
            int startTick,
            int departureTick,
            out EnclaveColonyVisitRecord visit,
            out string failureReason
        )
        {
            visit = null;
            failureReason = null;

            if (!IsEligibleDestination(source, destination))
            {
                failureReason =
                    "No eligible player home colony is available within " +
                    "30 tiles.";
                return false;
            }

            float distance =
                EnclaveProximityUtility.GetDistanceInTiles(
                    source,
                    destination
                );

            if (
                EnclaveExpeditionUtility.GetColonyVisitChancePercent(
                    source.Data,
                    distance
                ) <= 0
            )
            {
                failureReason =
                    "The enclave's current reputation does not permit " +
                    "this colony visit.";
                return false;
            }

            Map map = destination.Map;
            EnclaveColonyVisitMapComponent component =
                map.GetComponent<EnclaveColonyVisitMapComponent>();

            if (component == null)
            {
                failureReason =
                    "The destination colony visit component is " +
                    "unavailable.";
                return false;
            }

            if (FindVisitForSource(source) != null)
            {
                failureReason =
                    "This enclave already has an active colony visit.";
                return false;
            }

            Faction faction = EnclaveFactionUtility.GetOrCreateFaction();
            Ideo sourceIdeo;

            if (
                faction == null ||
                !EnclaveIdeologyUtility.TryGetOrCreateActualIdeo(
                    source.Data,
                    out sourceIdeo
                )
            )
            {
                failureReason =
                    "The dedicated enclave faction or source ideology " +
                    "is unavailable.";
                return false;
            }

            IntVec3 entryCell;

            if (
                !RCellFinder.TryFindRandomPawnEntryCell(
                    out entryCell,
                    map,
                    0f,
                    true,
                    cell => cell.Standable(map)
                )
            )
            {
                failureReason =
                    "No safe colony map-edge arrival cell was found.";
                return false;
            }

            int partySize =
                EnclaveExpeditionUtility.GetPartySize(source.Data);
            List<Pawn> visitors = new List<Pawn>();

            for (int index = 0; index < partySize; index++)
            {
                Pawn pawn = null;

                try
                {
                    PawnGenerationRequest request =
                        new PawnGenerationRequest(
                            PawnKindDefOf.Villager,
                            faction,
                            PawnGenerationContext.NonPlayer,
                            map.Tile,
                            forceGenerateNewPawn: true,
                            canGeneratePawnRelations: false,
                            mustBeCapableOfViolence:
                                purpose ==
                                    EnclaveExpeditionPurpose.Patrol,
                            fixedIdeo: sourceIdeo,
                            forceRecruitable: false
                        );

                    pawn = PawnGenerator.GeneratePawn(request);

                    if (purpose == EnclaveExpeditionPurpose.Patrol)
                    {
                        EnsureBasicWeapon(pawn);
                    }

                    IntVec3 spawnCell = index == 0
                        ? entryCell
                        : CellFinder.RandomSpawnCellForPawnNear(
                            entryCell,
                            map,
                            10
                        );

                    GenSpawn.Spawn(pawn, spawnCell, map);
                    visitors.Add(pawn);
                }
                catch (Exception exception)
                {
                    Log.Error(
                        "[IEE] Failed to generate a colony visitor: " +
                        exception
                    );
                    DiscardUnspawnedPawn(pawn);
                }
            }

            if (visitors.Count == 0)
            {
                failureReason =
                    "No valid temporary colony visitors could be " +
                    "generated.";
                return false;
            }

            Pawn trader = visitors[0];

            if (
                !EnclaveTemporaryTraderUtility.Initialize(
                    trader,
                    map,
                    purpose
                )
            )
            {
                CleanupFailedParty(visitors, map);
                failureReason =
                    "The temporary colony-visit Trader could not be " +
                    "initialized.";
                return false;
            }

            IntVec3 chillSpot;

            if (
                !RCellFinder.TryFindRandomSpotJustOutsideColony(
                    visitors[0],
                    out chillSpot
                )
            )
            {
                chillSpot = CellFinder.RandomClosewalkCellNear(
                    map.Center,
                    map,
                    10
                );
            }

            LordMaker.MakeNewLord(
                faction,
                new LordJob_VisitColony(
                    faction,
                    chillSpot,
                    Math.Max(1, departureTick - startTick)
                ),
                map,
                visitors
            );

            visit = new EnclaveColonyVisitRecord(
                expeditionId,
                purpose,
                source,
                destination,
                startTick,
                departureTick,
                visitors,
                trader
            );
            component.AddVisit(visit);

            SendArrivalLetter(visit, entryCell, map);

            Log.Message(
                "[IEE] Started " +
                EnclaveExpeditionUtility.GetColonyVisitTypeLabel(
                    purpose
                ) +
                " " +
                expeditionId +
                " from " +
                source.Data.Name +
                " at " +
                destination.Label +
                " with " +
                visitors.Count +
                " temporary visitor(s); departure tick " +
                departureTick +
                "."
            );

            return true;
        }

        public static void ProcessVisits(
            EnclaveColonyVisitMapComponent component,
            int currentTick
        )
        {
            if (component?.Visits == null)
            {
                return;
            }

            Map map = GetComponentMap(component);

            if (map == null)
            {
                return;
            }
            List<EnclaveColonyVisitRecord> snapshot =
                new List<EnclaveColonyVisitRecord>(component.Visits);

            foreach (EnclaveColonyVisitRecord visit in snapshot)
            {
                if (visit == null)
                {
                    component.RemoveVisit(null);
                    continue;
                }

                PruneTransferredAndExitedVisitors(visit, map);

                if (visit.Visitors.Count == 0)
                {
                    CompleteVisit(component, visit, currentTick);
                    continue;
                }

                if (
                    visit.State == EnclaveColonyVisitState.Active &&
                    ShouldDepart(visit, map, currentTick)
                )
                {
                    BeginDeparture(visit, map, currentTick);
                }

                if (visit.State == EnclaveColonyVisitState.Active)
                {
                    EnsureVisitLord(visit, map, currentTick);
                    continue;
                }

                EnsureExitLord(visit, map);

                if (
                    visit.State ==
                        EnclaveColonyVisitState.Departing &&
                    currentTick - visit.DepartureStartedTick >=
                        DownedDepartureGraceTicks &&
                    OnlyDownedVisitorsRemain(visit, map)
                )
                {
                    CompleteSourceExpedition(visit, currentTick);
                    visit.DetachFromSource();

                    Log.Warning(
                        "[IEE] Colony visit " +
                        visit.ExpeditionId +
                        " completed after the downed-visitor grace " +
                        "period. Remaining downed visitors were " +
                        "preserved on the map under their exit Lord."
                    );
                }
            }
        }

        public static bool TryBeginDeparture(
            PilgrimCamp source,
            out string result
        )
        {
            EnclaveColonyVisitRecord visit = FindVisitForSource(source);
            Map map = visit?.Destination?.Map;

            if (
                visit == null ||
                map == null ||
                visit.State ==
                    EnclaveColonyVisitState.DetachedCleanup
            )
            {
                result = "This enclave has no active colony visit.";
                return false;
            }

            int currentTick = Find.TickManager?.TicksGame ?? 0;
            BeginDeparture(visit, map, currentTick);
            result =
                EnclaveExpeditionUtility.GetColonyVisitTypeLabel(
                    visit.Purpose
                ) +
                " is departing through the production visitor exit " +
                "path.";
            return true;
        }

        public static EnclaveColonyVisitRecord FindVisitForSource(
            PilgrimCamp source
        )
        {
            if (source == null || Find.Maps == null)
            {
                return null;
            }

            foreach (Map map in Find.Maps)
            {
                EnclaveColonyVisitMapComponent component =
                    map.GetComponent<EnclaveColonyVisitMapComponent>();

                if (component?.Visits == null)
                {
                    continue;
                }

                foreach (EnclaveColonyVisitRecord visit in component.Visits)
                {
                    if (
                        visit?.SourceCamp == source &&
                        visit.IsSourceExpeditionActive
                    )
                    {
                        return visit;
                    }
                }
            }

            return null;
        }

        public static List<EnclaveColonyVisitRecord> FindVisitsForSource(
            PilgrimCamp source
        )
        {
            List<EnclaveColonyVisitRecord> result =
                new List<EnclaveColonyVisitRecord>();

            if (source == null || Find.Maps == null)
            {
                return result;
            }

            foreach (Map map in Find.Maps)
            {
                EnclaveColonyVisitMapComponent component =
                    map.GetComponent<EnclaveColonyVisitMapComponent>();

                if (component?.Visits == null)
                {
                    continue;
                }

                foreach (EnclaveColonyVisitRecord visit in component.Visits)
                {
                    if (
                        visit?.SourceCamp == source &&
                        visit.IsSourceExpeditionActive
                    )
                    {
                        result.Add(visit);
                    }
                }
            }

            return result;
        }

        public static bool TryGetVisitForTrader(
            Pawn trader,
            out EnclaveColonyVisitRecord visit
        )
        {
            visit = null;
            Map map = trader?.Map;

            if (map == null)
            {
                return false;
            }

            EnclaveColonyVisitMapComponent component =
                map.GetComponent<EnclaveColonyVisitMapComponent>();

            if (component?.Visits == null)
            {
                return false;
            }

            foreach (EnclaveColonyVisitRecord candidate in component.Visits)
            {
                if (
                    candidate?.Trader == trader &&
                    candidate.ContainsVisitor(trader)
                )
                {
                    visit = candidate;
                    return true;
                }
            }

            return false;
        }

        public static bool TryResolveVisitor(
            Pawn pawn,
            out PilgrimCamp source
        )
        {
            source = null;
            Map map = pawn?.Map;

            if (
                map == null ||
                pawn.Destroyed ||
                pawn.Faction == null ||
                pawn.Faction == Faction.OfPlayer ||
                pawn.IsPrisonerOfColony ||
                pawn.HostFaction == Faction.OfPlayer ||
                pawn.RaceProps?.Humanlike != true
            )
            {
                return false;
            }

            EnclaveColonyVisitMapComponent component =
                map.GetComponent<EnclaveColonyVisitMapComponent>();

            if (component?.Visits == null)
            {
                return false;
            }

            foreach (EnclaveColonyVisitRecord visit in component.Visits)
            {
                if (
                    visit?.ContainsVisitor(pawn) == true &&
                    visit.SourceCamp?.Data != null &&
                    !visit.SourceCamp.Destroyed
                )
                {
                    source = visit.SourceCamp;
                    return true;
                }
            }

            return false;
        }

        private static bool IsEligibleDestination(
            PilgrimCamp source,
            Settlement settlement
        )
        {
            if (
                source == null ||
                settlement == null ||
                settlement.Destroyed ||
                !settlement.Spawned ||
                settlement.Faction != Faction.OfPlayer ||
                !settlement.Tile.Valid ||
                settlement.Tile.Layer != source.Tile.Layer ||
                !settlement.HasMap ||
                settlement.Map == null ||
                !settlement.Map.IsPlayerHome ||
                settlement.Map.IsTempIncidentMap
            )
            {
                return false;
            }

            float distance =
                EnclaveProximityUtility.GetDistanceInTiles(
                    source,
                    settlement
                );

            return
                !float.IsNaN(distance) &&
                !float.IsInfinity(distance) &&
                distance <=
                    EnclaveExpeditionUtility.MaximumColonyVisitDistance;
        }

        private static bool ShouldDepart(
            EnclaveColonyVisitRecord visit,
            Map map,
            int currentTick
        )
        {
            if (
                visit.SourceCamp == null ||
                visit.SourceCamp.Destroyed ||
                visit.SourceCamp.Data == null ||
                visit.Destination == null ||
                visit.Destination.Destroyed ||
                visit.Destination.Map != map ||
                !map.IsPlayerHome ||
                currentTick >= visit.DepartureTick
            )
            {
                return true;
            }

            EnclaveReputationTier tier =
                visit.SourceCamp.Data.ReputationTier;

            return
                tier == EnclaveReputationTier.Hostile ||
                tier == EnclaveReputationTier.Wary;
        }

        private static void BeginDeparture(
            EnclaveColonyVisitRecord visit,
            Map map,
            int currentTick
        )
        {
            if (
                visit == null ||
                map == null ||
                visit.State != EnclaveColonyVisitState.Active
            )
            {
                return;
            }

            visit.BeginDeparture(currentTick);
            EnsureExitLord(visit, map);

            Log.Message(
                "[IEE] Colony visit " +
                visit.ExpeditionId +
                " from " +
                (visit.SourceCamp?.Data?.Name ?? "an enclave") +
                " began its production departure path."
            );
        }

        private static void EnsureVisitLord(
            EnclaveColonyVisitRecord visit,
            Map map,
            int currentTick
        )
        {
            List<Pawn> active = GetVisitorsOnMap(
                visit,
                map,
                includeDowned: true
            );

            if (active.Count == 0)
            {
                return;
            }

            foreach (Pawn pawn in active)
            {
                if (pawn.GetLord()?.LordJob is LordJob_VisitColony)
                {
                    return;
                }
            }

            IntVec3 chillSpot;

            if (
                !RCellFinder.TryFindRandomSpotJustOutsideColony(
                    active[0],
                    out chillSpot
                )
            )
            {
                chillSpot = map.Center;
            }

            foreach (Pawn pawn in active)
            {
                pawn.GetLord()?.RemovePawn(pawn);
            }

            Faction faction = EnclaveFactionUtility.GetOrCreateFaction();

            if (faction != null)
            {
                LordMaker.MakeNewLord(
                    faction,
                    new LordJob_VisitColony(
                        faction,
                        chillSpot,
                        Math.Max(1, visit.DepartureTick - currentTick)
                    ),
                    map,
                    active
                );
            }
        }

        private static void EnsureExitLord(
            EnclaveColonyVisitRecord visit,
            Map map
        )
        {
            List<Pawn> active = GetVisitorsOnMap(
                visit,
                map,
                includeDowned: true
            );

            if (active.Count == 0)
            {
                return;
            }

            Lord exitLord = null;

            foreach (Pawn pawn in active)
            {
                Lord lord = pawn.GetLord();

                if (lord?.LordJob is LordJob_ExitMapBest)
                {
                    exitLord = lord;
                    break;
                }
            }

            if (exitLord == null)
            {
                foreach (Pawn pawn in active)
                {
                    Lord lord = pawn.GetLord();

                    if (
                        lord?.LordJob is LordJob_VisitColony &&
                        lord.Map == map
                    )
                    {
                        lord.SetJob(
                            new LordJob_ExitMapBest(
                                LocomotionUrgency.Jog,
                                canDig: false,
                                canDefendSelf: true
                            ),
                            false
                        );
                        exitLord = lord;
                        break;
                    }
                }
            }

            if (exitLord == null)
            {
                foreach (Pawn pawn in active)
                {
                    pawn.GetLord()?.RemovePawn(pawn);
                }

                Faction faction =
                    EnclaveFactionUtility.GetOrCreateFaction();

                if (faction != null)
                {
                    exitLord = LordMaker.MakeNewLord(
                        faction,
                        new LordJob_ExitMapBest(
                            LocomotionUrgency.Jog,
                            canDig: false,
                            canDefendSelf: true
                        ),
                        map,
                        active
                    );
                }
            }

            if (exitLord == null)
            {
                return;
            }

            foreach (Pawn pawn in active)
            {
                Lord current = pawn.GetLord();

                if (current == exitLord)
                {
                    continue;
                }

                current?.RemovePawn(pawn);
                exitLord.AddPawn(pawn);
            }
        }

        private static void PruneTransferredAndExitedVisitors(
            EnclaveColonyVisitRecord visit,
            Map map
        )
        {
            List<Pawn> snapshot = new List<Pawn>(visit.Visitors);

            foreach (Pawn pawn in snapshot)
            {
                if (
                    pawn == null ||
                    pawn.Destroyed ||
                    pawn.Dead ||
                    IsLegitimatelyTransferred(pawn)
                )
                {
                    visit.RemoveVisitor(pawn);
                    continue;
                }

                if (!pawn.Spawned && pawn.MapHeld != map)
                {
                    // Vanilla's exit toil owns the world-pawn transfer.
                    // Releasing our reference avoids deleting a pawn that
                    // another legitimate world holder has adopted.
                    visit.RemoveVisitor(pawn);
                }
            }
        }

        private static bool IsLegitimatelyTransferred(Pawn pawn)
        {
            return
                pawn.Faction == Faction.OfPlayer ||
                pawn.IsPrisonerOfColony ||
                pawn.HostFaction == Faction.OfPlayer ||
                !EnclaveFactionUtility.IsEnclaveFaction(pawn.Faction);
        }

        private static List<Pawn> GetVisitorsOnMap(
            EnclaveColonyVisitRecord visit,
            Map map,
            bool includeDowned
        )
        {
            List<Pawn> result = new List<Pawn>();

            foreach (Pawn pawn in visit.Visitors)
            {
                if (
                    pawn != null &&
                    !pawn.Destroyed &&
                    !pawn.Dead &&
                    pawn.Spawned &&
                    pawn.Map == map &&
                    (includeDowned || !pawn.Downed) &&
                    !IsLegitimatelyTransferred(pawn)
                )
                {
                    result.Add(pawn);
                }
            }

            return result;
        }

        private static bool OnlyDownedVisitorsRemain(
            EnclaveColonyVisitRecord visit,
            Map map
        )
        {
            bool found = false;

            foreach (Pawn pawn in visit.Visitors)
            {
                if (
                    pawn == null ||
                    pawn.Destroyed ||
                    pawn.Dead ||
                    pawn.MapHeld != map ||
                    IsLegitimatelyTransferred(pawn)
                )
                {
                    continue;
                }

                found = true;

                if (!pawn.Downed)
                {
                    return false;
                }
            }

            return found;
        }

        private static void CompleteVisit(
            EnclaveColonyVisitMapComponent component,
            EnclaveColonyVisitRecord visit,
            int currentTick
        )
        {
            CompleteSourceExpedition(visit, currentTick);
            component.RemoveVisit(visit);

            Log.Message(
                "[IEE] Completed colony visit " +
                visit.ExpeditionId +
                " from " +
                (visit.SourceCamp?.Data?.Name ?? "an enclave") +
                "."
            );
        }

        private static void CompleteSourceExpedition(
            EnclaveColonyVisitRecord visit,
            int currentTick
        )
        {
            PilgrimCamp source = visit?.SourceCamp;
            EnclaveExpeditionRecord record = source?.Data?.Expedition;

            if (
                record?.IsColonyVisit != true ||
                record.ExpeditionId != visit.ExpeditionId
            )
            {
                return;
            }

            record.MarkCompleted();
            source.Data.SetNextExpeditionEligibleTick(
                currentTick +
                EnclaveExpeditionUtility.GetCooldownTicks(source.Data)
            );
        }

        private static Map GetComponentMap(
            EnclaveColonyVisitMapComponent component
        )
        {
            return component?.ParentMap;
        }

        private static void CleanupFailedParty(
            List<Pawn> visitors,
            Map map
        )
        {
            foreach (Pawn pawn in visitors)
            {
                if (pawn == null || pawn.Destroyed)
                {
                    continue;
                }

                pawn.GetLord()?.RemovePawn(pawn);

                if (pawn.Spawned && pawn.Map == map)
                {
                    pawn.DeSpawn();
                }

                DiscardUnspawnedPawn(pawn);
            }
        }

        private static void DiscardUnspawnedPawn(Pawn pawn)
        {
            if (pawn == null || pawn.Spawned || pawn.Destroyed)
            {
                return;
            }

            Find.WorldPawns.PassToWorld(
                pawn,
                PawnDiscardDecideMode.Discard
            );
        }

        private static void EnsureBasicWeapon(Pawn pawn)
        {
            if (
                pawn?.equipment == null ||
                pawn.equipment.Primary != null
            )
            {
                return;
            }

            ThingWithComps knife = ThingMaker.MakeThing(
                ThingDefOf.MeleeWeapon_Knife,
                ThingDefOf.Steel
            ) as ThingWithComps;

            if (knife != null)
            {
                pawn.equipment.AddEquipment(knife);
            }
        }

        private static void SendArrivalLetter(
            EnclaveColonyVisitRecord visit,
            IntVec3 entryCell,
            Map map
        )
        {
            string enclaveName =
                visit.SourceCamp?.Data?.Name ?? "An enclave";
            string title;
            string text;

            switch (visit.Purpose)
            {
                case EnclaveExpeditionPurpose.Trade:
                    title = "Enclave Merchants: " + enclaveName;
                    text =
                        "A merchant delegation from " +
                        enclaveName +
                        " has arrived to trade. They expect to remain " +
                        "for about three days.";
                    break;
                case EnclaveExpeditionPurpose.Patrol:
                    title = "Enclave Patrol: " + enclaveName;
                    text =
                        "A patrol from " +
                        enclaveName +
                        " has stopped at your colony during its " +
                        "regional expedition.";
                    break;
                default:
                    title = "Enclave Visitors: " + enclaveName;
                    text =
                        enclaveName +
                        " has sent a relief delegation to your colony. " +
                        "They intend to remain for about two days before " +
                        "continuing their travels.";
                    break;
            }

            Find.LetterStack.ReceiveLetter(
                title,
                text,
                LetterDefOf.PositiveEvent,
                new TargetInfo(entryCell, map)
            );
        }
    }
}
