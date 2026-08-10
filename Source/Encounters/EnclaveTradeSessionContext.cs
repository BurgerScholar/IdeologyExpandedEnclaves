using RimWorld;
using Verse;

namespace IdeologyExpandedEnclaves
{
    internal static class EnclaveTradeSessionContext
    {
        private static PilgrimCamp activeCamp;
        private static Pawn activeTraderPawn;
        private static ITrader activeSessionTrader;

        public static void Begin(
            PilgrimCamp camp,
            Pawn traderPawn,
            ITrader sessionTrader
        )
        {
            activeCamp = camp;
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

            activeCamp = null;
            activeTraderPawn = null;
            activeSessionTrader = null;
        }

        public static bool AllowsInventoryThing(Thing thing)
        {
            if (
                thing == null ||
                activeCamp == null ||
                activeTraderPawn == null ||
                activeSessionTrader == null ||
                !TradeSession.Active ||
                !ReferenceEquals(TradeSession.trader, activeSessionTrader) ||
                activeTraderPawn.Map?.Parent != activeCamp ||
                activeCamp.PawnRoles?.GetPawn(EnclavePawnRole.Trader) !=
                    activeTraderPawn ||
                activeCamp.VisitingGroup == null ||
                !activeCamp.VisitingGroup.ContainsInventoryThing(
                    activeCamp,
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
