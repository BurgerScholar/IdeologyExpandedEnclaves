using Verse;

namespace IdeologyExpandedEnclaves
{
    public enum InterEnclaveRelationshipState
    {
        Hostile,
        Rival,
        Neutral,
        Friendly,
        Allied
    }

    public sealed class InterEnclaveRelationshipRecord : IExposable
    {
        public int FirstEnclaveId = -1;
        public int SecondEnclaveId = -1;
        public int Score;

        public void ExposeData()
        {
            Scribe_Values.Look(
                ref FirstEnclaveId,
                "firstEnclaveId",
                -1
            );
            Scribe_Values.Look(
                ref SecondEnclaveId,
                "secondEnclaveId",
                -1
            );
            Scribe_Values.Look(ref Score, "score", 0);
        }

        public void Normalize()
        {
            if (FirstEnclaveId > SecondEnclaveId)
            {
                int temporary = FirstEnclaveId;
                FirstEnclaveId = SecondEnclaveId;
                SecondEnclaveId = temporary;
            }

            Score = InterEnclaveRelationshipUtility.ClampScore(Score);
        }
    }
}
