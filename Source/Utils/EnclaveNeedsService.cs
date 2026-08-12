using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveNeedsService
    {
        public const int PulseIntervalTicks = 900000;

        public static int EvaluateAllEnclaves()
        {
            int evaluatedCount = 0;

            foreach (PilgrimCamp camp in GetInitializedCamps())
            {
                EvaluateCampAndGenerateRequest(camp);
                evaluatedCount++;
            }

            if (Prefs.DevMode)
            {
                Log.Message(
                    "[IEE] Evaluated persistent needs for " +
                    evaluatedCount +
                    " initialized enclave(s)."
                );
            }

            return evaluatedCount;
        }

        public static bool EvaluateCampAndGenerateRequest(
            PilgrimCamp camp
        )
        {
            if (camp?.Data == null)
            {
                return false;
            }

            bool changed = EnclaveNeedsUtility.EvaluateNeeds(camp);
            EnclaveQuestRequest generatedRequest;

            EnclaveQuestService.TryGenerateSupplyRequest(
                camp,
                out generatedRequest
            );

            return changed || generatedRequest != null;
        }

        private static List<PilgrimCamp> GetInitializedCamps()
        {
            List<PilgrimCamp> camps = new List<PilgrimCamp>();

            if (Find.WorldObjects?.AllWorldObjects == null)
            {
                return camps;
            }

            foreach (
                WorldObject worldObject in
                Find.WorldObjects.AllWorldObjects
            )
            {
                PilgrimCamp camp = worldObject as PilgrimCamp;

                if (
                    camp?.Data == null ||
                    camp.Destroyed ||
                    !camp.Spawned ||
                    !camp.Tile.Valid
                )
                {
                    continue;
                }

                camps.Add(camp);
            }

            camps.Sort((first, second) => first.ID.CompareTo(second.ID));
            return camps;
        }
    }
}
