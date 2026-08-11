using System;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveDevelopmentUtility
    {
        public static EnclaveDevelopmentTier GetTier(
            EnclaveData data
        )
        {
            return IsValid(data?.DevelopmentTier ??
                    EnclaveDevelopmentTier.Unassigned)
                ? data.DevelopmentTier
                : EnclaveDevelopmentTier.Unassigned;
        }

        public static string GetDisplayName(EnclaveData data)
        {
            return GetDisplayName(GetTier(data));
        }

        public static string GetDisplayName(
            EnclaveDevelopmentTier tier
        )
        {
            switch (tier)
            {
                case EnclaveDevelopmentTier.TierI:
                    return "Tier I \u2014 Small Camp";
                case EnclaveDevelopmentTier.TierII:
                    return "Tier II \u2014 Established Enclave";
                case EnclaveDevelopmentTier.TierIII:
                    return "Tier III \u2014 Developed Enclave";
                case EnclaveDevelopmentTier.TierIV:
                    return "Tier IV \u2014 Major Enclave";
                default:
                    return "Unassigned";
            }
        }

        public static bool SetTier(
            EnclaveData data,
            EnclaveDevelopmentTier tier,
            string reason = null
        )
        {
            if (data == null || !IsValid(tier))
            {
                return false;
            }

            EnclaveDevelopmentTier previous = GetTier(data);

            if (previous == tier)
            {
                return true;
            }

            data.DevelopmentTier = tier;

            Log.Message(
                "[IEE] Development tier for " +
                (data.Name ?? "an enclave") +
                " changed from " +
                GetDisplayName(previous) +
                " to " +
                GetDisplayName(tier) +
                (reason.NullOrEmpty()
                    ? "."
                    : " (" + reason + ").")
            );

            return true;
        }

        public static int GetNumericTier(EnclaveData data)
        {
            switch (GetTier(data))
            {
                case EnclaveDevelopmentTier.TierI:
                    return 1;
                case EnclaveDevelopmentTier.TierII:
                    return 2;
                case EnclaveDevelopmentTier.TierIII:
                    return 3;
                case EnclaveDevelopmentTier.TierIV:
                    return 4;
                default:
                    return 0;
            }
        }

        public static bool EnsureTier(
            EnclaveData data,
            string reason = null
        )
        {
            if (data == null)
            {
                return false;
            }

            if (IsValid(data.DevelopmentTier))
            {
                if (data.DevelopmentTierInitialPopulation < 0)
                {
                    data.DevelopmentTierInitialPopulation =
                        data.Population;
                }

                return false;
            }

            data.DevelopmentTier = FromInitialPopulation(
                data.Population
            );
            data.DevelopmentTierInitialPopulation = data.Population;

            Log.Message(
                "[IEE] Assigned persistent development tier for " +
                (data.Name ?? "an enclave") +
                ": " +
                GetDisplayName(data) +
                " from population " +
                data.DevelopmentTierInitialPopulation +
                (reason.NullOrEmpty()
                    ? "."
                    : " (" + reason + ").")
            );

            return true;
        }

        private static EnclaveDevelopmentTier FromInitialPopulation(
            int population
        )
        {
            if (population <= 7)
            {
                return EnclaveDevelopmentTier.TierI;
            }

            if (population <= 9)
            {
                return EnclaveDevelopmentTier.TierII;
            }

            if (population <= 11)
            {
                return EnclaveDevelopmentTier.TierIII;
            }

            return EnclaveDevelopmentTier.TierIV;
        }

        private static bool IsValid(
            EnclaveDevelopmentTier tier
        )
        {
            return
                tier >= EnclaveDevelopmentTier.TierI &&
                tier <= EnclaveDevelopmentTier.TierIV &&
                Enum.IsDefined(typeof(EnclaveDevelopmentTier), tier);
        }
    }
}
