using System;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public class LayoutContext
    {
        public Map Map;
        public IntVec3 CampCenter;
        public int Population;
        public EnclaveData Enclave;
        public Random Random;
        public LayoutAnchors Anchors;
        public LayoutZones Zones;

        public LayoutContext(
            Map map,
            IntVec3 campCenter,
            int population,
            EnclaveData enclave
        )
        {
            Map = map;
            CampCenter = campCenter;
            Population = population;
            Enclave = enclave;
            Random = new Random();

            bool generatedLayout =
                Enclave.EnsureLayoutAssignments(Random);

            Log.Message(
                generatedLayout
                    ? "[IEE] Generated and retained missing or invalid layout: " +
                      Enclave.DescribeLayoutAssignments() + "."
                    : "[IEE] Reused persistent layout: " +
                      Enclave.DescribeLayoutAssignments() + "."
            );

            Anchors = new LayoutAnchors(campCenter, Enclave);
            Zones = new LayoutZones(Anchors);
        }
    }
}
