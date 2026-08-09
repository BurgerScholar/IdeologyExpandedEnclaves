using System.Collections.Generic;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public enum EnclavePawnRole
    {
        Leader,
        Trader,
        Recruiter
    }

    public class EnclavePawnRoleAssignments : IExposable
    {
        private Dictionary<EnclavePawnRole, Pawn> assignments =
            new Dictionary<EnclavePawnRole, Pawn>();
        private List<EnclavePawnRole> assignmentKeysWorkingList;
        private List<Pawn> assignmentValuesWorkingList;

        public void Assign(
            EnclavePawnRole role,
            Pawn pawn
        )
        {
            if (assignments == null)
            {
                assignments =
                    new Dictionary<EnclavePawnRole, Pawn>();
            }

            assignments[role] = pawn;
        }

        public Pawn GetPawn(EnclavePawnRole role)
        {
            Pawn pawn;

            return assignments != null &&
                assignments.TryGetValue(role, out pawn)
                ? pawn
                : null;
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(
                ref assignments,
                "assignments",
                LookMode.Value,
                LookMode.Reference,
                ref assignmentKeysWorkingList,
                ref assignmentValuesWorkingList
            );

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (assignments == null)
                {
                    assignments =
                        new Dictionary<EnclavePawnRole, Pawn>();
                }

                assignmentKeysWorkingList = null;
                assignmentValuesWorkingList = null;
            }
        }
    }
}
