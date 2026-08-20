using System;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveExpeditionUtility
    {
        public const int DailyEvaluationTicks = 60000;
        public const int ShortRetryTicks = 180000;
        public const int MinimumCooldownDays = 25;
        public const int MaximumPartySize = 6;
        public const int MaximumColonyVisitDistance = 30;
        public const int MaximumColonyVisitChancePercent = 75;

        public static EnclaveExpeditionPurpose GetPurpose(
            EnclaveData data
        )
        {
            switch (EnclaveArchetypeUtility.GetArchetype(data))
            {
                case EnclaveArchetype.TradeCompact:
                    return EnclaveExpeditionPurpose.Trade;
                case EnclaveArchetype.WarriorCovenant:
                    return EnclaveExpeditionPurpose.Patrol;
                default:
                    return EnclaveExpeditionPurpose.Relief;
            }
        }

        public static string GetPurposeLabel(
            EnclaveExpeditionPurpose purpose
        )
        {
            switch (purpose)
            {
                case EnclaveExpeditionPurpose.Trade:
                    return "Trade Expedition";
                case EnclaveExpeditionPurpose.Patrol:
                    return "Patrol Expedition";
                default:
                    return "Relief Expedition";
            }
        }

        public static string GetSiteTypeLabel(
            EnclaveExpeditionPurpose purpose
        )
        {
            switch (purpose)
            {
                case EnclaveExpeditionPurpose.Trade:
                    return "Trade Outpost";
                case EnclaveExpeditionPurpose.Patrol:
                    return "Patrol Camp";
                default:
                    return "Relief Camp";
            }
        }

        public static string GetColonyVisitTypeLabel(
            EnclaveExpeditionPurpose purpose
        )
        {
            switch (purpose)
            {
                case EnclaveExpeditionPurpose.Trade:
                    return "Merchant Delegation";
                case EnclaveExpeditionPurpose.Patrol:
                    return "Patrol Visit";
                default:
                    return "Relief Visit";
            }
        }

        public static string GetTraderKindDefName(
            EnclaveExpeditionPurpose purpose
        )
        {
            switch (purpose)
            {
                case EnclaveExpeditionPurpose.Trade:
                    return "IEE_ExpeditionTradeTrader";
                case EnclaveExpeditionPurpose.Patrol:
                    return "IEE_ExpeditionPatrolTrader";
                default:
                    return "IEE_ExpeditionReliefTrader";
            }
        }

        public static int GetDurationTicks(
            EnclaveExpeditionPurpose purpose
        )
        {
            switch (purpose)
            {
                case EnclaveExpeditionPurpose.Trade:
                    return 15 * 60000;
                case EnclaveExpeditionPurpose.Patrol:
                    return 10 * 60000;
                default:
                    return 12 * 60000;
            }
        }

        public static int GetColonyVisitDurationTicks(
            EnclaveExpeditionPurpose purpose
        )
        {
            return (
                purpose == EnclaveExpeditionPurpose.Trade ? 3 : 2
            ) * 60000;
        }

        public static int GetColonyVisitChancePercent(
            EnclaveData data,
            float distanceInTiles
        )
        {
            if (data == null)
            {
                return 0;
            }

            EnclaveExpeditionPurpose purpose = GetPurpose(data);
            int chance;

            switch (data.ReputationTier)
            {
                case EnclaveReputationTier.Friendly:
                    chance = 30;
                    break;
                case EnclaveReputationTier.Trusted:
                    chance = 45;
                    break;
                case EnclaveReputationTier.Revered:
                    chance = 60;
                    break;
                case EnclaveReputationTier.Neutral:
                    if (purpose != EnclaveExpeditionPurpose.Trade)
                    {
                        return 0;
                    }

                    chance = 20;
                    break;
                default:
                    return 0;
            }

            if (distanceInTiles <= 10f)
            {
                chance += 10;
            }
            else if (distanceInTiles > 20f)
            {
                chance -= 10;
            }

            switch (purpose)
            {
                case EnclaveExpeditionPurpose.Relief:
                case EnclaveExpeditionPurpose.Trade:
                    chance += 10;
                    break;
            }

            return Math.Max(
                0,
                Math.Min(MaximumColonyVisitChancePercent, chance)
            );
        }

        public static float GetStableColonyVisitRoll(
            PilgrimCamp camp,
            int evaluationTick
        )
        {
            uint hash = StableHash(
                camp?.ID ?? 0,
                evaluationTick,
                0x6C19
            );

            return (hash % 10000u) / 100f;
        }

        public static int GetCooldownTicks(EnclaveData data)
        {
            int days;

            switch (EnclaveDevelopmentUtility.GetTier(data))
            {
                case EnclaveDevelopmentTier.TierI:
                    days = 45;
                    break;
                case EnclaveDevelopmentTier.TierIII:
                    days = 35;
                    break;
                case EnclaveDevelopmentTier.TierIV:
                    days = 30;
                    break;
                default:
                    days = 40;
                    break;
            }

            if (
                GetPurpose(data) == EnclaveExpeditionPurpose.Trade
            )
            {
                days -= 5;
            }

            return Math.Max(MinimumCooldownDays, days) * 60000;
        }

        public static int GetGenerationChancePercent(
            PilgrimCamp camp,
            EnclaveRegionalStatus? regionalStatus = null
        )
        {
            int chance = 8;

            switch (
                EnclaveDevelopmentUtility.GetTier(camp?.Data)
            )
            {
                case EnclaveDevelopmentTier.TierII:
                    chance += 2;
                    break;
                case EnclaveDevelopmentTier.TierIII:
                    chance += 4;
                    break;
                case EnclaveDevelopmentTier.TierIV:
                    chance += 6;
                    break;
            }

            EnclaveRegionalStatus status = regionalStatus ??
                EnclaveInfluenceUtility
                    .CalculateRegionalSummary(camp)
                    .Status;

            switch (status)
            {
                case EnclaveRegionalStatus.StronglyPressured:
                    chance -= 3;
                    break;
                case EnclaveRegionalStatus.Pressured:
                    chance -= 1;
                    break;
                case EnclaveRegionalStatus.Supported:
                    chance += 1;
                    break;
                case EnclaveRegionalStatus.StronglySupported:
                    chance += 2;
                    break;
            }

            return Math.Max(3, Math.Min(18, chance));
        }

        public static float GetStableEvaluationRoll(
            PilgrimCamp camp,
            int evaluationTick
        )
        {
            uint hash = StableHash(
                camp?.ID ?? 0,
                evaluationTick,
                (int)GetPurpose(camp?.Data)
            );

            return (hash % 10000u) / 100f;
        }

        public static int GetDestinationSeed(
            PilgrimCamp camp,
            int evaluationTick
        )
        {
            return unchecked((int)StableHash(
                camp?.ID ?? 0,
                evaluationTick,
                0x45A9
            ));
        }

        public static int GetPartySize(EnclaveData data)
        {
            int count;

            switch (EnclaveDevelopmentUtility.GetTier(data))
            {
                case EnclaveDevelopmentTier.TierI:
                    count = 2;
                    break;
                case EnclaveDevelopmentTier.TierIII:
                    count = 4;
                    break;
                case EnclaveDevelopmentTier.TierIV:
                    count = 5;
                    break;
                default:
                    count = 3;
                    break;
            }

            if (
                GetPurpose(data) == EnclaveExpeditionPurpose.Patrol
            )
            {
                count++;
            }

            return Math.Max(1, Math.Min(MaximumPartySize, count));
        }

        public static string GetSizeLabel(EnclaveData data)
        {
            int size = GetPartySize(data);

            if (size <= 2)
            {
                return "Small";
            }

            return size <= 4 ? "Modest" : "Substantial";
        }

        public static string FormatRemainingTime(int expirationTick)
        {
            int currentTick = Verse.Find.TickManager?.TicksGame ?? 0;
            int remaining = Math.Max(0, expirationTick - currentTick);

            if (remaining <= 0)
            {
                return "expiring";
            }

            float days = remaining / 60000f;
            return days < 1f
                ? "less than one day"
                : days.ToString("0.#") + " days";
        }

        private static uint StableHash(int first, int second, int salt)
        {
            unchecked
            {
                uint value = 2166136261u;
                value = (value ^ (uint)first) * 16777619u;
                value = (value ^ (uint)second) * 16777619u;
                value = (value ^ (uint)salt) * 16777619u;
                value ^= value >> 13;
                value *= 1274126177u;
                value ^= value >> 16;
                return value;
            }
        }
    }
}
