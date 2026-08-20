using RimWorld;
using RimWorld.Planet;
using Verse;

namespace IdeologyExpandedEnclaves
{
    internal static class EnclaveTradeSessionContext
    {
        private static MapParent activeTradeHost;
        private static Pawn activeTraderPawn;
        private static ITrader activeSessionTrader;

        public static void Begin(
            MapParent tradeHost,
            Pawn traderPawn,
            ITrader sessionTrader
        )
        {
            activeTradeHost = tradeHost;
            activeTraderPawn = traderPawn;
            activeSessionTrader = sessionTrader;
        }

        public static void Clear(ITrader sessionTrader = null)
        {
            if (
                sessionTrader != null &&
                !ReferenceEquals(activeSessionTrader, sessionTrader)
            )
            {
                return;
            }

            activeTradeHost = null;
            activeTraderPawn = null;
            activeSessionTrader = null;
        }

        public static bool AllowsInventoryThing(Thing thing)
        {
            if (
                thing == null ||
                activeTradeHost == null ||
                activeTraderPawn == null ||
                activeSessionTrader == null ||
                !TradeSession.Active ||
                !ReferenceEquals(TradeSession.trader, activeSessionTrader) ||
                activeTraderPawn.Map?.Parent != activeTradeHost ||
                !EnclaveTradeService.IsDesignatedTrader(
                    activeTradeHost,
                    activeTraderPawn
                ) ||
                EnclaveTradeService.GetVisitingGroup(
                    activeTradeHost
                ) == null ||
                !EnclaveTradeService.GetVisitingGroup(
                    activeTradeHost
                ).ContainsInventoryThing(
                    activeTradeHost,
                    thing
                ) ||
                !TradeUtility.PlayerSellableNow(
                    thing,
                    activeSessionTrader
                )
            )
            {
                return false;
            }

            return true;
        }
    }
}
