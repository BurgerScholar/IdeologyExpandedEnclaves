using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class GatheringAreaPiece
    {
        public static void Generate(LayoutContext context)
        {
            EnclaveDevelopmentVisualProfile profile =
                context.VisualProfile;
            ThingDef tableDef = profile.GatheringTableDef;
            ThingDef tableStuff =
                EnclaveDevelopmentVisualUtility.GetFurnitureStuff(
                    profile,
                    tableDef
                );
            Thing table;
            bool placedTable =
                EnclaveLayoutPlacementUtility.TryPlaceBuilding(
                    context,
                    context.Zones.Gathering,
                    tableDef,
                    tableStuff,
                    Rot4.North,
                    new[] { context.Anchors.Gathering },
                    out table
                );

            int seatsPlaced = PlaceSeats(context);
            int lightsPlaced = PlaceLights(context);

            Log.Message(
                "[IEE] Gathering area: " +
                (placedTable ? tableDef.label : "no table") +
                ", " +
                seatsPlaced +
                "/" +
                profile.GatheringSeatCount +
                " seats, and " +
                lightsPlaced +
                "/" +
                profile.GatheringLightCount +
                " extra lights for " +
                EnclaveDevelopmentUtility.GetDisplayName(profile.Tier) +
                "."
            );
        }

        private static int PlaceSeats(LayoutContext context)
        {
            EnclaveDevelopmentVisualProfile profile =
                context.VisualProfile;
            List<IntVec3> candidates =
                EnclaveLayoutPlacementUtility.GetRingCells(
                    context.Anchors.Gathering,
                    Math.Max(2, profile.InternalSpacing)
                );
            ThingDef seatingDef = profile.SeatingDef;
            ThingDef stuffDef =
                EnclaveDevelopmentVisualUtility.GetFurnitureStuff(
                    profile,
                    seatingDef
                );
            int placed = 0;

            for (
                int index = 0;
                index < profile.GatheringSeatCount;
                index++
            )
            {
                IntVec3 preferred = candidates[
                    index % candidates.Count
                ];
                Thing seat;

                if (
                    EnclaveLayoutPlacementUtility.TryPlaceBuilding(
                        context,
                        context.Zones.Gathering,
                        seatingDef,
                        stuffDef,
                        GetFacingRotation(
                            preferred,
                            context.Anchors.Gathering
                        ),
                        new[] { preferred },
                        out seat
                    )
                )
                {
                    placed++;
                }
            }

            return placed;
        }

        private static int PlaceLights(LayoutContext context)
        {
            int target = context.VisualProfile.GatheringLightCount;
            List<IntVec3> candidates =
                EnclaveLayoutPlacementUtility.GetCornerCells(
                    context.Zones.Gathering
                );
            int placed = 0;

            for (int index = 0; index < target; index++)
            {
                Thing light;

                if (
                    EnclaveLayoutPlacementUtility.TryPlaceBuilding(
                        context,
                        context.Zones.Gathering,
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

        private static Rot4 GetFacingRotation(
            IntVec3 cell,
            IntVec3 center
        )
        {
            int xDifference = cell.x - center.x;
            int zDifference = cell.z - center.z;

            if (Math.Abs(xDifference) > Math.Abs(zDifference))
            {
                return xDifference < 0 ? Rot4.East : Rot4.West;
            }

            return zDifference < 0 ? Rot4.North : Rot4.South;
        }
    }
}
