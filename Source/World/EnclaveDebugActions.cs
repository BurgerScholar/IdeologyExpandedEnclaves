using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveDebugActions
    {
        [DebugAction(
            "Ideology Expanded: Enclaves",
            "Spawn pilgrim camp",
            allowedGameStates = AllowedGameStates.PlayingOnWorld
        )]
        public static void SpawnPilgrimCamp()
        {
            WorldObjectDef def =
                DefDatabase<WorldObjectDef>.GetNamed("IEE_PilgrimCamp");

            PilgrimCamp camp =
                 (PilgrimCamp)WorldObjectMaker.MakeWorldObject(def);

            camp.Data = EnclaveGenerator.Generate();

            int tile = TileFinder.RandomSettlementTileFor(
                Faction.OfPlayer,
                mustBeAutoChoosable: false
            );

            camp.Tile = tile;

            Find.WorldObjects.Add(camp);
            Find.WorldSelector.Select(camp);

            Messages.Message(
                "A pilgrim camp has appeared on the world map.",
                MessageTypeDefOf.PositiveEvent
            );
        }
    }
}