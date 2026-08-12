using System;
using System.Collections.Generic;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public sealed class EnclaveNeedProfile
    {
        public EnclaveNeedType Type { get; }
        public string Label { get; }
        public int BaseDemandPerPawn { get; }
        public int BaseRequestQuantity { get; }

        public EnclaveNeedProfile(
            EnclaveNeedType type,
            string label,
            int baseDemandPerPawn,
            int baseRequestQuantity
        )
        {
            Type = type;
            Label = label;
            BaseDemandPerPawn = baseDemandPerPawn;
            BaseRequestQuantity = baseRequestQuantity;
        }
    }

    public sealed class EnclaveIdeologyNeedProfile
    {
        private readonly Dictionary<EnclaveNeedType, int>
            demandPercentByNeed;

        public int GeneralSupplyPercentBonus { get; }

        public EnclaveIdeologyNeedProfile(
            int generalSupplyPercentBonus,
            params KeyValuePair<EnclaveNeedType, int>[] demandModifiers
        )
        {
            GeneralSupplyPercentBonus = generalSupplyPercentBonus;
            demandPercentByNeed =
                new Dictionary<EnclaveNeedType, int>();

            foreach (
                KeyValuePair<EnclaveNeedType, int> modifier in
                demandModifiers
            )
            {
                demandPercentByNeed[modifier.Key] = modifier.Value;
            }
        }

        public int GetDemandPercent(EnclaveNeedType needType)
        {
            int percent;

            return demandPercentByNeed.TryGetValue(
                needType,
                out percent
            )
                ? percent
                : EnclaveNeedsUtility.BaseDemandPercent;
        }
    }

    public static class EnclaveNeedsUtility
    {
        public const int BaseDemandPercent = 100;
        public const int EvaluationMovementPercent = 20;

        public const int NoneMinimumSupplyPercent = 90;
        public const int LowMinimumSupplyPercent = 70;
        public const int ModerateMinimumSupplyPercent = 45;
        public const int SevereMinimumSupplyPercent = 20;

        private static readonly Dictionary<
            EnclaveNeedType,
            EnclaveNeedProfile
        > needProfiles = new Dictionary<
            EnclaveNeedType,
            EnclaveNeedProfile
        >
        {
            {
                EnclaveNeedType.Food,
                new EnclaveNeedProfile(
                    EnclaveNeedType.Food,
                    "Food",
                    4,
                    20
                )
            },
            {
                EnclaveNeedType.Medicine,
                new EnclaveNeedProfile(
                    EnclaveNeedType.Medicine,
                    "Medicine",
                    1,
                    5
                )
            },
            {
                EnclaveNeedType.BuildingMaterials,
                new EnclaveNeedProfile(
                    EnclaveNeedType.BuildingMaterials,
                    "Building Materials",
                    8,
                    40
                )
            },
            {
                EnclaveNeedType.Textiles,
                new EnclaveNeedProfile(
                    EnclaveNeedType.Textiles,
                    "Textiles",
                    4,
                    30
                )
            },
            {
                EnclaveNeedType.Components,
                new EnclaveNeedProfile(
                    EnclaveNeedType.Components,
                    "Components",
                    1,
                    6
                )
            }
        };

        private static readonly Dictionary<
            EnclaveIdeologyType,
            EnclaveIdeologyNeedProfile
        > ideologyProfiles = new Dictionary<
            EnclaveIdeologyType,
            EnclaveIdeologyNeedProfile
        >
        {
            {
                EnclaveIdeologyType.Communal,
                CreateIdeologyProfile(
                    0,
                    EnclaveNeedType.Food, 130,
                    EnclaveNeedType.Textiles, 120
                )
            },
            {
                EnclaveIdeologyType.Isolationist,
                CreateIdeologyProfile(
                    0,
                    EnclaveNeedType.BuildingMaterials, 130,
                    EnclaveNeedType.Food, 115
                )
            },
            {
                EnclaveIdeologyType.Martial,
                CreateIdeologyProfile(
                    0,
                    EnclaveNeedType.Medicine, 130,
                    EnclaveNeedType.Components, 120,
                    EnclaveNeedType.BuildingMaterials, 110
                )
            },
            {
                EnclaveIdeologyType.Mercantile,
                CreateIdeologyProfile(
                    8,
                    EnclaveNeedType.Components, 125
                )
            },
            {
                EnclaveIdeologyType.Nature,
                CreateIdeologyProfile(
                    0,
                    EnclaveNeedType.Medicine, 125,
                    EnclaveNeedType.Components, 75
                )
            },
            {
                EnclaveIdeologyType.Spiritual,
                CreateIdeologyProfile(
                    0,
                    EnclaveNeedType.Food, 110,
                    EnclaveNeedType.Textiles, 115,
                    EnclaveNeedType.Components, 80
                )
            },
            {
                EnclaveIdeologyType.Transhumanist,
                CreateIdeologyProfile(
                    0,
                    EnclaveNeedType.Components, 150,
                    EnclaveNeedType.Medicine, 130
                )
            }
        };

        private static readonly EnclaveIdeologyNeedProfile
            defaultIdeologyProfile =
                new EnclaveIdeologyNeedProfile(0);

        public static IReadOnlyList<EnclaveNeedRecord> GetNeeds(
            PilgrimCamp camp
        )
        {
            return GetNeeds(camp?.Data);
        }

        public static IReadOnlyList<EnclaveNeedRecord> GetNeeds(
            EnclaveData data
        )
        {
            EnsureNeeds(data);
            return data?.Needs ?? Array.Empty<EnclaveNeedRecord>();
        }

        public static List<EnclaveNeedRecord> GetShortages(
            PilgrimCamp camp
        )
        {
            List<EnclaveNeedRecord> shortages =
                new List<EnclaveNeedRecord>();

            foreach (EnclaveNeedRecord need in GetNeeds(camp))
            {
                if (need.IsShortage)
                {
                    shortages.Add(need);
                }
            }

            shortages.Sort(CompareUrgency);
            return shortages;
        }

        public static EnclaveNeedRecord GetMostUrgentNeed(
            PilgrimCamp camp
        )
        {
            List<EnclaveNeedRecord> shortages = GetShortages(camp);
            return shortages.Count > 0 ? shortages[0] : null;
        }

        public static bool HasCriticalShortage(PilgrimCamp camp)
        {
            foreach (EnclaveNeedRecord need in GetNeeds(camp))
            {
                if (need.Severity == EnclaveNeedSeverity.Critical)
                {
                    return true;
                }
            }

            return false;
        }

        public static EnclaveNeedRecord GetNeed(
            EnclaveData data,
            EnclaveNeedType needType
        )
        {
            EnsureNeeds(data);

            if (data?.Needs == null)
            {
                return null;
            }

            foreach (EnclaveNeedRecord need in data.Needs)
            {
                if (need.Type == needType)
                {
                    return need;
                }
            }

            return null;
        }

        public static bool EnsureNeeds(
            EnclaveData data,
            string reason = null
        )
        {
            if (data == null)
            {
                return false;
            }

            List<EnclaveNeedRecord> existing =
                data.MutableNeeds ?? new List<EnclaveNeedRecord>();
            Dictionary<EnclaveNeedType, EnclaveNeedRecord> byType =
                new Dictionary<EnclaveNeedType, EnclaveNeedRecord>();

            foreach (EnclaveNeedRecord need in existing)
            {
                if (
                    need == null ||
                    !Enum.IsDefined(typeof(EnclaveNeedType), need.Type) ||
                    byType.ContainsKey(need.Type)
                )
                {
                    continue;
                }

                byType.Add(need.Type, need);
            }

            bool changed = byType.Count != existing.Count;
            List<EnclaveNeedRecord> normalized =
                new List<EnclaveNeedRecord>();
            int currentTick = Find.TickManager?.TicksGame ?? 0;

            foreach (
                EnclaveNeedType needType in
                (EnclaveNeedType[])Enum.GetValues(
                    typeof(EnclaveNeedType)
                )
            )
            {
                EnclaveNeedRecord need;

                if (!byType.TryGetValue(needType, out need))
                {
                    int target = CalculateTargetAmount(data, needType);

                    need = new EnclaveNeedRecord(
                        needType,
                        target,
                        target,
                        EnclaveNeedSeverity.None,
                        currentTick
                    );
                    changed = true;
                }

                normalized.Add(need);
            }

            data.MutableNeeds = normalized;

            if (changed)
            {
                Log.Message(
                    "[IEE] Initialized persistent needs for " +
                    (data.Name ?? "an enclave") +
                    (reason.NullOrEmpty()
                        ? "."
                        : " (" + reason + ").")
                );
            }

            return changed;
        }

        public static bool EvaluateNeeds(PilgrimCamp camp)
        {
            if (camp?.Data == null)
            {
                return false;
            }

            EnsureNeeds(camp.Data);

            int currentTick = Find.TickManager?.TicksGame ?? 0;
            EnclaveRegionalStatus regionalStatus =
                EnclaveInfluenceUtility
                    .CalculateRegionalSummary(camp)
                    .Status;
            bool changed = false;

            foreach (EnclaveNeedRecord need in camp.Data.Needs)
            {
                if (need.LastEvaluationTick == currentTick)
                {
                    continue;
                }

                int target = CalculateTargetAmount(
                    camp.Data,
                    need.Type
                );
                int desiredSupply = CalculateModeledSupply(
                    camp.Data,
                    need.Type,
                    regionalStatus
                );
                int movementLimit = Math.Max(
                    1,
                    DivideRoundUp(
                        (long)target * EvaluationMovementPercent,
                        100
                    )
                );
                int updatedSupply = MoveToward(
                    need.EstimatedSupply,
                    desiredSupply,
                    movementLimit
                );
                EnclaveNeedSeverity updatedSeverity = GetSeverity(
                    target,
                    updatedSupply
                );

                changed |=
                    need.TargetAmount != target ||
                    need.EstimatedSupply != updatedSupply ||
                    need.Severity != updatedSeverity;

                if (
                    Prefs.DevMode &&
                    need.Severity != updatedSeverity
                )
                {
                    Log.Message(
                        "[IEE] Need severity for " +
                        camp.Data.Name +
                        " / " +
                        GetNeedLabel(need.Type) +
                        " changed from " +
                        need.Severity +
                        " to " +
                        updatedSeverity +
                        " (regional status " +
                        EnclaveProximityProfileUtility
                            .GetRegionalStatusLabel(regionalStatus) +
                        ")."
                    );
                }

                need.ApplyEvaluation(
                    target,
                    updatedSupply,
                    updatedSeverity,
                    currentTick
                );
            }

            return changed;
        }

        public static bool SetNeedSeverity(
            EnclaveData data,
            EnclaveNeedType needType,
            EnclaveNeedSeverity severity,
            string reason = null
        )
        {
            if (
                data == null ||
                !Enum.IsDefined(typeof(EnclaveNeedType), needType) ||
                !Enum.IsDefined(
                    typeof(EnclaveNeedSeverity),
                    severity
                )
            )
            {
                return false;
            }

            EnclaveNeedRecord need = GetNeed(data, needType);

            if (need == null)
            {
                return false;
            }

            int target = CalculateTargetAmount(data, needType);
            int supplyPercent = GetRepresentativeSupplyPercent(
                severity
            );
            int supply = DivideRoundUp(
                (long)target * supplyPercent,
                100
            );
            EnclaveNeedSeverity previous = need.Severity;

            need.ApplyEvaluation(
                target,
                supply,
                severity,
                Find.TickManager?.TicksGame ?? 0
            );

            Log.Message(
                "[IEE] Set " +
                GetNeedLabel(needType) +
                " need for " +
                (data.Name ?? "an enclave") +
                " from " +
                previous +
                " to " +
                severity +
                (reason.NullOrEmpty()
                    ? "."
                    : " (" + reason + ").")
            );

            return true;
        }

        public static bool AdjustNeed(
            EnclaveData data,
            EnclaveNeedType needType,
            int estimatedSupplyDelta,
            string reason = null
        )
        {
            EnclaveNeedRecord need = GetNeed(data, needType);

            if (need == null)
            {
                return false;
            }

            int target = CalculateTargetAmount(data, needType);
            int updatedSupply = Math.Max(
                0,
                AddClamped(need.EstimatedSupply, estimatedSupplyDelta)
            );
            EnclaveNeedSeverity updatedSeverity = GetSeverity(
                target,
                updatedSupply
            );

            need.ApplyEvaluation(
                target,
                updatedSupply,
                updatedSeverity,
                Find.TickManager?.TicksGame ?? 0
            );

            Log.Message(
                "[IEE] Adjusted " +
                GetNeedLabel(needType) +
                " estimated supply for " +
                (data?.Name ?? "an enclave") +
                " by " +
                estimatedSupplyDelta +
                ": severity " +
                updatedSeverity +
                (reason.NullOrEmpty()
                    ? "."
                    : " (" + reason + ").")
            );

            return true;
        }

        public static EnclaveNeedSeverity GetSeverity(
            int targetAmount,
            int estimatedSupply
        )
        {
            if (targetAmount <= 0)
            {
                return EnclaveNeedSeverity.None;
            }

            long supplyPercent =
                (long)Math.Max(0, estimatedSupply) * 100 /
                targetAmount;

            if (supplyPercent >= NoneMinimumSupplyPercent)
            {
                return EnclaveNeedSeverity.None;
            }

            if (supplyPercent >= LowMinimumSupplyPercent)
            {
                return EnclaveNeedSeverity.Low;
            }

            if (supplyPercent >= ModerateMinimumSupplyPercent)
            {
                return EnclaveNeedSeverity.Moderate;
            }

            if (supplyPercent >= SevereMinimumSupplyPercent)
            {
                return EnclaveNeedSeverity.Severe;
            }

            return EnclaveNeedSeverity.Critical;
        }

        public static EnclaveShortageLevel GetShortageLevel(
            EnclaveNeedSeverity severity
        )
        {
            switch (severity)
            {
                case EnclaveNeedSeverity.Moderate:
                    return EnclaveShortageLevel.Minor;
                case EnclaveNeedSeverity.Severe:
                    return EnclaveShortageLevel.Serious;
                case EnclaveNeedSeverity.Critical:
                    return EnclaveShortageLevel.Emergency;
                default:
                    return EnclaveShortageLevel.None;
            }
        }

        public static string GetNeedLabel(EnclaveNeedType needType)
        {
            EnclaveNeedProfile profile;

            return needProfiles.TryGetValue(needType, out profile)
                ? profile.Label
                : needType.ToString();
        }

        public static EnclaveNeedProfile GetNeedProfile(
            EnclaveNeedType needType
        )
        {
            EnclaveNeedProfile profile;
            needProfiles.TryGetValue(needType, out profile);
            return profile;
        }

        public static int CalculateTargetAmount(
            EnclaveData data,
            EnclaveNeedType needType
        )
        {
            EnclaveNeedProfile profile = GetNeedProfile(needType);

            if (profile == null)
            {
                return 1;
            }

            int population = Math.Max(1, data?.Population ?? 1);
            int developmentPercent =
                GetDevelopmentConsumptionPercent(data);
            int ideologyPercent = GetIdeologyProfile(data)
                .GetDemandPercent(needType);

            return Math.Max(
                1,
                DivideRoundUp(
                    (long)population *
                    profile.BaseDemandPerPawn *
                    developmentPercent *
                    ideologyPercent,
                    10000
                )
            );
        }

        public static int CalculateModeledSupply(
            EnclaveData data,
            EnclaveNeedType needType,
            EnclaveRegionalStatus regionalStatus
        )
        {
            EnclaveNeedProfile profile = GetNeedProfile(needType);

            if (profile == null)
            {
                return 0;
            }

            int population = Math.Max(1, data?.Population ?? 1);
            int developmentConsumption =
                GetDevelopmentConsumptionPercent(data);
            int capacityPercent = Math.Max(
                20,
                Math.Min(
                    120,
                    GetDevelopmentResiliencePercent(data) +
                    GetRegionalSupplyAdjustment(regionalStatus) +
                    GetIdeologyProfile(data)
                        .GeneralSupplyPercentBonus
                )
            );

            return Math.Max(
                0,
                DivideRoundUp(
                    (long)population *
                    profile.BaseDemandPerPawn *
                    developmentConsumption *
                    capacityPercent,
                    10000
                )
            );
        }

        private static int GetDevelopmentConsumptionPercent(
            EnclaveData data
        )
        {
            switch (EnclaveDevelopmentUtility.GetTier(data))
            {
                case EnclaveDevelopmentTier.TierII:
                    return 110;
                case EnclaveDevelopmentTier.TierIII:
                    return 120;
                case EnclaveDevelopmentTier.TierIV:
                    return 130;
                default:
                    return 100;
            }
        }

        private static int GetDevelopmentResiliencePercent(
            EnclaveData data
        )
        {
            switch (EnclaveDevelopmentUtility.GetTier(data))
            {
                case EnclaveDevelopmentTier.TierII:
                    return 86;
                case EnclaveDevelopmentTier.TierIII:
                    return 90;
                case EnclaveDevelopmentTier.TierIV:
                    return 94;
                default:
                    return 82;
            }
        }

        private static int GetRegionalSupplyAdjustment(
            EnclaveRegionalStatus regionalStatus
        )
        {
            switch (regionalStatus)
            {
                case EnclaveRegionalStatus.StronglyPressured:
                    return -15;
                case EnclaveRegionalStatus.Pressured:
                    return -8;
                case EnclaveRegionalStatus.Supported:
                    return 8;
                case EnclaveRegionalStatus.StronglySupported:
                    return 15;
                default:
                    return 0;
            }
        }

        private static EnclaveIdeologyNeedProfile GetIdeologyProfile(
            EnclaveData data
        )
        {
            EnclaveIdeologyNeedProfile profile;

            return ideologyProfiles.TryGetValue(
                EnclaveIdeologyUtility.GetIdeologyType(data),
                out profile
            )
                ? profile
                : defaultIdeologyProfile;
        }

        private static EnclaveIdeologyNeedProfile
            CreateIdeologyProfile(
            int generalSupplyPercentBonus,
            params object[] modifiers
        )
        {
            List<KeyValuePair<EnclaveNeedType, int>> pairs =
                new List<KeyValuePair<EnclaveNeedType, int>>();

            for (int index = 0; index + 1 < modifiers.Length; index += 2)
            {
                pairs.Add(
                    new KeyValuePair<EnclaveNeedType, int>(
                        (EnclaveNeedType)modifiers[index],
                        (int)modifiers[index + 1]
                    )
                );
            }

            return new EnclaveIdeologyNeedProfile(
                generalSupplyPercentBonus,
                pairs.ToArray()
            );
        }

        private static int GetRepresentativeSupplyPercent(
            EnclaveNeedSeverity severity
        )
        {
            switch (severity)
            {
                case EnclaveNeedSeverity.Low:
                    return 80;
                case EnclaveNeedSeverity.Moderate:
                    return 55;
                case EnclaveNeedSeverity.Severe:
                    return 30;
                case EnclaveNeedSeverity.Critical:
                    return 10;
                default:
                    return 100;
            }
        }

        private static int MoveToward(
            int current,
            int target,
            int maximumChange
        )
        {
            if (current < target)
            {
                return Math.Min(target, current + maximumChange);
            }

            if (current > target)
            {
                return Math.Max(target, current - maximumChange);
            }

            return current;
        }

        private static int DivideRoundUp(long value, int divisor)
        {
            if (value <= 0 || divisor <= 0)
            {
                return 0;
            }

            long result = (value + divisor - 1) / divisor;
            return result >= int.MaxValue
                ? int.MaxValue
                : (int)result;
        }

        private static int AddClamped(int current, int adjustment)
        {
            long value = (long)current + adjustment;

            if (value <= 0)
            {
                return 0;
            }

            return value >= int.MaxValue
                ? int.MaxValue
                : (int)value;
        }

        private static int CompareUrgency(
            EnclaveNeedRecord first,
            EnclaveNeedRecord second
        )
        {
            int severityComparison = second.Severity.CompareTo(
                first.Severity
            );

            return severityComparison != 0
                ? severityComparison
                : first.Type.CompareTo(second.Type);
        }
    }
}
