using HarmonyLib;
using Verse;

namespace IdeologyExpandedEnclaves
{
    internal sealed class EnclaveHarmPatchState
    {
        public Pawn Pawn;
        public PilgrimCamp Camp;
        public bool WasDowned;
        public bool PlayerAttributed;
    }

    [HarmonyPatch(
        typeof(Pawn_HealthTracker),
        "MakeDowned",
        new System.Type[]
        {
            typeof(DamageInfo?),
            typeof(Hediff)
        }
    )]
    internal static class Patch_PawnHealthTracker_MakeDowned
    {
        private static void Prefix(
            Pawn ___pawn,
            DamageInfo? dinfo,
            ref EnclaveHarmPatchState __state
        )
        {
            PilgrimCamp camp;

            if (
                ___pawn == null ||
                ___pawn.Downed ||
                !EnclaveHarmService.TryResolveEnclavePawn(
                    ___pawn,
                    out camp
                )
            )
            {
                return;
            }

            __state = new EnclaveHarmPatchState
            {
                Pawn = ___pawn,
                Camp = camp,
                WasDowned = false,
                PlayerAttributed =
                    EnclaveHarmService.IsPlayerAttributed(dinfo)
            };
        }

        private static void Postfix(EnclaveHarmPatchState __state)
        {
            if (
                __state?.Pawn == null ||
                __state.WasDowned ||
                !__state.Pawn.Downed ||
                __state.Pawn.Dead
            )
            {
                return;
            }

            EnclaveHarmService.HandleDowned(
                __state.Camp,
                __state.Pawn,
                __state.PlayerAttributed
            );
        }
    }

    [HarmonyPatch(
        typeof(Pawn),
        nameof(Pawn.Kill),
        new System.Type[]
        {
            typeof(DamageInfo?),
            typeof(Hediff)
        }
    )]
    internal static class Patch_Pawn_Kill
    {
        private static void Prefix(
            Pawn __instance,
            DamageInfo? dinfo,
            ref EnclaveHarmPatchState __state
        )
        {
            PilgrimCamp camp;

            if (
                __instance == null ||
                __instance.Dead ||
                !EnclaveHarmService.IsPlayerAttributed(dinfo) ||
                !EnclaveHarmService.TryResolveEnclavePawn(
                    __instance,
                    out camp
                )
            )
            {
                return;
            }

            __state = new EnclaveHarmPatchState
            {
                Pawn = __instance,
                Camp = camp,
                WasDowned = __instance.Downed,
                PlayerAttributed = true
            };
        }

        private static void Postfix(EnclaveHarmPatchState __state)
        {
            if (__state?.Pawn == null || !__state.Pawn.Dead)
            {
                return;
            }

            EnclaveHarmService.HandleKilled(
                __state.Camp,
                __state.Pawn,
                __state.WasDowned
            );
        }
    }
}
