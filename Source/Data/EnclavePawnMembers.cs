using System.Collections.Generic;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public class EnclavePawnMembers : IExposable
    {
        private List<Pawn> members = new List<Pawn>();
        private bool initialized;

        public IReadOnlyList<Pawn> Members => members;
        public bool IsInitialized => initialized;

        public bool Contains(Pawn pawn)
        {
            return
                pawn != null &&
                members != null &&
                members.Contains(pawn);
        }

        public void SetMembers(IEnumerable<Pawn> pawns)
        {
            EnsureCollection();
            members.Clear();

            if (pawns != null)
            {
                foreach (Pawn pawn in pawns)
                {
                    if (pawn != null && !members.Contains(pawn))
                    {
                        members.Add(pawn);
                    }
                }
            }

            initialized = true;
        }

        public bool Add(Pawn pawn)
        {
            EnsureCollection();
            initialized = true;

            if (pawn == null || members.Contains(pawn))
            {
                return false;
            }

            members.Add(pawn);
            return true;
        }

        public bool Remove(Pawn pawn)
        {
            return
                pawn != null &&
                members != null &&
                members.Remove(pawn);
        }

        public void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                EnsureCollection();
                members.RemoveAll(
                    pawn => pawn == null || pawn.Destroyed
                );
            }

            Scribe_Collections.Look(
                ref members,
                "members",
                LookMode.Reference
            );
            Scribe_Values.Look(ref initialized, "initialized", false);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                EnsureCollection();
                members.RemoveAll(
                    pawn => pawn == null || pawn.Destroyed
                );
            }
        }

        private void EnsureCollection()
        {
            if (members == null)
            {
                members = new List<Pawn>();
            }
        }
    }
}
