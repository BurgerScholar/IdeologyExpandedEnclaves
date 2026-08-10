using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeologyExpandedEnclaves
{
    [HarmonyPatch(typeof(TradeDeal), "InSellablePosition")]
    internal static class Patch_TradeDeal_InSellablePosition
    {
        private static void Postfix(
            Thing t,
            ref string reason,
            ref bool __result
        )
        {
            if (
                !__result &&
                EnclaveTradeSessionContext.AllowsInventoryThing(t)
            )
            {
                reason = null;
                __result = true;
            }
        }
    }
}
