using System;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveIdeologyUtility
    {
        private sealed class WeightedIdeologyType
        {
            public readonly EnclaveIdeologyType Type;
            public readonly int Weight;

            public WeightedIdeologyType(
                EnclaveIdeologyType type,
                int weight
            )
            {
                Type = type;
                Weight = weight;
            }
        }

        private static readonly WeightedIdeologyType[] weightedTypes =
        {
            new WeightedIdeologyType(
                EnclaveIdeologyType.Communal,
                18
            ),
            new WeightedIdeologyType(
                EnclaveIdeologyType.Isolationist,
                12
            ),
            new WeightedIdeologyType(
                EnclaveIdeologyType.Martial,
                12
            ),
            new WeightedIdeologyType(
                EnclaveIdeologyType.Mercantile,
                15
            ),
            new WeightedIdeologyType(
                EnclaveIdeologyType.Nature,
                15
            ),
            new WeightedIdeologyType(
                EnclaveIdeologyType.Spiritual,
                18
            ),
            new WeightedIdeologyType(
                EnclaveIdeologyType.Transhumanist,
                10
            )
        };

        public static EnclaveIdeologyType GetIdeologyType(
            EnclaveData data
        )
        {
            return data?.IdeologyProfile?.IsValid == true
                ? data.IdeologyProfile.Type
                : EnclaveIdeologyType.Unassigned;
        }

        public static bool HasType(
            EnclaveData data,
            EnclaveIdeologyType type
        )
        {
            return
                type != EnclaveIdeologyType.Unassigned &&
                GetIdeologyType(data) == type;
        }

        public static string GetTypeLabel(EnclaveData data)
        {
            EnclaveIdeologyType type = GetIdeologyType(data);

            return type == EnclaveIdeologyType.Unassigned
                ? "Unknown"
                : type.ToString();
        }

        public static bool EnsureProfile(
            EnclaveData data,
            Random random = null,
            string reason = null
        )
        {
            if (data == null)
            {
                return false;
            }

            if (data.IdeologyProfile?.IsValid == true)
            {
                return false;
            }

            Random generationRandom =
                random ?? new Random(CreateStableSeed(data));

            data.IdeologyProfile = new EnclaveIdeologyProfile
            {
                Type = SelectType(generationRandom)
            };

            Log.Message(
                "[IEE] Generated persistent ideology profile for " +
                (data.Name ?? "an enclave") +
                ": " +
                data.IdeologyProfile.Type +
                (reason.NullOrEmpty()
                    ? "."
                    : " (" + reason + ").")
            );

            return true;
        }

        private static EnclaveIdeologyType SelectType(Random random)
        {
            int totalWeight = 0;

            foreach (WeightedIdeologyType weightedType in weightedTypes)
            {
                totalWeight += weightedType.Weight;
            }

            int selection = random.Next(totalWeight);

            foreach (WeightedIdeologyType weightedType in weightedTypes)
            {
                if (selection < weightedType.Weight)
                {
                    return weightedType.Type;
                }

                selection -= weightedType.Weight;
            }

            return EnclaveIdeologyType.Communal;
        }

        private static int CreateStableSeed(EnclaveData data)
        {
            unchecked
            {
                uint hash = 2166136261;

                AddStringToHash(ref hash, data.Name);
                AddStringToHash(ref hash, data.Leader);
                AddIntToHash(ref hash, data.Population);
                AddIntToHash(
                    ref hash,
                    (int)data.GatheringPosition
                );
                AddIntToHash(
                    ref hash,
                    (int)data.SleepingPosition
                );
                AddIntToHash(
                    ref hash,
                    (int)data.StoragePosition
                );
                AddIntToHash(
                    ref hash,
                    (int)data.RitualPosition
                );

                return (int)hash;
            }
        }

        private static void AddStringToHash(
            ref uint hash,
            string value
        )
        {
            if (value.NullOrEmpty())
            {
                AddIntToHash(ref hash, 0);
                return;
            }

            foreach (char character in value)
            {
                hash ^= character;
                hash *= 16777619;
            }
        }

        private static void AddIntToHash(ref uint hash, int value)
        {
            unchecked
            {
                hash ^= (uint)value;
                hash *= 16777619;
            }
        }
    }
}
