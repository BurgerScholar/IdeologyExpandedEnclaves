using RimWorld;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class SleepingAreaPiece
    {
        public static void Generate(LayoutContext context)
        {
            int bedCount = context.Population;

            if (bedCount < 1)
            {
                bedCount = 1;
            }

            if (bedCount > 12)
            {
                bedCount = 12;
            }

            int columns = 4;
            int rows = (bedCount + columns - 1) / columns;

            int placed = 0;

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    if (placed >= bedCount)
                    {
                        break;
                    }

                    IntVec3 startCell =
                        context.Zones.Sleeping.Area.Min;

                    IntVec3 cell =
                        startCell +
                        new IntVec3(
                             column * 2,
                             0,
                             row * 2
                        );

                   if (!context.Zones.Sleeping.Contains(cell) ||
                      !CanPlaceAt(cell, context.Map))
                    {
                      continue;
          } 

                    Thing bed =
                        ThingMaker.MakeThing(
                            ThingDefOf.Bed,
                            ThingDefOf.WoodLog
                        );

                    GenSpawn.Spawn(
                        bed,
                        cell,
                        context.Map,
                        Rot4.North
                    );

                    placed++;
                }
            }

            Log.Message(
                "[IEE] Placed " +
                placed +
                " beds for " +
                context.Population +
                " pilgrims."
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
                cell.GetFirstBuilding(map) == null;
        }
    }
}