using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveMapPopulator
    {
        public static void PopulateNewCamp(
            Map map,
            PilgrimCamp camp
        )
        {
            if (map == null)
            {
                Log.Error(
                    "[Ideology Expanded: Enclaves] Map was null."
                );

                return;
            }

            IntVec3 campCenter =
                CellFinder.RandomClosewalkCellNear(
                    map.Center,
                    map,
                    8
                );

            Thing campfire =
                ThingMaker.MakeThing(ThingDefOf.Campfire);

            GenSpawn.Spawn(
                campfire,
                campCenter,
                map
            );
            int population = camp?.Data?.Population ?? 6;

    BasicPilgrimCampLayout.Generate(
    map,
    campCenter,
    population,
    camp.Data
);

            Faction pilgrimFaction =
                Find.FactionManager.FirstFactionOfDef(
                    FactionDefOf.OutlanderCivil
                );

            if (pilgrimFaction == null)
            {
                Log.Error(
                    "[Ideology Expanded: Enclaves] " +
                    "No civil outlander faction exists."
                );

                return;
            }

            int pilgrimCount =
                camp?.Data?.Population ?? 6;

            pilgrimCount = Math.Max(
                1,
                Math.Min(pilgrimCount, 12)
            );

            List<Pawn> pilgrims = new List<Pawn>();

            for (int i = 0; i < pilgrimCount; i++)
            {
                Pawn pilgrim =
                    PawnGenerator.GeneratePawn(
                        PawnKindDefOf.Villager,
                        pilgrimFaction
                    );

                IntVec3 pilgrimCell =
                    CellFinder.RandomClosewalkCellNear(
                        campCenter,
                        map,
                        8
                    );

                GenSpawn.Spawn(
                    pilgrim,
                    pilgrimCell,
                    map
                );

                pilgrims.Add(pilgrim);
            }

            if (pilgrims.Count > 0)
            {
                AssignLeader(camp, pilgrims);
                AssignTrader(camp, pilgrims, map);
                AssignRecruiter(camp, pilgrims);

                LordMaker.MakeNewLord(
                    pilgrimFaction,
                    new LordJob_DefendPoint(
                        campCenter,
                        wanderRadius: 10f,
                        defendRadius: 24f,
                        isCaravanSendable: false,
                        addFleeToil: true
                    ),
                    map,
                    pilgrims
                );

                Pawn firstPilgrim = pilgrims[0];

                Messages.Message(
                    pilgrims.Count +
                    " neutral pilgrims are gathered at the enclave.",
                    firstPilgrim,
                    MessageTypeDefOf.PositiveEvent
                );

                CameraJumper.TryJump(
                    firstPilgrim.Position,
                    map
                );
            }

            Log.Message(
                "[Ideology Expanded: Enclaves] Spawned " +
                pilgrims.Count +
                " neutral pilgrims with camp behavior."
            );
        }

        private static void AssignLeader(
            PilgrimCamp camp,
            List<Pawn> pilgrims
        )
        {
            Pawn leader = pilgrims.Find(
                pawn =>
                    pawn != null &&
                    !pawn.Dead &&
                    pawn.RaceProps.Humanlike
            );

            if (leader == null)
            {
                Log.Error(
                    "[IEE] Could not assign the enclave Leader role."
                );

                return;
            }

            string leaderName = camp?.Data?.Leader;

            if (!leaderName.NullOrEmpty())
            {
                leader.Name = new NameSingle(leaderName);
            }

            if (camp.PawnRoles == null)
            {
                camp.PawnRoles = new EnclavePawnRoleAssignments();
            }

            camp.PawnRoles.Assign(
                EnclavePawnRole.Leader,
                leader
            );

            Log.Message(
                "[IEE] Assigned Leader role to " +
                leader.LabelShort +
                " (" +
                leader.GetUniqueLoadID() +
                ") for " +
                (camp.Data?.Name ?? "an enclave") +
                "."
            );
        }

        private static void AssignTrader(
            PilgrimCamp camp,
            List<Pawn> pilgrims,
            Map map
        )
        {
            Pawn leader = camp.PawnRoles?.GetPawn(
                EnclavePawnRole.Leader
            );

            if (leader == null)
            {
                Log.Error(
                    "[IEE] Could not assign the Trader role " +
                    "because the enclave Leader was missing."
                );

                return;
            }

            Pawn trader = pilgrims.Find(
                pawn =>
                    pawn != null &&
                    pawn != leader &&
                    !pawn.Dead &&
                    pawn.RaceProps.Humanlike
            );

            if (trader == null)
            {
                Log.Warning(
                    "[IEE] Enclave population was too small " +
                    "to assign a distinct Trader."
                );

                return;
            }

            if (!InitializeTrader(trader, map))
            {
                Log.Error(
                    "[IEE] Trader role assignment failed because " +
                    "vanilla trader initialization was invalid."
                );

                return;
            }

            camp.PawnRoles.Assign(
                EnclavePawnRole.Trader,
                trader
            );

            Log.Message(
                "[IEE] Assigned Trader role to " +
                trader.LabelShort +
                " (" +
                trader.GetUniqueLoadID() +
                ") for " +
                (camp.Data?.Name ?? "an enclave") +
                "."
            );
        }

        private static bool InitializeTrader(
            Pawn trader,
            Map map
        )
        {
            if (trader == null)
            {
                Log.Error(
                    "[IEE] Cannot initialize a null Trader pawn."
                );

                return false;
            }

            if (map == null)
            {
                Log.Error(
                    "[IEE] Cannot initialize Trader " +
                    trader.LabelShort +
                    " without a map."
                );

                return false;
            }

            TraderKindDef traderKind =
                DefDatabase<TraderKindDef>.GetNamedSilentFail(
                    "IEE_PilgrimCampTrader"
                );

            if (traderKind == null)
            {
                Log.Error(
                    "[IEE] Missing TraderKindDef " +
                    "IEE_PilgrimCampTrader."
                );

                return false;
            }

            if (trader.mindState == null)
            {
                Log.Error(
                    "[IEE] Trader " +
                    trader.LabelShort +
                    " has no mind state."
                );

                return false;
            }

            trader.mindState.wantsToTradeWithColony = true;
            PawnComponentsUtility.AddAndRemoveDynamicComponents(trader);

            if (trader.trader == null)
            {
                Log.Error(
                    "[IEE] RimWorld did not create a pawn trader " +
                    "tracker for " +
                    trader.LabelShort +
                    "."
                );

                return false;
            }

            if (trader.trader.traderKind == traderKind)
            {
                Log.Message(
                    "[IEE] Trader stock already initialized for " +
                    trader.LabelShort +
                    "."
                );

                return true;
            }

            if (trader.inventory == null)
            {
                Log.Error(
                    "[IEE] Trader " +
                    trader.LabelShort +
                    " has no inventory tracker."
                );

                return false;
            }

            if (traderKind.stockGenerators.NullOrEmpty())
            {
                Log.Error(
                    "[IEE] TraderKindDef IEE_PilgrimCampTrader " +
                    "has no stock generators."
                );

                return false;
            }

            trader.trader.traderKind = traderKind;

            int stackCount = 0;
            int itemCount = 0;

            foreach (StockGenerator stockGenerator in
                traderKind.stockGenerators)
            {
                foreach (Thing thing in stockGenerator.GenerateThings(
                    map.Tile,
                    trader.Faction
                ))
                {
                    if (trader.inventory.innerContainer.TryAdd(thing))
                    {
                        stackCount++;
                        itemCount += thing.stackCount;
                    }
                    else
                    {
                        Log.Warning(
                            "[IEE] Could not add " +
                            thing.Label +
                            " to trader inventory."
                        );
                    }
                }
            }

            Log.Message(
                "[IEE] Initialized vanilla trader " +
                trader.LabelShort +
                " with " +
                stackCount +
                " stock stacks (" +
                itemCount +
                " total items)."
            );

            return true;
        }

        private static void AssignRecruiter(
            PilgrimCamp camp,
            List<Pawn> pilgrims
        )
        {
            Pawn leader = camp.PawnRoles?.GetPawn(
                EnclavePawnRole.Leader
            );
            Pawn trader = camp.PawnRoles?.GetPawn(
                EnclavePawnRole.Trader
            );

            if (leader == null || trader == null)
            {
                Log.Warning(
                    "[IEE] Could not assign a distinct Recruiter " +
                    "because the Leader or Trader assignment was missing."
                );

                return;
            }

            Pawn recruiter = pilgrims.Find(
                pawn =>
                    pawn != null &&
                    pawn != leader &&
                    pawn != trader &&
                    !pawn.Dead &&
                    pawn.RaceProps.Humanlike
            );

            if (recruiter == null)
            {
                Log.Warning(
                    "[IEE] Enclave population was too small " +
                    "to assign a distinct Recruiter."
                );

                return;
            }

            camp.PawnRoles.Assign(
                EnclavePawnRole.Recruiter,
                recruiter
            );

            Log.Message(
                "[IEE] Assigned Recruiter role to " +
                recruiter.LabelShort +
                " (" +
                recruiter.GetUniqueLoadID() +
                ") for " +
                (camp.Data?.Name ?? "an enclave") +
                "."
            );
        }
    }
}
