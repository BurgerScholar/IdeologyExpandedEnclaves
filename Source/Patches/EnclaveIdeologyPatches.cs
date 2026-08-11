using HarmonyLib;
using RimWorld;

namespace IdeologyExpandedEnclaves
{
    [HarmonyPatch(typeof(IdeoManager), "CanRemoveIdeo")]
    internal static class Patch_IdeoManager_CanRemoveIdeo
    {
        private static void Postfix(Ideo ideo, ref bool __result)
        {
            if (
                __result &&
                EnclaveIdeologyUtility.IsPersistentCampIdeo(ideo)
            )
            {
                __result = false;
            }
        }
    }
}
