using System;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveGenerator
    {
        private static readonly Random random = new Random();

        private static readonly string[] names =
        {
            "Circle of New Dawn",
            "Ashen Brotherhood",
            "Iron Communion",
            "Green Covenant",
            "Silent Horizon"
        };

        private static readonly string[] leaders =
        {
            "Brother Elias",
            "Sister Helena",
            "High Keeper Rowan",
            "Father Isaac",
            "Elder Miriam"
        };

        public static EnclaveData Generate(int? populationOverride = null)
        {
            EnclaveData data = new EnclaveData
            {
                Name = names[random.Next(names.Length)],
                Leader = leaders[random.Next(leaders.Length)],
                Population = populationOverride ?? random.Next(6, 13),
                Ideology = "Unknown",
                Friendly = true
            };

            data.InitializeReputation();
            data.EnsureLayoutAssignments(random);
            EnclaveIdeologyUtility.EnsureProfile(
                data,
                random,
                "new enclave generation"
            );
            EnclaveDevelopmentUtility.EnsureTier(
                data,
                "new enclave generation"
            );

            Log.Message(
                "[IEE] Generated persistent layout for " +
                data.Name +
                ": " +
                data.DescribeLayoutAssignments() +
                "."
            );

            return data;
        }
    }
}
