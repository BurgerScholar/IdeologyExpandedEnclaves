using RimWorld;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class GatheringAreaPiece
    {
        public static void Generate(LayoutContext context)
        {
            PlaceTable(context);
            PlaceStools(context);
        }

        private static void PlaceTable(LayoutContext context)
        {
            IntVec3 tableCell =
                context.Anchors.Gathering + new IntVec3(0, 0, 4);

            if (!CanPlaceAt(tableCell, context.Map))
            {
                return;
            }

            Thing table = ThingMaker.MakeThing(
                ThingDefOf.Table1x2c,
                ThingDefOf.WoodLog
            );

            GenSpawn.Spawn(
                table,
                tableCell,
                context.Map,
                Rot4.East
            );
        }

        private static void PlaceStools(LayoutContext context)
        {
            IntVec3[] stoolCells =
            {
                context.Anchors.Gathering + new IntVec3(-2, 0, 4),
                context.Anchors.Gathering + new IntVec3(2, 0, 4),
                context.Anchors.Gathering + new IntVec3(0, 0, 2),
                context.Anchors.Gathering + new IntVec3(0, 0, 6)
            };

            foreach (IntVec3 cell in stoolCells)
            {
                if (!CanPlaceAt(cell, context.Map))
                {
                    continue;
                }

                Thing stool = ThingMaker.MakeThing(
                    ThingDefOf.Stool,
                    ThingDefOf.WoodLog
                );

                GenSpawn.Spawn(
                    stool,
                    cell,
                    context.Map
                );
            }
        }

        private static bool CanPlaceAt(IntVec3 cell, Map map)
        {
            return
                cell.InBounds(map) &&
                cell.Standable(map) &&
                cell.GetFirstBuilding(map) == null;
        }
    }
}