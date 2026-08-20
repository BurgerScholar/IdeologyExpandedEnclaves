using System;

namespace IdeologyExpandedEnclaves
{
    public sealed class EnclaveInterventionProfile
    {
        public PilgrimCamp Camp { get; internal set; }
        public EnclaveInterventionSide Side { get; internal set; }
        public EnclaveDistanceBand DistanceBand { get; internal set; }
        public float DistanceInTiles { get; internal set; }
        public EnclaveDevelopmentTier DevelopmentTier
        {
            get;
            internal set;
        }
        public EnclaveReputationTier ReputationTier
        {
            get;
            internal set;
        }
        public EnclaveIdeologyType IdeologyType { get; internal set; }
        public float BaseChance { get; internal set; }
        public float DistanceChance { get; internal set; }
        public float DevelopmentChance { get; internal set; }
        public float ReputationChance { get; internal set; }
        public float IdeologyChance { get; internal set; }
        public float ActivationChance { get; internal set; }
        public float ActivationRoll { get; internal set; }
        public int PartyStrength { get; internal set; }
        public string Flavor { get; internal set; }

        public bool RollSucceeded =>
            Side != EnclaveInterventionSide.None &&
            ActivationRoll < ActivationChance;
    }

    public static class EnclaveInterventionProfileUtility
    {
        public const float BaseActivationChance = 0.07f;
        public const float MaximumActivationChance = 0.60f;
        public const float MinimumEligibleActivationChance = 0.05f;

        public static EnclaveInterventionSide GetSide(
            EnclaveData data
        )
        {
            switch (
                data?.ReputationTier ?? EnclaveReputationTier.Neutral
            )
            {
                case EnclaveReputationTier.Friendly:
                case EnclaveReputationTier.Trusted:
                case EnclaveReputationTier.Revered:
                    return EnclaveInterventionSide.Friendly;
                case EnclaveReputationTier.Hostile:
                    return EnclaveInterventionSide.Hostile;
                default:
                    return EnclaveInterventionSide.None;
            }
        }

        public static string GetDispositionLabel(EnclaveData data)
        {
            switch (GetSide(data))
            {
                case EnclaveInterventionSide.Friendly:
                    return "May aid your colony";
                case EnclaveInterventionSide.Hostile:
                    return "May intervene against you";
                default:
                    return "Will not intervene";
            }
        }

        public static EnclaveInterventionProfile CreateProfile(
            PilgrimCamp camp,
            float distanceInTiles,
            EnclaveDistanceBand distanceBand,
            int rollSeed
        )
        {
            EnclaveData data = camp?.Data;
            EnclaveInterventionSide side = GetSide(data);
            EnclaveDevelopmentTier developmentTier =
                EnclaveDevelopmentUtility.GetTier(data);
            EnclaveReputationTier reputationTier =
                data?.ReputationTier ?? EnclaveReputationTier.Neutral;
            EnclaveIdeologyType ideologyType =
                EnclaveIdeologyUtility.GetIdeologyType(data);
            float distanceChance = GetDistanceChance(distanceBand);
            float developmentChance =
                GetDevelopmentChance(developmentTier);
            float reputationChance =
                GetReputationChance(reputationTier);
            float ideologyChance =
                GetIdeologyChance(ideologyType, side);
            float totalChance = side == EnclaveInterventionSide.None
                ? 0f
                : Clamp(
                    BaseActivationChance +
                    distanceChance +
                    developmentChance +
                    reputationChance +
                    ideologyChance,
                    MinimumEligibleActivationChance,
                    MaximumActivationChance
                );

            return new EnclaveInterventionProfile
            {
                Camp = camp,
                Side = side,
                DistanceBand = distanceBand,
                DistanceInTiles = distanceInTiles,
                DevelopmentTier = developmentTier,
                ReputationTier = reputationTier,
                IdeologyType = ideologyType,
                BaseChance = side == EnclaveInterventionSide.None
                    ? 0f
                    : BaseActivationChance,
                DistanceChance = side == EnclaveInterventionSide.None
                    ? 0f
                    : distanceChance,
                DevelopmentChance = side == EnclaveInterventionSide.None
                    ? 0f
                    : developmentChance,
                ReputationChance = side == EnclaveInterventionSide.None
                    ? 0f
                    : reputationChance,
                IdeologyChance = side == EnclaveInterventionSide.None
                    ? 0f
                    : ideologyChance,
                ActivationChance = totalChance,
                ActivationRoll = GetStableUnitValue(
                    rollSeed,
                    camp?.ID ?? 0,
                    0x51A7
                ),
                PartyStrength = side == EnclaveInterventionSide.None
                    ? 0
                    : CalculatePartyStrength(
                        camp,
                        side,
                        distanceBand,
                        rollSeed
                    ),
                Flavor = GetFlavor(ideologyType, side)
            };
        }

        private static float GetDistanceChance(
            EnclaveDistanceBand distanceBand
        )
        {
            switch (distanceBand)
            {
                case EnclaveDistanceBand.Strong:
                    return 0.15f;
                case EnclaveDistanceBand.Moderate:
                    return 0.08f;
                case EnclaveDistanceBand.Weak:
                    return 0.02f;
                default:
                    return 0f;
            }
        }

