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
                AssignTrader(camp, pilgrims);

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
            List<Pawn> pilgrims
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
    }
}
