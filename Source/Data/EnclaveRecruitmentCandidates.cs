using System.Collections.Generic;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public class EnclaveRecruitmentCandidates : IExposable
    {
        private List<Pawn> candidates = new List<Pawn>();

        public IReadOnlyList<Pawn> Candidates => candidates;

        public bool IsRecruitmentCandidate(Pawn pawn)
        {
            return pawn != null &&
                candidates != null &&
                candidates.Contains(pawn);
        }

        public void SetCandidates(IEnumerable<Pawn> pawns)
        {
            if (candidates == null)
            {
                candidates = new List<Pawn>();
            }
            else
            {
                candidates.Clear();
            }

            if (pawns == null)
            {
                return;
            }

            foreach (Pawn pawn in pawns)
            {
                if (pawn != null && !candidates.Contains(pawn))
                {
                    candidates.Add(pawn);
                }
            }
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(
                ref candidates,
                "candidates",
                LookMode.Reference
            );

            if (
                Scribe.mode == LoadSaveMode.PostLoadInit &&
                candidates == null
            )
            {
                candidates = new List<Pawn>();
            }
        }
    }
}
