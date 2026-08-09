using RimWorld;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class RitualAreaPiece
    {
        public static void Generate(LayoutContext context)
        {
            IntVec3 focalCell;

            if (!TryPlaceFocalObject(context, out focalCell))
            {
                Log.Warning(
                    "[IEE] Could not place a ritual focal object."
                );

                return;
            }

            PlaceSeating(context, focalCell);
        }

        private static bool TryPlaceFocalObject(
            LayoutContext context,
            out IntVec3 focalCell
        )
        {
            IntVec3[] candidateCells =
            {
                context.Anchors.Ritual,
                context.Anchors.Ritual + new IntVec3(-1, 0, 0),
                context.Anchors.Ritual + new IntVec3(1, 0, 0),
                context.Anchors.Ritual + new IntVec3(0, 0, -1),
                context.Anchors.Ritual + new IntVec3(0, 0, 1)
            };

            foreach (IntVec3 cell in candidateCells)
            {
                if (!context.Zones.Ritual.Contains(cell) ||
                    !CanPlaceAt(cell, context.Map))
                {
                    continue;
                }

                Thing torchLamp = ThingMaker.MakeThing(
                    ThingDefOf.TorchLamp
                );

                GenSpawn.Spawn(
                    torchLamp,
                    cell,
                    context.Map
                );

                focalCell = cell;
                return true;
            }

            focalCell = IntVec3.Invalid;
            return false;
        }

        private static void PlaceSeating(
            LayoutContext context,
            IntVec3 focalCell
        )
        {
            IntVec3[] stoolCells =
            {
                focalCell + new IntVec3(-2, 0, 0),
                focalCell + new IntVec3(2, 0, 0),
                focalCell + new IntVec3(0, 0, -2),
                focalCell + new IntVec3(0, 0, 2)
            };

            int placed = 0;

            foreach (IntVec3 cell in stoolCells)
            {
                if (!context.Zones.Ritual.Contains(cell) ||
                    !CanPlaceAt(cell, context.Map))
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

                placed++;
            }

            Log.Message(
                "[IEE] Placed a ritual focal object and " +
                placed +
                " stools."
            );
        }

        private static bool CanPlaceAt(
            IntVec3 cell,
            Map map
        )
        {
            return
                cell.InBounds(map) &&
                cell.Standable(map) &&
                cell.GetThingList(map).Count == 0;
        }
    }
}
