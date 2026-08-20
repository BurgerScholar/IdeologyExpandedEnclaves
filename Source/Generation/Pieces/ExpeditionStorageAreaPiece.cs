using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class ExpeditionStorageAreaPiece
    {
        public static void Generate(
            LayoutContext context,
            EnclaveExpeditionPurpose purpose
        )
        {
            List<EnclaveVisualStorageStack> stacks =
                CreateStacks(context.Enclave, purpose);
            int target = Math.Min(
                context.VisualProfile.StorageStackCount,
                stacks.Count
            );
            List<IntVec3> cells =
                EnclaveLayoutPlacementUtility.GetCenteredGridCells(
                    context.Anchors.Storage,
                    12,
                    3,
                    2
                );
            int placed = 0;

            for (int index = 0; index < target; index++)
            {
                EnclaveVisualStorageStack stack = stacks[index];
                Thing ignored;

                if (
                    EnclaveLayoutPlacementUtility.TryPlaceItem(
                        context,
                        context.Zones.Storage,
                        stack.ThingDef,
                        stack.StackCount,
                        new[] { cells[index % cells.Count] },
                        out ignored
                    )
                )
                {
                    placed++;
                }
            }

            Log.Message(
                "[IEE] Expedition storage placed " +
                placed +
                "/" +
                target +
                " conservative visible stacks."
            );
        }

        private static List<EnclaveVisualStorageStack> CreateStacks(
            EnclaveData data,
            EnclaveExpeditionPurpose purpose
        )
        {
            int tier = Math.Max(
                1,
                EnclaveDevelopmentUtility.GetNumericTier(data)
            );
            List<EnclaveVisualStorageStack> stacks =
                new List<EnclaveVisualStorageStack>();

            switch (purpose)
            {
                case EnclaveExpeditionPurpose.Trade:
                    Add(stacks, ThingDefOf.Steel, 12 + tier * 4);
                    Add(stacks, ThingDefOf.Cloth, 8 + tier * 4);
                    Add(
                        stacks,
                        ThingDefOf.ComponentIndustrial,
                        Math.Min(3, tier)
                    );
                    Add(
                        stacks,
                        ThingDefOf.MedicineIndustrial,
                        Math.Max(1, tier / 2)
                    );
                    break;
                case EnclaveExpeditionPurpose.Patrol:
                    Add(stacks, ThingDefOf.Steel, 12 + tier * 4);
                    Add(stacks, ThingDefOf.MedicineHerbal, 2 + tier);
                    Add(stacks, ThingDefOf.MealSurvivalPack, 3 + tier);
                    break;
                default:
                    Add(stacks, ThingDefOf.MealSurvivalPack, 4 + tier * 2);
                    Add(stacks, ThingDefOf.MedicineHerbal, 1 + tier);
                    Add(stacks, ThingDefOf.Cloth, 8 + tier * 4);
                    break;
            }

            return stacks;
        }

        private static void Add(
            List<EnclaveVisualStorageStack> stacks,
            ThingDef thingDef,
            int count
        )
        {
            stacks.Add(
                new EnclaveVisualStorageStack(
                    thingDef,
                    Math.Min(count, thingDef.stackLimit)
                )
            );
        }
    }
}
