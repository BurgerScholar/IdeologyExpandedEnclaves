using System;
using System.Collections.Generic;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public sealed class EnclaveArchetypeTraderStockEntry
    {
        public string ThingDefName { get; }
        public string StuffDefName { get; }
        public int Count { get; }

        public EnclaveArchetypeTraderStockEntry(
            string thingDefName,
            int count,
            string stuffDefName = null
        )
        {
            ThingDefName = thingDefName;
            StuffDefName = stuffDefName;
            Count = count;
        }
    }

    public sealed class EnclaveArchetypeProfile
    {
        private readonly Dictionary<EnclaveNeedType, int>
            needDemandBonusPercent;
        private readonly Dictionary<EnclaveNeedType, int>
            needSupplyBonusPercent;
        private readonly Dictionary<EnclaveNeedType, int>
            supplyRequestPriority;
        private readonly Dictionary<string, int>
            visualStorageWeightBonus;
        private readonly Dictionary<string, int>
            visualStorageQuantityBonusPercent;
        private readonly List<EnclaveArchetypeTraderStockEntry>
            initialTraderStock;

        public EnclaveArchetype Archetype { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string BenefitSummary { get; }
        public int RecruitmentPriceAdjustmentPercent { get; }
        public int TradeFavorableBonusPercent { get; }
        public int GeneralSupplyCapacityBonusPercent { get; }
        public int SupplyRequestCooldownTicks { get; }
        public float FriendlyInterventionChance { get; }
        public float HostileInterventionChance { get; }
        public int InterventionPartyStrengthBonus { get; }
        public int GatheringSeatBonus { get; }
        public int StorageStackBonus { get; }
        public int InternalSpacingAdjustment { get; }
        public string OrganizationSuffix { get; }
        public IReadOnlyList<EnclaveArchetypeTraderStockEntry>
            InitialTraderStock => initialTraderStock;

        internal EnclaveArchetypeProfile(
            EnclaveArchetype archetype,
            string displayName,
            string description,
            string benefitSummary,
            int recruitmentPriceAdjustmentPercent,
            int tradeFavorableBonusPercent,
            int generalSupplyCapacityBonusPercent,
            int supplyRequestCooldownTicks,
            float friendlyInterventionChance,
            float hostileInterventionChance,
            int interventionPartyStrengthBonus,
            int gatheringSeatBonus,
            int storageStackBonus,
            int internalSpacingAdjustment,
            string organizationSuffix,
            IEnumerable<EnclaveArchetypeTraderStockEntry> traderStock,
            IEnumerable<KeyValuePair<EnclaveNeedType, int>> demandBonuses,
            IEnumerable<KeyValuePair<EnclaveNeedType, int>> supplyBonuses,
            IEnumerable<KeyValuePair<EnclaveNeedType, int>> priorities,
            IEnumerable<KeyValuePair<string, int>> storageWeightBonuses,
            IEnumerable<KeyValuePair<string, int>> storageQuantityBonuses
        )
        {
            Archetype = archetype;
            DisplayName = displayName;
            Description = description;
            BenefitSummary = benefitSummary;
            RecruitmentPriceAdjustmentPercent =
                recruitmentPriceAdjustmentPercent;
            TradeFavorableBonusPercent = tradeFavorableBonusPercent;
            GeneralSupplyCapacityBonusPercent =
                generalSupplyCapacityBonusPercent;
            SupplyRequestCooldownTicks = supplyRequestCooldownTicks;
            FriendlyInterventionChance = friendlyInterventionChance;
            HostileInterventionChance = hostileInterventionChance;
            InterventionPartyStrengthBonus =
                interventionPartyStrengthBonus;
            GatheringSeatBonus = gatheringSeatBonus;
            StorageStackBonus = storageStackBonus;
            InternalSpacingAdjustment = internalSpacingAdjustment;
            OrganizationSuffix = organizationSuffix ?? string.Empty;
            initialTraderStock = new List<
                EnclaveArchetypeTraderStockEntry
            >(traderStock ?? Array.Empty<
                EnclaveArchetypeTraderStockEntry
            >());
            needDemandBonusPercent = ToDictionary(demandBonuses);
            needSupplyBonusPercent = ToDictionary(supplyBonuses);
            supplyRequestPriority = ToDictionary(priorities);
            visualStorageWeightBonus = ToDictionary(
                storageWeightBonuses
            );
            visualStorageQuantityBonusPercent = ToDictionary(
                storageQuantityBonuses
            );
        }

        public int GetNeedDemandBonusPercent(
            EnclaveNeedType needType
        )
        {
            return GetValue(needDemandBonusPercent, needType);
        }

        public int GetNeedDemandPercent(EnclaveNeedType needType)
        {
            return 100 + GetNeedDemandBonusPercent(needType);
        }

        public int GetNeedSupplyCapacityBonusPercent(
            EnclaveNeedType needType
        )
        {
            return
                GeneralSupplyCapacityBonusPercent +
                GetValue(needSupplyBonusPercent, needType);
        }

        public int GetSupplyRequestPriority(
            EnclaveNeedType needType
        )
        {
            return GetValue(supplyRequestPriority, needType);
        }

        public float GetInterventionChance(
            EnclaveInterventionSide side
        )
        {
            switch (side)
            {
                case EnclaveInterventionSide.Friendly:
                    return FriendlyInterventionChance;
                case EnclaveInterventionSide.Hostile:
                    return HostileInterventionChance;
                default:
                    return 0f;
            }
        }

        public int GetVisualStorageWeightBonus(string thingDefName)
        {
            return GetValue(visualStorageWeightBonus, thingDefName);
        }

        public int GetVisualStorageQuantityBonusPercent(
            string thingDefName
        )
        {
            return GetValue(
                visualStorageQuantityBonusPercent,
                thingDefName
            );
        }

        private static Dictionary<TKey, int> ToDictionary<TKey>(
            IEnumerable<KeyValuePair<TKey, int>> values
        )
        {
            Dictionary<TKey, int> result =
                new Dictionary<TKey, int>();

            if (values == null)
            {
                return result;
            }

            foreach (KeyValuePair<TKey, int> value in values)
            {
                result[value.Key] = value.Value;
            }

            return result;
        }

        private static int GetValue<TKey>(
            Dictionary<TKey, int> values,
            TKey key
        )
        {
            int value;
            return values.TryGetValue(key, out value) ? value : 0;
        }
    }

    public static class EnclaveArchetypeUtility
    {
        public const int DefaultSupplyRequestCooldownTicks = 1800000;
        public const int TradeCompactSupplyRequestCooldownTicks = 1500000;
        public const int MaximumTradeFavorableBonusPercent = 20;
        public const int MinimumRecruitmentPricePercent = 40;
        public const int MaximumRecruitmentPricePercent = 150;
        public const int MaximumInterventionPartySize = 7;

        private static readonly Dictionary<
            EnclaveArchetype,
            EnclaveArchetypeProfile
        > profiles = CreateProfiles();

        public static EnclaveArchetype GetArchetype(EnclaveData data)
        {
            return IsValid(data?.Archetype ?? EnclaveArchetype.Unassigned)
                ? data.Archetype
                : EnclaveArchetype.Unassigned;
        }

        public static EnclaveArchetypeProfile GetProfile(
            EnclaveData data
        )
        {
            if (data != null && !IsValid(data.Archetype))
            {
                EnsureArchetype(
                    data,
                    -1,
                    "archetype profile resolution"
                );
            }

            EnclaveArchetype archetype = GetArchetype(data);
            EnclaveArchetypeProfile profile;

            return profiles.TryGetValue(archetype, out profile)
                ? profile
                : profiles[EnclaveArchetype.Hearthbound];
        }

        public static EnclaveArchetypeProfile GetProfileFor(
            EnclaveArchetype archetype
        )
        {
            EnclaveArchetypeProfile profile;

            return profiles.TryGetValue(archetype, out profile)
                ? profile
                : profiles[EnclaveArchetype.Hearthbound];
        }

        public static string GetDisplayName(EnclaveData data)
        {
            return GetProfile(data).DisplayName;
        }

        public static string GetDescription(EnclaveData data)
        {
            return GetProfile(data).Description;
        }

        public static string GetBenefitSummary(EnclaveData data)
        {
            return GetProfile(data).BenefitSummary;
        }

        public static bool EnsureArchetype(
            EnclaveData data,
            int worldObjectId = -1,
            string reason = null
        )
        {
            if (data == null || IsValid(data.Archetype))
            {
                return false;
            }

            EnclaveIdeologyType ideologyType =
                EnclaveIdeologyUtility.GetIdeologyType(data);
            uint seed = CreateStableSeed(
                data,
                worldObjectId,
                ideologyType
            );
            int roll = (int)(seed % 100u);

            data.Archetype = SelectWeighted(ideologyType, roll);

            Log.Message(
                "[IEE] Assigned persistent archetype for " +
                (data.Name ?? "an enclave") +
                ": " +
                GetDisplayName(data) +
                " from " +
                ideologyType +
                " weighting (stable roll " +
                roll +
                ")" +
                (reason.NullOrEmpty()
                    ? "."
                    : " (" + reason + ").")
            );

            return true;
        }

        public static bool SetArchetype(
            EnclaveData data,
            EnclaveArchetype archetype,
            string reason = null
        )
        {
            if (data == null || !IsValid(archetype))
            {
                return false;
            }

            EnclaveArchetype previous = GetArchetype(data);

            if (previous == archetype)
            {
                return true;
            }

            data.Archetype = archetype;

            Log.Message(
                "[IEE] Enclave archetype for " +
                (data.Name ?? "an enclave") +
                " changed from " +
                previous +
                " to " +
                archetype +
                (reason.NullOrEmpty()
                    ? "."
                    : " (" + reason + ").")
            );

            return true;
        }

        private static Dictionary<EnclaveArchetype, EnclaveArchetypeProfile>
            CreateProfiles()
        {
            return new Dictionary<
                EnclaveArchetype,
                EnclaveArchetypeProfile
            >
            {
                {
                    EnclaveArchetype.Hearthbound,
                    new EnclaveArchetypeProfile(
                        EnclaveArchetype.Hearthbound,
                        "Hearthbound Enclave",
                        "A community-oriented enclave focused on mutual " +
                            "support and settlement stability.",
                        "Recruitment-friendly; community-focused supplies; " +
                            "more likely to aid nearby colonies.",
                        -10,
                        0,
                        0,
                        DefaultSupplyRequestCooldownTicks,
                        0.05f,
                        -0.03f,
                        0,
                        1,
                        0,
                        0,
                        ", communal",
                        new[]
                        {
                            new EnclaveArchetypeTraderStockEntry(
                                "MealSurvivalPack",
                                2
                            ),
                            new EnclaveArchetypeTraderStockEntry(
                                "MedicineHerbal",
                                2
                            ),
                            new EnclaveArchetypeTraderStockEntry(
                                "Cloth",
                                12
                            )
                        },
                        NeedPairs(
                            EnclaveNeedType.Food, 10,
                            EnclaveNeedType.Textiles, 5
                        ),
                        NeedPairs(EnclaveNeedType.Medicine, 5),
                        NeedPairs(
                            EnclaveNeedType.Food, 30,
                            EnclaveNeedType.Medicine, 20,
                            EnclaveNeedType.Textiles, 10
                        ),
                        StringPairs(
                            "MealSurvivalPack", 35,
                            "Cloth", 30
                        ),
                        StringPairs(
                            "MealSurvivalPack", 10,
                            "Cloth", 10
                        )
                    )
                },
                {
                    EnclaveArchetype.TradeCompact,
                    new EnclaveArchetypeProfile(
                        EnclaveArchetype.TradeCompact,
                        "Trade Compact",
                        "A commercially minded enclave built around trade " +
                            "and material exchange.",
                        "Better trading; broader commercial stock; more " +
                            "frequent supply requests.",
                        0,
                        5,
                        5,
                        TradeCompactSupplyRequestCooldownTicks,
                        0.02f,
                        0f,
                        0,
                        0,
                        1,
                        0,
                        ", commercially dense",
                        new[]
                        {
                            new EnclaveArchetypeTraderStockEntry(
                                "Steel",
                                15
                            ),
                            new EnclaveArchetypeTraderStockEntry(
                                "ComponentIndustrial",
                                1
                            ),
                            new EnclaveArchetypeTraderStockEntry(
                                "MedicineIndustrial",
                                1
                            ),
                            new EnclaveArchetypeTraderStockEntry(
                                "Cloth",
                                10
                            )
                        },
                        NeedPairs(
                            EnclaveNeedType.Components, 10,
                            EnclaveNeedType.Textiles, 5
                        ),
                        NeedPairs(),
                        NeedPairs(
                            EnclaveNeedType.Components, 30,
                            EnclaveNeedType.Textiles, 20,
                            EnclaveNeedType.BuildingMaterials, 10
                        ),
                        StringPairs(
                            "Steel", 35,
                            "ComponentIndustrial", 45,
                            "Cloth", 25
                        ),
                        StringPairs(
                            "Steel", 10,
                            "ComponentIndustrial", 10,
                            "Cloth", 10
                        )
                    )
                },
                {
                    EnclaveArchetype.WarriorCovenant,
                    new EnclaveArchetypeProfile(
                        EnclaveArchetype.WarriorCovenant,
                        "Warrior Covenant",
                        "A disciplined enclave that emphasizes defense and " +
                            "martial strength.",
                        "Stronger intervention parties; martial stock " +
                            "focus; more expensive recruitment.",
                        10,
                        0,
                        0,
                        DefaultSupplyRequestCooldownTicks,
                        0.04f,
                        0.05f,
                        1,
                        0,
                        0,
                        -1,
                        ", disciplined ranks",
                        new[]
                        {
                            new EnclaveArchetypeTraderStockEntry(
                                "Steel",
                                12
                            ),
                            new EnclaveArchetypeTraderStockEntry(
                                "MedicineIndustrial",
                                2
                            ),
                            new EnclaveArchetypeTraderStockEntry(
                                "ComponentIndustrial",
                                1
                            ),
                            new EnclaveArchetypeTraderStockEntry(
                                "MeleeWeapon_Knife",
                                1,
                                "Steel"
                            )
                        },
                        NeedPairs(
                            EnclaveNeedType.Medicine, 10,
                            EnclaveNeedType.Components, 10,
                            EnclaveNeedType.Food, 5
                        ),
                        NeedPairs(),
                        NeedPairs(
                            EnclaveNeedType.Medicine, 30,
                            EnclaveNeedType.Components, 20,
                            EnclaveNeedType.BuildingMaterials, 10
                        ),
                        StringPairs(
                            "Steel", 50,
                            "ComponentIndustrial", 30
                        ),
                        StringPairs("Steel", 15)
                    )
                }
            };
        }

        private static EnclaveArchetype SelectWeighted(
            EnclaveIdeologyType ideologyType,
            int roll
        )
        {
            int hearthbound;
            int tradeCompact;

            switch (ideologyType)
            {
                case EnclaveIdeologyType.Communal:
                    hearthbound = 60;
                    tradeCompact = 30;
                    break;
                case EnclaveIdeologyType.Isolationist:
                    hearthbound = 15;
                    tradeCompact = 15;
                    break;
                case EnclaveIdeologyType.Martial:
                    hearthbound = 10;
                    tradeCompact = 15;
                    break;
                case EnclaveIdeologyType.Mercantile:
                    hearthbound = 15;
                    tradeCompact = 75;
                    break;
                case EnclaveIdeologyType.Nature:
                    hearthbound = 70;
                    tradeCompact = 20;
                    break;
                case EnclaveIdeologyType.Spiritual:
                    hearthbound = 65;
                    tradeCompact = 15;
                    break;
                case EnclaveIdeologyType.Transhumanist:
                    hearthbound = 15;
                    tradeCompact = 50;
                    break;
                default:
                    hearthbound = 34;
                    tradeCompact = 33;
                    break;
            }

            if (roll < hearthbound)
            {
                return EnclaveArchetype.Hearthbound;
            }

            if (roll < hearthbound + tradeCompact)
            {
                return EnclaveArchetype.TradeCompact;
            }

            return EnclaveArchetype.WarriorCovenant;
        }

        private static uint CreateStableSeed(
            EnclaveData data,
            int worldObjectId,
            EnclaveIdeologyType ideologyType
        )
        {
            unchecked
            {
                uint hash = 2166136261u;

                AddStringToHash(ref hash, data?.Name);
                AddStringToHash(ref hash, data?.Leader);
                AddIntToHash(ref hash, worldObjectId);
                AddIntToHash(ref hash, (int)ideologyType);
                AddIntToHash(
                    ref hash,
                    (int)(data?.GatheringPosition ??
                        EnclaveLayoutPosition.Unassigned)
                );
                AddIntToHash(
                    ref hash,
                    (int)(data?.SleepingPosition ??
                        EnclaveLayoutPosition.Unassigned)
                );
                AddIntToHash(
                    ref hash,
                    (int)(data?.StoragePosition ??
                        EnclaveLayoutPosition.Unassigned)
                );
                AddIntToHash(
                    ref hash,
                    (int)(data?.RitualPosition ??
                        EnclaveLayoutPosition.Unassigned)
                );

                hash ^= hash >> 13;
                hash *= 1274126177u;
                hash ^= hash >> 16;
                return hash;
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
                hash = (hash ^ character) * 16777619u;
            }
        }

        private static void AddIntToHash(ref uint hash, int value)
        {
            unchecked
            {
                hash = (hash ^ (uint)value) * 16777619u;
            }
        }

        private static bool IsValid(EnclaveArchetype archetype)
        {
            return
                archetype >= EnclaveArchetype.Hearthbound &&
                archetype <= EnclaveArchetype.WarriorCovenant &&
                Enum.IsDefined(typeof(EnclaveArchetype), archetype);
        }

        private static KeyValuePair<EnclaveNeedType, int>[] NeedPairs(
            params object[] values
        )
        {
            List<KeyValuePair<EnclaveNeedType, int>> result =
                new List<KeyValuePair<EnclaveNeedType, int>>();

            for (int index = 0; index + 1 < values.Length; index += 2)
            {
                result.Add(
                    new KeyValuePair<EnclaveNeedType, int>(
                        (EnclaveNeedType)values[index],
                        (int)values[index + 1]
                    )
                );
            }

            return result.ToArray();
        }

        private static KeyValuePair<string, int>[] StringPairs(
            params object[] values
        )
        {
            List<KeyValuePair<string, int>> result =
                new List<KeyValuePair<string, int>>();

            for (int index = 0; index + 1 < values.Length; index += 2)
            {
                result.Add(
                    new KeyValuePair<string, int>(
                        (string)values[index],
                        (int)values[index + 1]
                    )
                );
            }

            return result.ToArray();
        }
    }
}
