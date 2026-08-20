using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveExpeditionEncounterMapUtility
    {
        public static Map EnsureMapGenerated(
            EnclaveExpeditionSite site
        )
        {
            if (site == null || site.Destroyed)
            {
                Log.Error(
                    "[IEE] Cannot generate an expedition map without " +
                    "an active expedition site."
                );
                return null;
            }

            Map map = site.Map;

            if (map != null)
            {
                return map;
            }

            map = MapGenerator.GenerateMap(
                new IntVec3(90, 1, 90),
                site,
                site.MapGeneratorDef,
                null,
                null,
                false,
                false
            );

            EnclaveExpeditionMapPopulator.PopulateNewSite(map, site);
            return map;
        }
    }
}
