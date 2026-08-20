using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class SleepingAreaPiece
    {
        public static void Generate(LayoutContext context)
        {
            int bedCount = Math.Max(
                1,
                Math.Min(context.Population, 12)
            );
            EnclaveDevelopmentVisualProfile profile =
                context.VisualProfile;
            List<IntVec3> bedCells =
                EnclaveLayoutPlacementUtility.GetCenteredGridCells(
                    context.Anchors.Sleeping,
                    bedCount,
                    profile.SleepingColumns,
                    profile.SleepingSpacing,
                    profile.StaggerSleepingRows
                );
            ThingDef sleepingDef = profile.SleepingDef;
            ThingDef stuffDef =
                EnclaveDevelopmentVisualUtility.GetFurnitureStuff(
                    profile,
                    sleepingDef
                );
            int bedsPlaced = 0;

            foreach (IntVec3 cell in bedCells)
            {
                Thing bed;

                if (
                    EnclaveLayoutPlacementUtility.TryPlaceBuilding(
                        context,
                        context.Zones.Sleeping,
                        sleepingDef,
                        stuffDef,
                        Rot4.North,
                        new[] { cell },
                        out bed
                    )
                )
                {
                    bedsPlaced++;
                }
            }

            int lightsPlaced = PlaceLights(context);

            Log.Message(
                "[IEE] Sleeping area: placed " +
                bedsPlaced +
                "/" +
                bedCount +
                " " +
                sleepingDef.label +
                " spaces for " +
                context.Population +
                " pilgrims and " +
                lightsPlaced +
                "/" +
                profile.SleepingLightCount +
                " lights."
            );
        }

        private static int PlaceLights(LayoutContext context)
        {
            int target = context.VisualProfile.SleepingLightCount;
            List<IntVec3> candidates =
                EnclaveLayoutPlacementUtility.GetCornerCells(
                    context.Zones.Sleeping
                );
            int placed = 0;

            for (int index = 0; index < target; index++)
            {
                Thing light;

                if (
                    EnclaveLayoutPlacementUtility.TryPlaceBuilding(
                        context,
                        context.Zones.Sleeping,
                        ThingDefOf.TorchLamp,
                        null,
                        Rot4.North,
                        new[]
                        {
                            candidates[index % candidates.Count]
                        },
                        out light
                    )
                )
                {
                    placed++;
                }
            }

            return placed;
        }
    }
}
