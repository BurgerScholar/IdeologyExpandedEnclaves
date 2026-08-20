using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveExpeditionMapPopulator
    {
        public static void PopulateNewSite(
            Map map,
            EnclaveExpeditionSite site
        )
        {
            EnclaveData sourceData = site?.SourceCamp?.Data;

            if (map == null || site == null || sourceData == null)
            {
                Log.Error(
                    "[IEE] Expedition map population requires a map, " +
                    "site, and source enclave."
                );
                return;
            }

            if (site.PartyInitialized)
            {
                Log.Message(
                    "[IEE] Reused the persisted expedition party for " +
                    site.Label +
                    "."
                );
                return;
            }

            IntVec3 center = CellFinder.RandomClosewalkCellNear(
                map.Center,
                map,
                6
            );
            Thing campfire = ThingMaker.MakeThing(ThingDefOf.Campfire);
            GenSpawn.Spawn(campfire, center, map);

            int partySize =
                EnclaveExpeditionUtility.GetPartySize(sourceData);
            EnclaveExpeditionCampLayout.Generate(
                map,
                center,
                partySize,
                sourceData,
                site.Purpose
            );

            Faction faction =
                EnclaveFactionUtility.GetOrCreateFaction();
            Ideo ideo;

            if (
                faction == null ||
                !EnclaveIdeologyUtility.TryGetOrCreateActualIdeo(
                    sourceData,
                    out ideo
                )
            )
            {
                Log.Error(
                    "[IEE] Expedition pawns require the hidden enclave " +
                    "faction and source Ideo."
                );
                return;
            }

            List<Pawn> members = new List<Pawn>();

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
                                site.Purpose ==
                                EnclaveExpeditionPurpose.Patrol,
                            fixedIdeo: ideo,
                            forceRecruitable: false
                        );

                    pawn = PawnGenerator.GeneratePawn(request);

                    if (
                        site.Purpose ==
                            EnclaveExpeditionPurpose.Patrol
                    )
                    {
                        EnsureBasicWeapon(pawn);
                    }

                    IntVec3 spawnCell =
                        CellFinder.RandomClosewalkCellNear(
                            center,
                            map,
                            8
                        );

                    GenSpawn.Spawn(pawn, spawnCell, map);
                    members.Add(pawn);
                }
                catch (Exception exception)
                {
                    Log.Error(
                        "[IEE] Failed to generate an expedition member: " +
                        exception
                    );

                    DiscardUnspawnedPawn(pawn);
                }
            }

            if (members.Count == 0)
            {
                Log.Error(
                    "[IEE] Expedition map generated without a valid " +
                    "temporary party."
                );
                return;
            }

            Pawn trader = members[0];

            if (
                !EnclaveTemporaryTraderUtility.Initialize(
                    trader,
                    map,
                    site.Purpose
                )
            )
            {
                Log.Error(
                    "[IEE] The expedition party spawned, but its " +
                    "temporary Trader could not be initialized."
                );
                trader = null;
            }

            site.SetTemporaryParty(members, trader);

            LordMaker.MakeNewLord(
                faction,
                new LordJob_DefendPoint(
                    center,
                    wanderRadius: 8f,
                    defendRadius: 20f,
                    isCaravanSendable: false,
                    addFleeToil: true
                ),
                map,
                members
            );

            Log.Message(
                "[IEE] Populated " +
                site.Label +
                " with " +
                members.Count +
                " temporary expedition member(s); source population " +
                "remains " +
                sourceData.Population +
                "."
            );
        }

        public static void CleanupTemporaryPawns(
            EnclaveExpeditionSite site
        )
        {
            if (site?.ExpeditionMembers?.Members == null)
            {
                return;
            }

            List<Pawn> members = new List<Pawn>();

            foreach (Pawn pawn in site.ExpeditionMembers.Members)
            {
                members.Add(pawn);
            }

            foreach (Pawn pawn in members)
            {
                if (
                    pawn == null ||
                    pawn.Destroyed ||
                    pawn.Dead ||
                    pawn.Faction == Faction.OfPlayer ||
                    pawn.IsPrisonerOfColony ||
                    pawn.HostFaction == Faction.OfPlayer
                )
                {
                    continue;
                }

                try
                {
                    pawn.GetLord()?.RemovePawn(pawn);

                    if (pawn.Spawned && pawn.Map == site.Map)
                    {
                        pawn.DeSpawn();
                    }

                    if (!pawn.Spawned)
                    {
                        Find.WorldPawns.PassToWorld(
                            pawn,
                            PawnDiscardDecideMode.Discard
                        );
                    }
                }
                catch (Exception exception)
                {
                    Log.Warning(
                        "[IEE] Could not discard temporary expedition " +
                        "pawn " +
                        pawn.GetUniqueLoadID() +
                        " cleanly: " +
                        exception.Message
                    );
                }
            }

            site.ClearTemporaryParty();
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

            Find.WorldPawns.PassToWorld(
                pawn,
                PawnDiscardDecideMode.Discard
            );
        }
    }
}
