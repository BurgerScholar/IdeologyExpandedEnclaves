using RimWorld;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class StorageAreaPiece
    {
        public static void Generate(LayoutContext context)
        {
            ThingDef[] resourceDefs =
            {
                ThingDefOf.WoodLog,
                ThingDefOf.Steel,
                ThingDefOf.MealSurvivalPack
            };

            int[] stackCounts =
            {
                75,
                50,
                10
            };

            IntVec3[] candidateCells =
            {
                context.Anchors.Storage + new IntVec3(-2, 0, -2),
                context.Anchors.Storage + new IntVec3(0, 0, -2),
                context.Anchors.Storage + new IntVec3(2, 0, -2),
                context.Anchors.Storage + new IntVec3(-2, 0, 0),
                context.Anchors.Storage,
                context.Anchors.Storage + new IntVec3(2, 0, 0),
                context.Anchors.Storage + new IntVec3(-2, 0, 2),
                context.Anchors.Storage + new IntVec3(0, 0, 2),
                context.Anchors.Storage + new IntVec3(2, 0, 2)
            };

            int resourceIndex = 0;

            foreach (IntVec3 cell in candidateCells)
            {
                if (resourceIndex >= resourceDefs.Length)
                {
                    break;
                }

                if (!context.Zones.Storage.Contains(cell) ||
                    !CanPlaceAt(cell, context.Map))
                {
                    continue;
                }

                Thing resource = ThingMaker.MakeThing(
                    resourceDefs[resourceIndex]
                );

                resource.stackCount = stackCounts[resourceIndex];

                GenSpawn.Spawn(
                    resource,
                    cell,
                    context.Map
                );

                resourceIndex++;
            }

            Log.Message(
                "[IEE] Placed " +
                resourceIndex +
                " resource stacks in storage."
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
