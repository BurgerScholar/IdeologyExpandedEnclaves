using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveInterventionService
    {
        public static void NotifyRaidGenerated(
            Map map,
            Faction raidFaction,
            List<Pawn> raidPawns
        )
        {
            Faction playerFaction = Faction.OfPlayerSilentFail;

            if (
                !IsEligibleColonyMap(map) ||
                playerFaction == null ||
                raidFaction == null ||
                !raidFaction.HostileTo(playerFaction) ||
                raidPawns.NullOrEmpty()
            )
            {
                return;
            }

            EnclaveInterventionMapComponent component =
                map.GetComponent<EnclaveInterventionMapComponent>();

            if (component == null)
            {
                Log.Error(
                    "[IEE] Could not register a colony raid because " +
                    "the intervention map component was unavailable."
                );
                return;
            }

            EnclaveInterventionRecord record =
                component.RegisterRaid(raidPawns);

            Log.Message(
                "[IEE] Registered raid " +
                record.Id +
                " on " +
                map.Parent.LabelCap +
                " with " +
                record.RaidPawns.Count +
                " exact raid pawn reference(s). Intervention " +
                "evaluation is deferred until vanilla raid Lord " +
                "creation finishes."
            );
        }

        public static bool IsEligibleColonyMap(Map map)
        {
            return
                map != null &&
                map.IsPlayerHome &&
                map.Parent is Settlement &&
                map.ParentFaction == Faction.OfPlayerSilentFail;
        }

        public static void ProcessRecords(
            EnclaveInterventionMapComponent component,
            int currentTick
        )
        {
            if (component?.ParentMap == null || component.Records == null)
            {
                return;
            }

            Map map = component.ParentMap;
            List<EnclaveInterventionRecord> snapshot =
                new List<EnclaveInterventionRecord>(component.Records);

            foreach (EnclaveInterventionRecord record in snapshot)
            {
                if (record == null)
                {
                    continue;
                }

                if (
                    record.State ==
                        EnclaveRaidInterventionState.PendingEvaluation &&
                    currentTick > record.RegisteredTick
                )
                {
                    EvaluateRecord(
                        component,
                        record,
                        null,
                        out _
                    );
                }

                bool raidActive = IsOriginalRaidActive(map, record);

                if (
                    record.State ==
                        EnclaveRaidInterventionState.Active
                )
                {
                    if (raidActive)
                    {
                        EnsureCombatLord(map, record);
                    }
                    else
                    {
                        BeginExit(map, record);
                    }
                }

                if (
                    record.State ==
                        EnclaveRaidInterventionState.Exiting
                )
                {
                    EnsureExitLord(map, record);

                    if (!HasActivePartyMembers(map, record))
                    {
                        component.RemoveRecord(record);

                        Log.Message(
                            "[IEE] Cleaned completed enclave " +
                            "intervention record " +
                            record.Id +
                            " for " +
                            record.SourceEnclaveName +
                            "."
                        );
                    }
                }
                else if (
                    record.State ==
                        EnclaveRaidInterventionState.NoIntervention &&
                    !raidActive
                )
                {
                    component.RemoveRecord(record);
                }
            }
        }

        public static List<EnclaveInterventionProfile>
            GetNearbyProfilesForDebug(Map map)
        {
            int seed = GetPreviewSeed(map);
            EnclaveInterventionMapComponent component =
                map?.GetComponent<EnclaveInterventionMapComponent>();
            EnclaveInterventionRecord record =
                component?.GetLatestActiveRaidRecord();

            if (record != null)
            {
                seed = record.RollSeed;
            }

            return BuildCandidates(
                map,
                seed,
                includeNonIntervening: true
            );
        }

        public static bool TryEvaluateCurrentRaid(
            Map map,
            out string result
        )
        {
            result = null;

            if (!IsEligibleColonyMap(map))
            {
                result =
                    "The current map is not a normal player colony map.";
                return false;
            }

            EnclaveInterventionMapComponent component =
                map.GetComponent<EnclaveInterventionMapComponent>();
            EnclaveInterventionRecord record =
                component?.GetLatestActiveRaidRecord();

            if (record == null)
            {
                result =
                    "No registered active hostile raid exists on this map.";
                return false;
            }

            if (
                record.State !=
                    EnclaveRaidInterventionState.PendingEvaluation
            )
            {
                result =
                    "Raid " +
                    record.Id +
                    " has already been evaluated; it will not be rerolled.";
                return false;
            }

            return EvaluateRecord(
                component,
                record,
                null,
                out result
            );
        }

        public static bool TryForceIntervention(
            Map map,
            EnclaveInterventionSide forcedSide,
            out string result
        )
        {
            result = null;

            if (
                forcedSide != EnclaveInterventionSide.Friendly &&
                forcedSide != EnclaveInterventionSide.Hostile
            )
            {
                result = "A valid intervention side was not selected.";
                return false;
            }

            if (!IsEligibleColonyMap(map))
            {
                result =
                    "The current map is not a normal player colony map.";
                return false;
            }

            EnclaveInterventionMapComponent component =
                map.GetComponent<EnclaveInterventionMapComponent>();
            EnclaveInterventionRecord record =
                component?.GetLatestActiveRaidRecord();

            if (record == null)
            {
                result =
                    "No registered active hostile raid exists on this map.";
                return false;
            }

            if (
                record.State == EnclaveRaidInterventionState.Active ||
                record.State == EnclaveRaidInterventionState.Exiting
            )
            {
                result =
                    "Raid " +
                    record.Id +
                    " already has an enclave intervention.";
                return false;
            }

            return EvaluateRecord(
                component,
                record,
                forcedSide,
                out result
            );
        }

        public static bool IsOriginalRaidActive(
            Map map,
            EnclaveInterventionRecord record
        )
        {
            if (map == null || record?.RaidPawns == null)
            {
                return false;
            }

            foreach (Pawn pawn in record.RaidPawns)
            {
                if (
                    pawn != null &&
                    !pawn.Destroyed &&
                    !pawn.Dead &&
                    !pawn.Downed &&
                    !pawn.IsPrisonerOfColony &&
                    pawn.Faction != Faction.OfPlayerSilentFail &&
                    pawn.MapHeld == map
                )
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryGetLocalInterventionHostility(
            Thing first,
            Thing second,
            out bool hostile
        )
        {
            hostile = false;

            if (
                first == null ||
                second == null ||
                first == second ||
                first.Destroyed ||
                second.Destroyed
            )
            {
                return false;
            }

            Map map = first.MapHeld;

            if (map == null || second.MapHeld != map)
            {
                return false;
            }

            EnclaveInterventionMapComponent component =
                map.GetComponent<EnclaveInterventionMapComponent>();

            if (component?.Records == null)
            {
                return false;
            }

            EnclaveInterventionRecord firstRecord =
                FindActivePartyRecord(
                    component,
                    first as Pawn,
                    map
                );
            EnclaveInterventionRecord secondRecord =
                FindActivePartyRecord(
                    component,
                    second as Pawn,
                    map
                );

            if (firstRecord != null && secondRecord != null)
            {
                if (firstRecord == secondRecord)
                {
                    hostile = false;
                    return true;
                }

                // Distinct intervention parties do not inherit a local
                // relationship from each other.
                return false;
            }

            EnclaveInterventionRecord record =
                firstRecord ?? secondRecord;

            if (record == null)
            {
                return false;
            }

            Thing other = firstRecord != null ? second : first;
            Pawn otherPawn = other as Pawn;

            if (IsRecordedRaidParticipant(record, otherPawn, map))
            {
                hostile =
                    record.Side == EnclaveInterventionSide.Friendly;
                return true;
            }

            if (other.Faction == Faction.OfPlayerSilentFail)
            {
                hostile =
                    record.Side == EnclaveInterventionSide.Hostile;
                return true;
            }

            // Active intervention parties have no local hostility toward
            // entities outside their exact player/triggering-raid matrix.
            hostile = false;
            return true;
        }

        public static bool TryGetLocalInterventionHostility(
            Thing thing,
            Faction faction,
            out bool hostile
        )
        {
            hostile = false;

            if (
                thing == null ||
                faction == null ||
                thing.Destroyed ||
                thing.MapHeld == null
            )
            {
                return false;
            }

            Map map = thing.MapHeld;
            EnclaveInterventionMapComponent component =
                map.GetComponent<EnclaveInterventionMapComponent>();

            if (component?.Records == null)
            {
                return false;
            }

            EnclaveInterventionRecord partyRecord =
                FindActivePartyRecord(
                    component,
                    thing as Pawn,
                    map
                );

            if (partyRecord != null)
            {
                if (faction == Faction.OfPlayerSilentFail)
                {
                    hostile =
                        partyRecord.Side ==
                        EnclaveInterventionSide.Hostile;
                    return true;
                }

                if (RecordContainsRaidFaction(partyRecord, faction, map))
                {
                    hostile =
                        partyRecord.Side ==
                        EnclaveInterventionSide.Friendly;
                    return true;
                }

                // This overload is used to build faction-indexed target
                // caches. Exact Thing-to-Thing validation still owns the
                // final relationship, so unrelated factions remain out.
                hostile = false;
                return true;
            }

            if (!EnclaveFactionUtility.IsEnclaveFaction(faction))
            {
                return false;
            }

            Pawn pawn = thing as Pawn;
            bool playerOwned =
                thing.Faction == Faction.OfPlayerSilentFail;
            bool hasHostileParty = false;

            foreach (EnclaveInterventionRecord record in component.Records)
            {
                if (!IsActiveInterventionRecord(record))
                {
                    continue;
                }

                if (IsRecordedRaidParticipant(record, pawn, map))
                {
                    hostile =
                        record.Side ==
                        EnclaveInterventionSide.Friendly;
                    return true;
                }

                if (
                    playerOwned &&
                    record.Side == EnclaveInterventionSide.Hostile &&
                    HasActivePartyParticipant(record, map)
                )
                {
                    hasHostileParty = true;
                }
            }

            if (hasHostileParty)
            {
                hostile = true;
                return true;
            }

            return false;
        }

        public static Pawn FindClosestCombatTarget(
            Pawn attacker,
            int recordId
        )
        {
            EnclaveInterventionRecord record =
                ResolveRecord(attacker?.Map, recordId);

            if (!IsValidInterventionPawn(attacker, record))
            {
                return null;
            }

            Pawn closest = null;
            int closestDistance = int.MaxValue;

            if (record.Side == EnclaveInterventionSide.Friendly)
            {
                foreach (Pawn target in record.RaidPawns)
                {
                    ConsiderTarget(
                        attacker,
                        record,
                        target,
                        ref closest,
                        ref closestDistance
                    );
                }
            }
            else if (record.Side == EnclaveInterventionSide.Hostile)
            {
                foreach (Pawn target in attacker.Map.mapPawns.AllPawnsSpawned)
                {
                    ConsiderTarget(
                        attacker,
                        record,
                        target,
                        ref closest,
                        ref closestDistance
                    );
                }
            }

            return closest;
        }

        public static bool IsValidCombatTarget(
            Pawn attacker,
            Pawn target,
            int recordId
        )
        {
            EnclaveInterventionRecord record =
                ResolveRecord(attacker?.Map, recordId);

            if (
                !IsValidInterventionPawn(attacker, record) ||
                target == null ||
                target.Destroyed ||
                target.Dead ||
                target.Downed ||
                !target.Spawned ||
                target.Map != attacker.Map
            )
            {
                return false;
            }

            if (record.Side == EnclaveInterventionSide.Friendly)
            {
                return
                    record.ContainsRaidPawn(target) &&
                    !target.IsPrisonerOfColony &&
                    target.Faction != Faction.OfPlayerSilentFail;
            }

            return
                record.Side == EnclaveInterventionSide.Hostile &&
                target.Faction == Faction.OfPlayerSilentFail;
        }

        private static bool EvaluateRecord(
            EnclaveInterventionMapComponent component,
            EnclaveInterventionRecord record,
            EnclaveInterventionSide? forcedSide,
            out string result
        )
        {
            Map map = component?.ParentMap;
            result = null;

            if (
                map == null ||
                record == null ||
                !IsOriginalRaidActive(map, record)
            )
            {
                if (record != null)
                {
                    record.State =
                        EnclaveRaidInterventionState.NoIntervention;
                }

                result = "The registered raid is no longer active.";
                return false;
            }

            // Mark the raid evaluated before spawning. A failure cannot
            // silently reroll on a later tick or after save/reload.
            record.State = EnclaveRaidInterventionState.NoIntervention;

            List<EnclaveInterventionProfile> candidates =
                BuildCandidates(
                    map,
                    record.RollSeed,
                    includeNonIntervening: false
                );
            List<EnclaveInterventionProfile> successful =
                new List<EnclaveInterventionProfile>();

            foreach (EnclaveInterventionProfile candidate in candidates)
            {
                if (
                    forcedSide.HasValue
                        ? candidate.Side == forcedSide.Value
                        : candidate.RollSucceeded
                )
                {
                    successful.Add(candidate);
                }
            }

            if (successful.Count == 0)
            {
                result = forcedSide.HasValue
                    ? "No nearby enclave is eligible for a " +
                        forcedSide.Value +
                        " intervention."
                    : "No nearby enclave succeeded its intervention roll.";

                Log.Message(
                    "[IEE] Raid " +
                    record.Id +
                    " intervention evaluation completed with no " +
                    "intervention from " +
                    candidates.Count +
                    " eligible nearby enclave(s)."
                );
                return false;
            }

            successful.Sort(CompareCandidateStrength);
            EnclaveInterventionProfile selected = successful[0];

            if (!TrySpawnIntervention(map, record, selected))
            {
                result =
                    "The selected enclave intervention party could not " +
                    "be spawned safely.";
                return false;
            }

            result =
                (forcedSide.HasValue ? "Forced " : "Started ") +
                selected.Side +
                " intervention from " +
                selected.Camp.Data.Name +
                " with " +
                record.PartyPawns.Count +
                " fighter(s).";
            return true;
        }

        private static List<EnclaveInterventionProfile>
            BuildCandidates(
                Map map,
                int rollSeed,
                bool includeNonIntervening
            )
        {
            List<EnclaveInterventionProfile> candidates =
                new List<EnclaveInterventionProfile>();

            if (
                !IsEligibleColonyMap(map) ||
                map.Parent == null ||
                Find.WorldObjects?.AllWorldObjects == null
            )
            {
                return candidates;
            }

            foreach (WorldObject worldObject in
                Find.WorldObjects.AllWorldObjects)
            {
                PilgrimCamp camp = worldObject as PilgrimCamp;

                if (
                    camp?.Data == null ||
                    camp.Destroyed ||
                    !camp.Spawned ||
                    !camp.Tile.Valid
                )
                {
                    continue;
                }

                float distance = EnclaveProximityUtility
                    .GetDistanceInTiles(camp, map.Parent);
                EnclaveDistanceBand distanceBand =
                    EnclaveProximityUtility.GetDistanceBand(distance);

                if (distanceBand == EnclaveDistanceBand.None)
                {
                    continue;
                }

                EnclaveInterventionProfile profile =
                    EnclaveInterventionProfileUtility.CreateProfile(
                        camp,
                        distance,
                        distanceBand,
                        rollSeed
                    );

                if (
                    includeNonIntervening ||
                    profile.Side != EnclaveInterventionSide.None
                )
                {
                    candidates.Add(profile);
                }
            }

            candidates.Sort(
                (first, second) =>
                    (first.Camp?.ID ?? int.MaxValue).CompareTo(
                        second.Camp?.ID ?? int.MaxValue
                    )
            );
            return candidates;
        }

        private static int CompareCandidateStrength(
            EnclaveInterventionProfile first,
            EnclaveInterventionProfile second
        )
        {
            int comparison = second.PartyStrength.CompareTo(
                first.PartyStrength
            );

            if (comparison != 0)
            {
                return comparison;
            }

            comparison = second.ActivationChance.CompareTo(
                first.ActivationChance
            );

            if (comparison != 0)
            {
                return comparison;
            }

            comparison = first.DistanceInTiles.CompareTo(
                second.DistanceInTiles
            );

            if (comparison != 0)
            {
                return comparison;
            }

            return (first.Camp?.ID ?? int.MaxValue).CompareTo(
                second.Camp?.ID ?? int.MaxValue
            );
        }

        private static bool TrySpawnIntervention(
            Map map,
            EnclaveInterventionRecord record,
            EnclaveInterventionProfile profile
        )
        {
            Faction enclaveFaction =
                EnclaveFactionUtility.GetOrCreateFaction();

            if (
                enclaveFaction == null ||
                profile?.Camp?.Data == null ||
                profile.PartyStrength <= 0
            )
            {
                Log.Error(
                    "[IEE] Intervention spawning failed because its " +
                    "faction, source enclave, or party strength was invalid."
                );
                return false;
            }

            Ideo sourceIdeo;

            if (
                !EnclaveIdeologyUtility.TryGetOrCreateActualIdeo(
                    profile.Camp.Data,
                    out sourceIdeo
                )
            )
            {
                Log.Error(
                    "[IEE] Intervention spawning failed because " +
                    profile.Camp.Data.Name +
                    " has no valid persistent Ideo."
                );
                return false;
            }

            IntVec3 entryCell;

            if (!TryFindEntryCell(map, out entryCell))
            {
                Log.Warning(
                    "[IEE] No safe colony-map edge cell was available " +
                    "for an enclave intervention."
                );
                return false;
            }

            List<Pawn> party = new List<Pawn>();

            for (int i = 0; i < profile.PartyStrength; i++)
            {
                Pawn pawn = null;

                try
                {
                    PawnGenerationRequest request =
                        new PawnGenerationRequest(
                            PawnKindDefOf.Villager,
                            enclaveFaction,
                            PawnGenerationContext.NonPlayer,
                            map.Tile,
                            forceGenerateNewPawn: true,
                            canGeneratePawnRelations: false,
                            mustBeCapableOfViolence: true,
                            fixedIdeo: sourceIdeo,
                            forceRecruitable: false
                        );

                    pawn = PawnGenerator.GeneratePawn(request);

                    if (pawn == null)
                    {
                        continue;
                    }

                    EnsureBasicWeapon(pawn);

                    IntVec3 spawnCell;

                    if (
                        !TryFindPartySpawnCell(
                            map,
                            entryCell,
                            out spawnCell
                        )
                    )
                    {
                        DiscardUnspawnedPawn(pawn);
                        continue;
                    }

                    GenSpawn.Spawn(pawn, spawnCell, map);
                    party.Add(pawn);
                }
                catch (Exception exception)
                {
                    Log.Error(
                        "[IEE] Failed to generate or spawn an enclave " +
                        "intervention fighter: " +
                        exception
                    );

                    if (pawn != null && !pawn.Spawned)
                    {
                        DiscardUnspawnedPawn(pawn);
                    }
                }
            }

            if (party.Count == 0)
            {
                return false;
            }

            record.Activate(profile.Camp, profile.Side, party);

            try
            {
                EnsureCombatLord(map, record);
            }
            catch (Exception exception)
            {
                Log.Error(
                    "[IEE] Intervention fighters spawned, but their " +
                    "combat Lord could not be created: " +
                    exception
                );
                BeginExit(map, record);
                return false;
            }

            SendInterventionLetter(record, profile, party);

            Log.Message(
                "[IEE] Started " +
                profile.Side +
                " enclave intervention for raid " +
                record.Id +
                " from " +
                profile.Camp.Data.Name +
                ": distance " +
                profile.DistanceInTiles.ToString("0.#") +
                " (" +
                profile.DistanceBand +
                "), development " +
                profile.DevelopmentTier +
                ", reputation " +
                profile.ReputationTier +
                ", ideology " +
                profile.IdeologyType +
                ", archetype " +
                profile.Archetype +
                ", chance " +
                profile.ActivationChance.ToString("P0") +
                ", roll " +
                profile.ActivationRoll.ToString("0.000") +
                ", party " +
                party.Count +
                "."
            );
            return true;
        }

        private static void EnsureCombatLord(
            Map map,
            EnclaveInterventionRecord record
        )
        {
            List<Pawn> activeParty = GetActivePartyMembers(map, record);

            if (activeParty.Count == 0)
            {
                return;
            }

            Lord matchingLord = null;

            foreach (Pawn pawn in activeParty)
            {
                Lord lord = pawn.GetLord();
                LordJob_EnclaveIntervention job =
                    lord?.LordJob as LordJob_EnclaveIntervention;

                if (job?.RecordId == record.Id && lord.Map == map)
                {
                    matchingLord = lord;
                    break;
                }
            }

            if (matchingLord == null)
            {
                matchingLord = LordMaker.MakeNewLord(
                    EnclaveFactionUtility.GetOrCreateFaction(),
                    new LordJob_EnclaveIntervention(
                        record.Id,
                        record.Side
                    ),
                    map,
                    activeParty
                );
                return;
            }

            foreach (Pawn pawn in activeParty)
            {
                Lord currentLord = pawn.GetLord();

                if (currentLord == matchingLord)
                {
                    continue;
                }

                currentLord?.RemovePawn(pawn);
                matchingLord.AddPawn(pawn);
            }
        }

        private static void BeginExit(
            Map map,
            EnclaveInterventionRecord record
        )
        {
            if (
                map == null ||
                record == null ||
                record.State == EnclaveRaidInterventionState.Exiting
            )
            {
                return;
            }

            record.State = EnclaveRaidInterventionState.Exiting;
            record.ClearRaidPawns();
            EnsureExitLord(map, record);

            Log.Message(
                "[IEE] Original raid " +
                record.Id +
                " is no longer active. Surviving fighters from " +
                record.SourceEnclaveName +
                " are leaving the colony map."
            );
        }

        private static void EnsureExitLord(
            Map map,
            EnclaveInterventionRecord record
        )
        {
            List<Pawn> activeParty = GetPartyMembersOnMap(
                map,
                record,
                includeDowned: true
            );

            if (activeParty.Count == 0)
            {
                return;
            }

            Lord exitLord = null;

            foreach (Pawn pawn in activeParty)
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
                foreach (Pawn pawn in activeParty)
                {
                    Lord candidateLord = pawn.GetLord();
                    LordJob_EnclaveIntervention interventionJob =
                        candidateLord?.LordJob as
                            LordJob_EnclaveIntervention;

                    if (
                        interventionJob?.RecordId == record.Id &&
                        candidateLord.Map == map
                    )
                    {
                        candidateLord.SetJob(
                            new LordJob_ExitMapBest(
                                LocomotionUrgency.Jog,
                                canDig: false,
                                canDefendSelf: true
                            ),
                            false
                        );
                        exitLord = candidateLord;
                        break;
                    }
                }
            }

            if (exitLord == null)
            {
                foreach (Pawn pawn in activeParty)
                {
                    pawn.GetLord()?.RemovePawn(pawn);
                }

                LordMaker.MakeNewLord(
                    EnclaveFactionUtility.GetOrCreateFaction(),
                    new LordJob_ExitMapBest(
                        LocomotionUrgency.Jog,
                        canDig: false,
                        canDefendSelf: true
                    ),
                    map,
                    activeParty
                );
                return;
            }

            foreach (Pawn pawn in activeParty)
            {
                Lord currentLord = pawn.GetLord();

                if (currentLord == exitLord)
                {
                    continue;
                }

                currentLord?.RemovePawn(pawn);
                exitLord.AddPawn(pawn);
            }
        }

        private static List<Pawn> GetActivePartyMembers(
            Map map,
            EnclaveInterventionRecord record
        )
        {
            return GetPartyMembersOnMap(
                map,
                record,
                includeDowned: false
            );
        }

        private static List<Pawn> GetPartyMembersOnMap(
            Map map,
            EnclaveInterventionRecord record,
            bool includeDowned
        )
        {
            List<Pawn> active = new List<Pawn>();

            if (record?.PartyPawns == null)
            {
                return active;
            }

            foreach (Pawn pawn in record.PartyPawns)
            {
                if (
                    pawn != null &&
                    !pawn.Destroyed &&
                    !pawn.Dead &&
                    (includeDowned || !pawn.Downed) &&
                    pawn.Spawned &&
                    pawn.Map == map &&
                    !pawn.IsPrisonerOfColony &&
                    EnclaveFactionUtility.IsEnclaveFaction(pawn.Faction)
                )
                {
                    active.Add(pawn);
                }
            }

            return active;
        }

        private static bool HasActivePartyMembers(
            Map map,
            EnclaveInterventionRecord record
        )
        {
            return GetPartyMembersOnMap(
                map,
                record,
                includeDowned: true
            ).Count > 0;
        }

        private static bool IsValidInterventionPawn(
            Pawn pawn,
            EnclaveInterventionRecord record
        )
        {
            return
                pawn != null &&
                record != null &&
                record.State == EnclaveRaidInterventionState.Active &&
                record.ContainsPartyPawn(pawn) &&
                !pawn.Destroyed &&
                !pawn.Dead &&
                !pawn.Downed &&
                pawn.Spawned &&
                EnclaveFactionUtility.IsEnclaveFaction(pawn.Faction);
        }

        private static EnclaveInterventionRecord FindActivePartyRecord(
            EnclaveInterventionMapComponent component,
            Pawn pawn,
            Map map
        )
        {
            if (
                component?.Records == null ||
                pawn == null ||
                pawn.Destroyed ||
                !pawn.Spawned ||
                pawn.Map != map ||
                !EnclaveFactionUtility.IsEnclaveFaction(pawn.Faction)
            )
            {
                return null;
            }

            foreach (EnclaveInterventionRecord record in component.Records)
            {
                if (
                    IsActiveInterventionRecord(record) &&
                    record.ContainsPartyPawn(pawn)
                )
                {
                    return record;
                }
            }

            return null;
        }

        private static bool IsActiveInterventionRecord(
            EnclaveInterventionRecord record
        )
        {
            return
                record != null &&
                record.State == EnclaveRaidInterventionState.Active &&
                (
                    record.Side == EnclaveInterventionSide.Friendly ||
                    record.Side == EnclaveInterventionSide.Hostile
                );
        }

        private static bool IsRecordedRaidParticipant(
            EnclaveInterventionRecord record,
            Pawn pawn,
            Map map
        )
        {
            return
                pawn != null &&
                record?.ContainsRaidPawn(pawn) == true &&
                !pawn.Destroyed &&
                !pawn.Dead &&
                pawn.MapHeld == map &&
                !pawn.IsPrisonerOfColony &&
                pawn.Faction != Faction.OfPlayerSilentFail;
        }

        private static bool RecordContainsRaidFaction(
            EnclaveInterventionRecord record,
            Faction faction,
            Map map
        )
        {
            if (record?.RaidPawns == null || faction == null)
            {
                return false;
            }

            foreach (Pawn pawn in record.RaidPawns)
            {
                if (
                    IsRecordedRaidParticipant(record, pawn, map) &&
                    pawn.Faction == faction
                )
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasActivePartyParticipant(
            EnclaveInterventionRecord record,
            Map map
        )
        {
            if (record?.PartyPawns == null)
            {
                return false;
            }

            foreach (Pawn pawn in record.PartyPawns)
            {
                if (
                    pawn != null &&
                    !pawn.Destroyed &&
                    !pawn.Dead &&
                    pawn.Spawned &&
                    pawn.Map == map &&
                    EnclaveFactionUtility.IsEnclaveFaction(pawn.Faction)
                )
                {
                    return true;
                }
            }

            return false;
        }

        private static EnclaveInterventionRecord ResolveRecord(
            Map map,
            int recordId
        )
        {
            return map
                ?.GetComponent<EnclaveInterventionMapComponent>()
                ?.GetRecord(recordId);
        }

        private static void ConsiderTarget(
            Pawn attacker,
            EnclaveInterventionRecord record,
            Pawn target,
            ref Pawn closest,
            ref int closestDistance
        )
        {
            if (
                !IsValidCombatTarget(
                    attacker,
                    target,
                    record.Id
                ) ||
                !attacker.CanReach(
                    target,
                    PathEndMode.Touch,
                    Danger.Deadly
                )
            )
            {
                return;
            }

            int distance =
                (attacker.Position - target.Position)
                    .LengthHorizontalSquared;

            if (distance < closestDistance)
            {
                closest = target;
                closestDistance = distance;
            }
        }

        private static bool TryFindEntryCell(
            Map map,
            out IntVec3 entryCell
        )
        {
            return CellFinder.TryFindRandomEdgeCellWith(
                cell =>
                    cell.Standable(map) &&
                    !cell.Fogged(map) &&
                    cell.GetFirstPawn(map) == null &&
                    map.reachability.CanReachColony(cell),
                map,
                roadChance: 0.5f,
                out entryCell
            );
        }

        private static bool TryFindPartySpawnCell(
            Map map,
            IntVec3 entryCell,
            out IntVec3 spawnCell
        )
        {
            return CellFinder.TryFindRandomSpawnCellForPawnNear(
                entryCell,
                map,
                out spawnCell,
                firstTryWithRadius: 8,
                extraValidator: cell =>
                    cell.InBounds(map) &&
                    cell.Standable(map) &&
                    !cell.Fogged(map) &&
                    cell.GetFirstPawn(map) == null
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

        private static void DiscardUnspawnedPawn(Pawn pawn)
        {
            if (pawn == null || pawn.Spawned)
            {
                return;
            }

            try
            {
                Find.WorldPawns.PassToWorld(
                    pawn,
                    PawnDiscardDecideMode.Discard
                );
            }
            catch (Exception exception)
            {
                Log.Warning(
                    "[IEE] Could not discard an unspawned failed " +
                    "intervention pawn cleanly: " +
                    exception.Message
                );
            }
        }

        private static void SendInterventionLetter(
            EnclaveInterventionRecord record,
            EnclaveInterventionProfile profile,
            List<Pawn> party
        )
        {
            string enclaveName = profile.Camp.Data.Name;
            bool friendly =
                profile.Side == EnclaveInterventionSide.Friendly;
            TaggedString label = friendly
                ? "Enclave Intervention: " + enclaveName
                : "Hostile Enclave Intervention: " + enclaveName;
            TaggedString body = friendly
                ? "The " +
                    enclaveName +
                    " has sent fighters to assist your colony against " +
                    "the attackers."
                : "Fighters from the " +
                    enclaveName +
                    " have arrived to support the assault on your colony.";

            if (!profile.Flavor.NullOrEmpty())
            {
                body += "\n\n" + profile.Flavor;
            }

            Find.LetterStack.ReceiveLetter(
                label,
                body,
                friendly
                    ? LetterDefOf.PositiveEvent
                    : LetterDefOf.ThreatBig,
                new LookTargets(party)
            );
        }

        private static int GetPreviewSeed(Map map)
        {
            unchecked
            {
                return ((map?.uniqueID ?? 0) * 397) ^ 0x1EE701;
            }
        }
    }
}
