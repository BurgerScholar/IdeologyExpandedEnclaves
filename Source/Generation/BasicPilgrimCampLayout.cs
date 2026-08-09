using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class BasicPilgrimCampLayout
    {
        public static void Generate(
            Map map,
            IntVec3 campCenter,
            int population,
            EnclaveData enclave
        )
        {
            LayoutContext context =
                new LayoutContext(
                    map,
                    campCenter,
                    population,
                    enclave
                );

            GatheringAreaPiece.Generate(context);

            SleepingAreaPiece.Generate(context);

            StorageAreaPiece.Generate(context);

            Log.Message("[IEE] Layout generated.");
        }
    }
}
