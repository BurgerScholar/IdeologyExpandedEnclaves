using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveFactionUtility
    {
        public const string FactionDefName = "IEE_EnclavePilgrims";

        private sealed class DefendPointSettings
        {
            public IntVec3 Point;
            public float? WanderRadius;
            public float? DefendRadius;
            public bool IsCaravanSendable;
            public bool AddFleeToil;

            public LordJob_DefendPoint CreateJob()
            {
                return new LordJob_DefendPoint(
                    Point,
                    WanderRadius,
                    DefendRadius,
                    IsCaravanSendable,
                    AddFleeToil
                );
            }
        }

        public static Faction GetOrCreateFaction()
        {
            FactionManager factionManager = Find.FactionManager;
            FactionDef factionDef =
                DefDatabase<FactionDef>.GetNamedSilentFail(
                    FactionDefName
                );

            if (factionManager == null || factionDef == null)
            {
                Log.Error(
                    "[IEE] Cannot resolve the dedicated enclave faction " +
                    "because its manager or FactionDef is unavailable."
                );
                return null;
            }

            Faction faction = null;

            foreach (
                Faction existing in
                factionManager.AllFactionsListForReading
            )
            {
                if (existing?.def == factionDef)
                {
                    if (faction == null)
                    {
                        faction = existing;
                    }
                    else
                    {
                        Log.ErrorOnce(
                            "[IEE] More than one " +
                            FactionDefName +
                            " faction exists. The first instance will be " +
                            "used; no faction was removed.",
                            162435771
                        );
                    }
                }
            }

            if (faction == null)
            {
                faction = FactionGenerator.NewGeneratedFaction(
                    new FactionGeneratorParms(
                        factionDef,
                        default(IdeoGenerationParms),
                        hidden: true
                    )
                );
                factionManager.Add(faction);

                Log.Message(
                    "[IEE] Created the persistent dedicated enclave " +
                    "faction " +
                    faction.GetUniqueLoadID() +
                    "."
                );
            }

            NormalizeMechanicalNeutrality(faction);
            return faction;
        }

        public static bool IsEnclaveFaction(Faction faction)
        {
            return faction?.def?.defName == FactionDefName;
        }

        public static void EnsureCampFaction(PilgrimCamp camp)
        {
            if (camp == null)
            {
                return;
            }

            Faction enclaveFaction = GetOrCreateFaction();
            Map map = camp.Map;

            if (enclaveFaction == null || map == null)
            {
                return;
            }

            if (camp.PawnMembers == null)
            {
                camp.PawnMembers = new EnclavePawnMembers();
            }

            bool recoveredLegacyMembership = false;

            if (!camp.PawnMembers.IsInitialized)
            {
                RecoverLegacyMembership(camp);
                recoveredLegacyMembership = true;
            }

            List<Pawn> registeredMembers =
                SnapshotRegisteredMembers(camp);
            DefendPointSettings settings =
                CreateProductionDefendSettings(map);
            int migratedCount = 0;

            foreach (Pawn pawn in registeredMembers)
            {
                if (pawn == null)
                {
                    continue;
                }

                if (pawn.Faction == Faction.OfPlayer)
                {
                    camp.PawnMembers.Remove(pawn);
                    continue;
                }

                if (
                    pawn.Destroyed ||
                    pawn.RaceProps == null ||
                    !pawn.RaceProps.Humanlike ||
                    pawn.Faction == enclaveFaction
                )
                {
                    continue;
                }

                try
                {
                    pawn.SetFaction(enclaveFaction);
                    migratedCount++;
                }
                catch (Exception exception)
                {
                    Log.Error(
                        "[IEE] Could not migrate registered pilgrim " +
                        pawn.GetUniqueLoadID() +
                        " to the dedicated enclave faction: " +
                        exception
                    );
                }
            }

            if (migratedCount > 0)
            {
                RebuildDefendLordIfNeeded(
                    camp,
                    enclaveFaction,
                    settings
                );

                if (!EnclaveTradeService.RestoreTraderAfterFactionChange(camp))
                {
                    Log.Warning(
                        "[IEE] The designated Trader tracker could not be " +
                        "restored after faction migration for " +
                        (camp.Data?.Name ?? "an enclave") +
                        ". Existing inventory was left unchanged."
                    );
                }

                Log.Message(
                    "[IEE] Migrated " +
                    migratedCount +
                    " registered pilgrim(s) at " +
                    (camp.Data?.Name ?? "an enclave") +
                    " to the dedicated enclave faction. " +
                    "Roles, candidates, inventories, and persistent pawn " +
                    "data were retained."
                );
            }
            else if (recoveredLegacyMembership)
            {
                Log.Message(
                    "[IEE] Recovered persistent enclave membership for " +
                    (camp.Data?.Name ?? "an enclave") +
                    "; its registered pawns already used the dedicated " +
                    "faction or no eligible legacy pawns remained."
                );
            }
        }

        private static void NormalizeMechanicalNeutrality(Faction faction)
        {
            if (faction == null)
            {
                return;
            }

            faction.hidden = true;
            faction.temporary = false;
            faction.factionHostileOnHarmByPlayer = false;

            Faction playerFaction = Faction.OfPlayerSilentFail;

            if (
                playerFaction != null &&
                faction.RelationKindWith(playerFaction) !=
                    FactionRelationKind.Neutral
            )
            {
                faction.SetRelationDirect(
                    playerFaction,
                    FactionRelationKind.Neutral,
                    canSendHostilityLetter: false
                );

                Log.Warning(
                    "[IEE] Restored the dedicated enclave faction's " +
                    "mechanical relation with the player to Neutral."
                );
            }
        }

        private static void RecoverLegacyMembership(PilgrimCamp camp)
        {
            List<Pawn> recovered = new List<Pawn>();

            AddKnownPawn(
                recovered,
                camp.PawnRoles?.GetPawn(EnclavePawnRole.Leader)
            );
            AddKnownPawn(
                recovered,
                camp.PawnRoles?.GetPawn(EnclavePawnRole.Trader)
            );
            AddKnownPawn(
                recovered,
                camp.PawnRoles?.GetPawn(EnclavePawnRole.Recruiter)
            );

            if (camp.RecruitmentCandidates?.Candidates != null)
            {
                foreach (
                    Pawn candidate in
                    camp.RecruitmentCandidates.Candidates
                )
                {
                    AddKnownPawn(recovered, candidate);
                }
            }

            Faction sourceFaction = FindSourceFaction(recovered);
            Lord legacyLord = FindMemberLord(recovered, null);

            if (legacyLord == null)
            {
                legacyLord = FindUnambiguousLegacyDefendLord(
                    camp,
                    sourceFaction
                );
            }

            if (legacyLord != null)
            {
                foreach (Pawn pawn in legacyLord.ownedPawns)
                {
                    if (IsLegacyCampPawn(camp, pawn, sourceFaction))
                    {
                        AddKnownPawn(recovered, pawn);
                    }
                }
            }

            if (camp.Map != null && camp.HarmPenalties != null)
            {
                foreach (Pawn pawn in camp.Map.mapPawns.AllPawnsSpawned)
                {
                    if (
                        camp.HarmPenalties.HasRecordedIncident(pawn) &&
                        IsLegacyCampPawn(camp, pawn, sourceFaction)
                    )
                    {
                        AddKnownPawn(recovered, pawn);
                    }
                }
            }

            recovered.RemoveAll(
                pawn => pawn == null || pawn.Faction == Faction.OfPlayer
            );
            camp.PawnMembers.SetMembers(recovered);

            int expectedPopulation = camp.Data?.Population ?? 0;

            Log.Message(
                "[IEE] Recovered " +
                recovered.Count +
                " legacy pilgrim member reference(s) for " +
                (camp.Data?.Name ?? "an enclave") +
                "."
            );

            if (
                expectedPopulation > 0 &&
                recovered.Count < expectedPopulation
            )
            {
                Log.Warning(
                    "[IEE] Legacy membership recovery found " +
                    recovered.Count +
                    " of an expected " +
                    expectedPopulation +
                    " pilgrim(s) for " +
                    (camp.Data?.Name ?? "an enclave") +
                    ". Ambiguous pawns were intentionally not converted."
                );
            }
        }

        private static bool IsLegacyCampPawn(
            PilgrimCamp camp,
            Pawn pawn,
            Faction sourceFaction
        )
        {
            return
                pawn != null &&
                pawn.Faction != null &&
                pawn.Faction != Faction.OfPlayer &&
                (sourceFaction == null || pawn.Faction == sourceFaction) &&
                pawn.RaceProps != null &&
                pawn.RaceProps.Humanlike &&
                (pawn.Map == null || pawn.Map == camp.Map);
        }

        private static Faction FindSourceFaction(List<Pawn> pawns)
        {
            foreach (Pawn pawn in pawns)
            {
                if (
                    pawn?.Faction != null &&
                    pawn.Faction != Faction.OfPlayer
                )
                {
                    return pawn.Faction;
                }
            }

            return null;
        }

        private static Lord FindMemberLord(
            List<Pawn> members,
            Faction factionToExclude
        )
        {
            foreach (Pawn pawn in members)
            {
                Lord lord = pawn?.GetLord();

                if (
                    lord != null &&
                    (factionToExclude == null ||
                        lord.faction != factionToExclude)
                )
                {
                    return lord;
                }
            }

            return null;
        }

        private static Lord FindUnambiguousLegacyDefendLord(
            PilgrimCamp camp,
            Faction sourceFaction
        )
        {
            if (camp?.Map?.lordManager?.lords == null)
            {
                return null;
            }

            Lord match = null;

            foreach (Lord lord in camp.Map.lordManager.lords)
            {
                if (
                    lord == null ||
                    !(lord.LordJob is LordJob_DefendPoint) ||
                    (sourceFaction != null &&
                        lord.faction != sourceFaction)
                )
                {
                    continue;
                }

                bool hasEligibleMember = false;

                foreach (Pawn pawn in lord.ownedPawns)
                {
                    if (IsLegacyCampPawn(camp, pawn, sourceFaction))
                    {
                        hasEligibleMember = true;
                        break;
                    }
                }

                if (!hasEligibleMember)
                {
                    continue;
                }

                if (match != null)
                {
                    Log.Warning(
                        "[IEE] More than one legacy defend-point Lord " +
                        "could belong to " +
                        (camp.Data?.Name ?? "an enclave") +
                        ". No unlinked Lord was migrated."
                    );
                    return null;
                }

                match = lord;
            }

            if (match != null)
            {
                Log.Message(
                    "[IEE] Identified the legacy enclave defend-point " +
                    "Lord by its unique camp-map signature."
                );
            }

            return match;
        }

        private static DefendPointSettings CreateProductionDefendSettings(
            Map map
        )
        {
            return new DefendPointSettings
            {
                Point = FindCampCenter(map),
                WanderRadius = 10f,
                DefendRadius = 24f,
                IsCaravanSendable = false,
                AddFleeToil = true
            };
        }

        private static void RebuildDefendLordIfNeeded(
            PilgrimCamp camp,
            Faction enclaveFaction,
            DefendPointSettings settings
        )
        {
            List<Pawn> activeMembers = new List<Pawn>();

            foreach (Pawn pawn in camp.PawnMembers.Members)
            {
                if (
                    pawn != null &&
                    !pawn.Dead &&
                    !pawn.Downed &&
                    pawn.Spawned &&
                    pawn.Map == camp.Map &&
                    pawn.Faction == enclaveFaction
                )
                {
                    activeMembers.Add(pawn);
                }
            }

            if (activeMembers.Count == 0)
            {
                return;
            }

            foreach (Pawn pawn in activeMembers)
            {
                Lord existingLord = pawn.GetLord();

                if (existingLord?.faction == enclaveFaction)
                {
                    return;
                }
            }

            LordMaker.MakeNewLord(
                enclaveFaction,
                settings.CreateJob(),
                camp.Map,
                activeMembers
            );

            Log.Message(
                "[IEE] Rebuilt the existing defend-point Lord for " +
                (camp.Data?.Name ?? "an enclave") +
                " after faction migration."
            );
        }

        private static IntVec3 FindCampCenter(Map map)
        {
            if (map != null)
            {
                List<Thing> campfires =
                    map.listerThings.ThingsOfDef(ThingDefOf.Campfire);

                if (campfires != null && campfires.Count > 0)
                {
                    return campfires[0].Position;
                }

                return map.Center;
            }

            return IntVec3.Invalid;
        }

        private static List<Pawn> SnapshotRegisteredMembers(
            PilgrimCamp camp
        )
        {
            List<Pawn> snapshot = new List<Pawn>();

            if (camp.PawnMembers?.Members == null)
            {
                return snapshot;
            }

            foreach (Pawn pawn in camp.PawnMembers.Members)
            {
                if (pawn != null && !snapshot.Contains(pawn))
                {
                    snapshot.Add(pawn);
                }
            }

            return snapshot;
        }

        private static void AddKnownPawn(
            List<Pawn> pawns,
            Pawn pawn
        )
        {
            if (pawn != null && !pawns.Contains(pawn))
            {
                pawns.Add(pawn);
            }
        }
    }
}
