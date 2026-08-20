using System.Collections.Generic;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class StorageAreaPiece
    {
        public static void Generate(LayoutContext context)
        {
            List<EnclaveVisualStorageStack> stacks =
                EnclaveDevelopmentVisualUtility.CreateStorageStacks(
                    context.VisualProfile,
                    context.Random
                );
            List<IntVec3> candidateCells =
                EnclaveLayoutPlacementUtility.GetCenteredGridCells(
                    context.Anchors.Storage,
                    16,
                    4,
                    2
                );
            int placed = 0;

            for (int index = 0; index < stacks.Count; index++)
            {
                EnclaveVisualStorageStack stack = stacks[index];
                Thing resource;

                if (
                    EnclaveLayoutPlacementUtility.TryPlaceItem(
                        context,
                        context.Zones.Storage,
                        stack.ThingDef,
                        stack.StackCount,
                        new[]
                        {
                            candidateCells[
                                index % candidateCells.Count
                            ]
                        },
                        out resource
                    )
                )
                {
                    placed++;
                }
            }

            Log.Message(
                "[IEE] Storage area: placed " +
                placed +
                "/" +
                stacks.Count +
                " modest visible resource stacks for " +
                EnclaveDevelopmentUtility.GetDisplayName(
                    context.VisualProfile.Tier
                ) +
                ". These are map resources, not trader stock."
            );
        }
    }
}
