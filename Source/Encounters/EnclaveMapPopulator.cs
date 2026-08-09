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
    }
}