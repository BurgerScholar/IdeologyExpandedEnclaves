using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class RitualAreaPiece
    {
        public static void Generate(LayoutContext context)
        {
            Thing focalObject;

            if (
                !EnclaveLayoutPlacementUtility.TryPlaceBuilding(
                    context,
                    context.Zones.Ritual,
                    ThingDefOf.TorchLamp,
                    null,
                    Rot4.North,
                    new[] { context.Anchors.Ritual },
                    out focalObject
                )
            )
            {
                Log.Warning(
                    "[IEE] Could not place a ritual focal object; " +
                    "the remaining ritual embellishments were skipped."
                );
                return;
            }

            int seatsPlaced = PlaceSeating(context);
            int extraLightsPlaced = PlaceExtraLights(context);

            Log.Message(
                "[IEE] Ritual area: focal torch, " +
                seatsPlaced +
                "/" +
                context.VisualProfile.RitualSeatCount +
                " seats, and " +
                extraLightsPlaced +
                "/" +
                Math.Max(
                    0,
                    context.VisualProfile.RitualLightCount - 1
                ) +
                " extra lights."
            );
        }

        private static int PlaceSeating(LayoutContext context)
        {
            EnclaveDevelopmentVisualProfile profile =
                context.VisualProfile;
            List<IntVec3> candidates =
                EnclaveLayoutPlacementUtility.GetRingCells(
                    context.Anchors.Ritual,
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
                index < profile.RitualSeatCount;
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
                        context.Zones.Ritual,
                        seatingDef,
                        stuffDef,
                        Rot4.North,
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

        private static int PlaceExtraLights(LayoutContext context)
        {
            int target = Math.Max(
                0,
                context.VisualProfile.RitualLightCount - 1
            );
            List<IntVec3> candidates =
                EnclaveLayoutPlacementUtility.GetCornerCells(
                    context.Zones.Ritual
                );
            List<IntVec3> ringCandidates =
                EnclaveLayoutPlacementUtility.GetRingCells(
                    context.Anchors.Ritual,
                    Math.Max(3, context.VisualProfile.InternalSpacing + 1)
                );

            candidates.AddRange(ringCandidates);
            int placed = 0;

            for (int index = 0; index < target; index++)
            {
                Thing light;

                if (
                    EnclaveLayoutPlacementUtility.TryPlaceBuilding(
                        context,
                        context.Zones.Ritual,
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
