using System.Collections.Generic;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public class EnclaveHarmPenaltyTracker : IExposable
    {
        private Dictionary<string, int> currentIncidentPenalties =
            new Dictionary<string, int>();
        private List<string> pawnIdsWorkingList;
        private List<int> penaltiesWorkingList;

        public bool HasRecordedIncident(Pawn pawn)
        {
            string pawnId = GetPawnId(pawn);

            return
                !pawnId.NullOrEmpty() &&
                currentIncidentPenalties != null &&
                currentIncidentPenalties.ContainsKey(pawnId);
        }

        public int RecordDowning(
            Pawn pawn,
            int appliedPenalty
        )
        {
            string pawnId = GetPawnId(pawn);

            if (pawnId.NullOrEmpty())
            {
                return 0;
            }

            EnsureCollection();

            if (appliedPenalty <= 0)
            {
                currentIncidentPenalties.Remove(pawnId);
                return 0;
            }

            currentIncidentPenalties[pawnId] = appliedPenalty;
            return appliedPenalty;
        }

        public int RecordDeath(
            Pawn pawn,
            int totalPenalty,
            bool wasDowned
        )
        {
            string pawnId = GetPawnId(pawn);

            if (pawnId.NullOrEmpty() || totalPenalty <= 0)
            {
                return 0;
            }

            EnsureCollection();

            int alreadyApplied = 0;

            if (wasDowned)
            {
                currentIncidentPenalties.TryGetValue(
                    pawnId,
                    out alreadyApplied
                );
            }

            alreadyApplied = System.Math.Max(
                0,
                System.Math.Min(alreadyApplied, totalPenalty)
            );
            currentIncidentPenalties[pawnId] = totalPenalty;

            return totalPenalty - alreadyApplied;
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(
                ref currentIncidentPenalties,
                "currentIncidentPenalties",
                LookMode.Value,
                LookMode.Value,
                ref pawnIdsWorkingList,
                ref penaltiesWorkingList
            );

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                EnsureCollection();

                Dictionary<string, int> validPenalties =
                    new Dictionary<string, int>();

                foreach (
                    KeyValuePair<string, int> entry in
                    currentIncidentPenalties
                )
                {
                    if (!entry.Key.NullOrEmpty() && entry.Value > 0)
                    {
                        validPenalties[entry.Key] = entry.Value;
                    }
                }

                currentIncidentPenalties = validPenalties;
                pawnIdsWorkingList = null;
                penaltiesWorkingList = null;
            }
        }

        private void EnsureCollection()
        {
            if (currentIncidentPenalties == null)
            {
                currentIncidentPenalties =
                    new Dictionary<string, int>();
            }
        }

        private static string GetPawnId(Pawn pawn)
        {
            return pawn?.GetUniqueLoadID();
        }
    }
}
