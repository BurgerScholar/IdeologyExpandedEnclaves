using System.Collections.Generic;
using RimWorld;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveTraderStockService
    {
        private sealed class StockEntry
        {
            public readonly string ThingDefName;
            public readonly int Count;

            public StockEntry(string thingDefName, int count)
            {
                ThingDefName = thingDefName;
                Count = count;
            }
        }

        private static readonly StockEntry[] FriendlyStock =
        {
            new StockEntry("MedicineHerbal", 5),
            new StockEntry("MealSurvivalPack", 5),
            new StockEntry("Leather_Plain", 25),
            new StockEntry("ComponentIndustrial", 1)
        };

        private static readonly StockEntry[] TrustedStock =
        {
            new StockEntry("MedicineIndustrial", 4),
            new StockEntry("ComponentIndustrial", 4),
            new StockEntry("Plasteel", 15),
            new StockEntry("DevilstrandCloth", 20)
        };

        private static readonly StockEntry[] ReveredStock =
        {
            new StockEntry("ComponentSpacer", 2),
            new StockEntry("MedicineUltratech", 2),
            new StockEntry("Gold", 20),
            new StockEntry("Uranium", 15)
        };

        public static bool EnsureStockForCurrentTier(
            PilgrimCamp camp,
            Pawn trader
        )
        {
            if (camp?.Data == null)
            {
                Log.Error(
                    "[IEE] Cannot grant reputation trader stock " +
                    "without enclave data."
                );
                return false;
            }

            if (
                trader == null ||
                camp.PawnRoles?.GetPawn(EnclavePawnRole.Trader) !=
                    trader
            )
            {
                Log.Error(
                    "[IEE] Cannot grant reputation trader stock " +
                    "without the enclave's designated Trader."
                );
                return false;
            }

            if (trader.inventory?.innerContainer == null)
            {
                Log.Error(
                    "[IEE] Cannot grant reputation trader stock " +
                    "because " +
                    trader.LabelShort +
                    " has no inventory container."
                );
                return false;
            }

            EnclaveTraderStockGrantTier targetTier =
                GetGrantTier(camp.Data.ReputationTier);
            EnclaveTraderStockGrantTier grantedTier =
                camp.Data.HighestTraderStockTierGranted;

            if (grantedTier >= targetTier)
            {
                return true;
            }

            for (
                int tierValue = (int)grantedTier + 1;
                tierValue <= (int)targetTier;
                tierValue++
            )
            {
                EnclaveTraderStockGrantTier tier =
                    (EnclaveTraderStockGrantTier)tierValue;

                if (!TryGrantTierStock(camp, trader, tier))
                {
                    return false;
                }

                camp.Data.HighestTraderStockTierGranted = tier;
            }

            return true;
        }

        private static EnclaveTraderStockGrantTier GetGrantTier(
            EnclaveReputationTier reputationTier
        )
        {
            switch (reputationTier)
            {
                case EnclaveReputationTier.Friendly:
                    return EnclaveTraderStockGrantTier.Friendly;
                case EnclaveReputationTier.Trusted:
                    return EnclaveTraderStockGrantTier.Trusted;
                case EnclaveReputationTier.Revered:
                    return EnclaveTraderStockGrantTier.Revered;
                default:
                    return EnclaveTraderStockGrantTier.None;
            }
        }

        private static bool TryGrantTierStock(
            PilgrimCamp camp,
            Pawn trader,
            EnclaveTraderStockGrantTier tier
        )
        {
            StockEntry[] entries = GetStockEntries(tier);
            List<ThingDef> thingDefs = new List<ThingDef>(entries.Length);

            foreach (StockEntry entry in entries)
            {
                ThingDef thingDef =
                    DefDatabase<ThingDef>.GetNamedSilentFail(
                        entry.ThingDefName
                    );

                if (thingDef == null)
                {
                    Log.Error(
                        "[IEE] Cannot grant " +
                        tier +
                        " trader stock because ThingDef " +
                        entry.ThingDefName +
                        " is missing."
                    );
                    return false;
                }

                thingDefs.Add(thingDef);
            }

            int addedStacks = 0;
            int addedItems = 0;

            for (int index = 0; index < entries.Length; index++)
            {
                StockEntry entry = entries[index];
                Thing thing = ThingMaker.MakeThing(thingDefs[index]);
                thing.stackCount = entry.Count;

                if (trader.inventory.innerContainer.TryAdd(thing))
                {
                    addedStacks++;
                    addedItems += entry.Count;
                    continue;
                }

                Log.Error(
                    "[IEE] Failed to add " +
                    entry.Count +
                    " " +
                    thingDefs[index].label +
                    " to " +
                    trader.LabelShort +
                    " while granting " +
                    tier +
                    " trader stock. The tier will still be recorded " +
                    "to prevent duplicate grants."
                );

                if (!thing.Destroyed)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }

            Log.Message(
                "[IEE] Granted " +
                tier +
                " trader stock to " +
                trader.LabelShort +
                " for " +
                (camp.Data.Name ?? "an enclave") +
                ": " +
                addedStacks +
                " stacks, " +
                addedItems +
                " total items."
            );

            return true;
        }

        private static StockEntry[] GetStockEntries(
            EnclaveTraderStockGrantTier tier
        )
        {
            switch (tier)
            {
                case EnclaveTraderStockGrantTier.Friendly:
                    return FriendlyStock;
                case EnclaveTraderStockGrantTier.Trusted:
                    return TrustedStock;
                case EnclaveTraderStockGrantTier.Revered:
                    return ReveredStock;
                default:
                    return new StockEntry[0];
            }
        }
    }
}
