using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveEncounterMapUtility
    {
        public static Map EnsureMapGenerated(PilgrimCamp camp)
        {
            if (camp == null)
            {
                Log.Error(
                    "[IEE] Cannot generate an enclave map without a camp."
                );

                return null;
            }

            Map map = camp.Map;

            if (map != null)
            {
                return map;
            }

            map = MapGenerator.GenerateMap(
                new IntVec3(120, 1, 120),
                camp,
                camp.MapGeneratorDef,
                null,
                null,
                false,
                false
            );

            EnclaveMapPopulator.PopulateNewCamp(map, camp);

            return map;
        }
    }
}
