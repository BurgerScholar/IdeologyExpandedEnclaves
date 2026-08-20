using RimWorld;
using Verse;

namespace IdeologyExpandedEnclaves
{
    internal static class EnclaveTemporaryTraderUtility
    {
        public static bool Initialize(
            Pawn trader,
            Map map,
            EnclaveExpeditionPurpose purpose
        )
        {
            if (
                trader?.mindState == null ||
                trader.inventory == null ||
                map == null
            )
            {
                return false;
            }

            TraderKindDef traderKind = GetTraderKind(purpose);

            if (traderKind?.stockGenerators.NullOrEmpty() != false)
            {
                return false;
            }

            trader.mindState.wantsToTradeWithColony = true;
            PawnComponentsUtility.AddAndRemoveDynamicComponents(trader);

            if (trader.trader == null)
            {
                return false;
            }

            trader.trader.traderKind = traderKind;

            foreach (StockGenerator generator in traderKind.stockGenerators)
            {
                foreach (
                    Thing thing in
                    generator.GenerateThings(map.Tile, trader.Faction)
                )
                {
                    if (!trader.inventory.innerContainer.TryAdd(thing))
                    {
                        thing.Destroy(DestroyMode.Vanish);
                    }
                }
            }

            EnclaveTradeService.SuppressVanillaTradeOption(trader);
            return true;
        }

        public static TraderKindDef GetTraderKind(
            EnclaveExpeditionPurpose purpose
        )
        {
            return DefDatabase<TraderKindDef>.GetNamedSilentFail(
                EnclaveExpeditionUtility.GetTraderKindDefName(purpose)
            );
        }
    }
}
