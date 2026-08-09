using System.Collections.Generic;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public enum EnclavePawnRole
    {
        Leader
    }

    public class EnclavePawnRoleAssignments : IExposable
    {
        private Dictionary<EnclavePawnRole, Pawn> assignments =
            new Dictionary<EnclavePawnRole, Pawn>();

        public void Assign(
            EnclavePawnRole role,
            Pawn pawn
        )
        {
            assignments[role] = pawn;
        }

        public Pawn GetPawn(EnclavePawnRole role)
        {
            Pawn pawn;

            return assignments.TryGetValue(role, out pawn)
                ? pawn
                : null;
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(
                ref assignments,
                "assignments",
                LookMode.Value,
                LookMode.Reference
            );

            if (Scribe.mode == LoadSaveMode.PostLoadInit &&
                assignments == null)
            {
                assignments =
                    new Dictionary<EnclavePawnRole, Pawn>();
            }
        }
    }
}