        private static float GetDevelopmentChance(
            EnclaveDevelopmentTier tier
        )
        {
            switch (tier)
            {
                case EnclaveDevelopmentTier.TierI:
                    return 0.02f;
                case EnclaveDevelopmentTier.TierII:
                    return 0.06f;
                case EnclaveDevelopmentTier.TierIII:
                    return 0.10f;
                case EnclaveDevelopmentTier.TierIV:
                    return 0.14f;
                default:
                    return 0f;
            }
        }

        private static float GetReputationChance(
            EnclaveReputationTier tier
        )
        {
            switch (tier)
            {
                case EnclaveReputationTier.Hostile:
                    return 0.08f;
                case EnclaveReputationTier.Trusted:
                    return 0.06f;
                case EnclaveReputationTier.Revered:
                    return 0.12f;
                default:
                    return 0f;
            }
        }

        private static float GetIdeologyChance(
            EnclaveIdeologyType ideologyType,
            EnclaveInterventionSide side
        )
        {
            bool friendly = side == EnclaveInterventionSide.Friendly;

            switch (ideologyType)
            {
                case EnclaveIdeologyType.Communal:
                    return friendly ? 0.06f : -0.03f;
                case EnclaveIdeologyType.Isolationist:
                    return friendly ? -0.07f : 0.04f;
                case EnclaveIdeologyType.Martial:
                    return friendly ? 0.06f : 0.08f;
                case EnclaveIdeologyType.Mercantile:
                    return friendly ? 0.03f : 0f;
                case EnclaveIdeologyType.Nature:
                    return friendly ? 0f : -0.03f;
                case EnclaveIdeologyType.Spiritual:
                    return friendly ? 0.03f : 0f;
                case EnclaveIdeologyType.Transhumanist:
                    return friendly ? 0.01f : 0.03f;
                default:
                    return 0f;
            }
        }

        private static int CalculatePartyStrength(
            PilgrimCamp camp,
            EnclaveInterventionSide side,
            EnclaveDistanceBand distanceBand,
            int rollSeed
        )
        {
            int minimum;
            int maximum;

            switch (EnclaveDevelopmentUtility.GetTier(camp?.Data))
            {
                case EnclaveDevelopmentTier.TierI:
                    minimum = 1;
                    maximum = 2;
                    break;
                case EnclaveDevelopmentTier.TierII:
                    minimum = 2;
                    maximum = 3;
                    break;
                case EnclaveDevelopmentTier.TierIII:
                    minimum = 3;
                    maximum = 5;
                    break;
                case EnclaveDevelopmentTier.TierIV:
                    minimum = 4;
                    maximum = 6;
                    break;
                default:
                    minimum = 1;
                    maximum = 1;
                    break;
            }

            uint variation = StableHash(
                rollSeed,
                camp?.ID ?? 0,
                0x7A11
            );
            int count = minimum +
                (int)(variation % (uint)(maximum - minimum + 1));

            if (distanceBand == EnclaveDistanceBand.Strong)
            {
                count++;
            }
            else if (distanceBand == EnclaveDistanceBand.Weak)
            {
                count--;
            }

            EnclaveIdeologyType ideologyType =
                EnclaveIdeologyUtility.GetIdeologyType(camp?.Data);

            if (ideologyType == EnclaveIdeologyType.Martial)
            {
                count++;
            }
            else if (
                side == EnclaveInterventionSide.Friendly &&
                ideologyType == EnclaveIdeologyType.Isolationist
            )
            {
                count--;
            }

            return Math.Max(minimum, Math.Min(maximum, count));
        }

        private static string GetFlavor(
            EnclaveIdeologyType ideologyType,
            EnclaveInterventionSide side
        )
        {
            if (side == EnclaveInterventionSide.None)
            {
                return string.Empty;
            }

            switch (ideologyType)
            {
                case EnclaveIdeologyType.Communal:
                    return side == EnclaveInterventionSide.Friendly
                        ? "Their communal convictions call them to mutual defense."
                        : "Their fighters have come to punish those they reject.";
                case EnclaveIdeologyType.Isolationist:
                    return "They have chosen to act despite their usual isolation.";
                case EnclaveIdeologyType.Martial:
                    return "Their martial tradition has dispatched an organized war party.";
                case EnclaveIdeologyType.Mercantile:
                    return "They consider the colony's fate important to regional stability.";
                case EnclaveIdeologyType.Nature:
                    return "Their defenders advance with austere, practical purpose.";
                case EnclaveIdeologyType.Spiritual:
                    return "They believe the battle carries spiritual consequence.";
                case EnclaveIdeologyType.Transhumanist:
                    return "Their fighters see intervention as a necessary step toward their future.";
                default:
                    return string.Empty;
            }
        }

        private static float GetStableUnitValue(
            int seed,
            int campId,
            int salt
        )
        {
            uint value = StableHash(seed, campId, salt);
            return (value & 0x00FFFFFFu) / 16777216f;
        }

        private static uint StableHash(
            int seed,
            int campId,
            int salt
        )
        {
            unchecked
            {
                uint value = 2166136261u;
                value = (value ^ (uint)seed) * 16777619u;
                value = (value ^ (uint)campId) * 16777619u;
                value = (value ^ (uint)salt) * 16777619u;
                value ^= value >> 13;
                value *= 1274126177u;
                value ^= value >> 16;
                return value;
            }
        }

        private static float Clamp(
            float value,
            float minimum,
            float maximum
        )
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }
    }
}
