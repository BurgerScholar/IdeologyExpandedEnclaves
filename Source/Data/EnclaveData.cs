using Verse;

namespace IdeologyExpandedEnclaves
{
    public class EnclaveData : IExposable
    {
        public string Name;
        public string Leader;
        public string Ideology;
        public int Population;
        public bool Friendly;

        public void ExposeData()
        {
            Scribe_Values.Look(ref Name, "name", "Unnamed Enclave");
            Scribe_Values.Look(ref Leader, "leader", "Unknown");
            Scribe_Values.Look(ref Ideology, "ideology", "Unknown");
            Scribe_Values.Look(ref Population, "population", 0);
            Scribe_Values.Look(ref Friendly, "friendly", true);
        }
    }
}