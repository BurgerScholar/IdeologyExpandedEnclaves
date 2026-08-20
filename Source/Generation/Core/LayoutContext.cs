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
        public EnclaveDevelopmentVisualProfile VisualProfile;
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

            Random layoutRandom = new Random(
                CreateStableSeed(enclave, includeLayout: false)
            );

            bool generatedLayout =
                Enclave.EnsureLayoutAssignments(layoutRandom);

            Log.Message(
                generatedLayout
                    ? "[IEE] Generated and retained missing or invalid layout: " +
                      Enclave.DescribeLayoutAssignments() + "."
                    : "[IEE] Reused persistent layout: " +
                      Enclave.DescribeLayoutAssignments() + "."
            );

            Random = new Random(
                CreateStableSeed(enclave, includeLayout: true)
            );
            VisualProfile =
                EnclaveDevelopmentVisualUtility.GetProfile(enclave);
            Anchors = new LayoutAnchors(campCenter, Enclave);
            Zones = new LayoutZones(Anchors, VisualProfile);

            Log.Message(
                "[IEE] Visual generation profile for " +
                (Enclave.Name ?? "an enclave") +
                ": " +
                EnclaveDevelopmentUtility.GetDisplayName(
                    VisualProfile.Tier
                ) +
                ", " +
                VisualProfile.IdeologyType +
                ", " +
                EnclaveArchetypeUtility.GetDisplayName(Enclave) +
                ", " +
                VisualProfile.DiagnosticSummary +
                "."
            );
        }

        private static int CreateStableSeed(
            EnclaveData enclave,
            bool includeLayout
        )
        {
            unchecked
            {
                int seed = 17;

                AddStringToSeed(ref seed, enclave?.Name);
                AddStringToSeed(ref seed, enclave?.Leader);
                seed = seed * 31 + (enclave?.Population ?? 0);
                seed = seed * 31 +
                    (int)EnclaveDevelopmentUtility.GetTier(enclave);
                seed = seed * 31 +
                    (int)EnclaveIdeologyUtility.GetIdeologyType(enclave);
                seed = seed * 31 +
                    (int)EnclaveArchetypeUtility.GetArchetype(enclave);

                if (includeLayout && enclave != null)
                {
                    seed = seed * 31 +
                        (int)enclave.GatheringPosition;
                    seed = seed * 31 +
                        (int)enclave.SleepingPosition;
                    seed = seed * 31 +
                        (int)enclave.StoragePosition;
                    seed = seed * 31 +
                        (int)enclave.RitualPosition;
                }

                return seed;
            }
        }

        private static void AddStringToSeed(
            ref int seed,
            string value
        )
        {
            if (value.NullOrEmpty())
            {
                seed *= 31;
                return;
            }

            unchecked
            {
                for (int index = 0; index < value.Length; index++)
                {
                    seed = seed * 31 + value[index];
                }
            }
        }
    }
}
