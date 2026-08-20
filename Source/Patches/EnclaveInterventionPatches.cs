using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeologyExpandedEnclaves
{
    [HarmonyPatch(
        typeof(IncidentWorker_Raid),
        "TryGenerateRaidInfo"
    )]
    internal static class Patch_IncidentWorkerRaid_TryGenerateRaidInfo
    {
        private static void Postfix(
            IncidentParms parms,
            ref List<Pawn> pawns,
            bool debugTest,
            bool __result
        )
        {
            if (!__result || debugTest || pawns.NullOrEmpty())
            {
                return;
            }

            EnclaveInterventionService.NotifyRaidGenerated(
                parms?.target as Map,
                parms?.faction,
                pawns
            );
        }
    }

    [HarmonyPatch(
        typeof(GenHostility),
        nameof(GenHostility.HostileTo),
        new System.Type[]
        {
            typeof(Thing),
            typeof(Thing)
        }
    )]
    internal static class Patch_GenHostility_HostileToThings
    {
        private static void Postfix(
            Thing __0,
            Thing __1,
            ref bool __result
        )
        {
            bool localHostility;

            if (
                EnclaveInterventionService
                    .TryGetLocalInterventionHostility(
                        __0,
                        __1,
                        out localHostility
                    )
            )
            {
                __result = localHostility;
            }
        }
    }

    [HarmonyPatch(
        typeof(GenHostility),
        nameof(GenHostility.HostileTo),
        new System.Type[]
        {
            typeof(Thing),
            typeof(Faction)
        }
    )]
    internal static class Patch_GenHostility_HostileToFaction
    {
        private static void Postfix(
            Thing __0,
            Faction __1,
            ref bool __result
        )
        {
            bool localHostility;

            if (
                EnclaveInterventionService
                    .TryGetLocalInterventionHostility(
                        __0,
                        __1,
                        out localHostility
                    )
            )
            {
                __result = localHostility;
            }
        }
    }
}
