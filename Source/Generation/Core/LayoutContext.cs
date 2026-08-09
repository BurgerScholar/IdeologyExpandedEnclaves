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
            Anchors = new LayoutAnchors(campCenter);
            Zones = new LayoutZones(Anchors);
        }
    }
}