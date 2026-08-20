using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public sealed class EnclaveDevelopmentVisualProfile
    {
        public EnclaveDevelopmentTier Tier { get; internal set; }
        public EnclaveIdeologyType IdeologyType { get; internal set; }
        public string DensityLabel { get; internal set; }
        public string OrganizationLabel { get; internal set; }
        public int AreaScalePercent { get; internal set; }
        public int InternalSpacing { get; internal set; }

        public int GatheringWidth { get; internal set; }
        public int GatheringHeight { get; internal set; }
        public ThingDef GatheringTableDef { get; internal set; }
        public int GatheringSeatCount { get; internal set; }
        public int GatheringLightCount { get; internal set; }

        public int SleepingWidth { get; internal set; }
        public int SleepingHeight { get; internal set; }
        public ThingDef SleepingDef { get; internal set; }
        public int SleepingColumns { get; internal set; }
        public int SleepingSpacing { get; internal set; }
        public bool StaggerSleepingRows { get; internal set; }
        public int SleepingLightCount { get; internal set; }

        public int StorageWidth { get; internal set; }
        public int StorageHeight { get; internal set; }
        public int StorageStackCount { get; internal set; }

        public int RitualWidth { get; internal set; }
        public int RitualHeight { get; internal set; }
        public int RitualSeatCount { get; internal set; }
        public int RitualLightCount { get; internal set; }

        public ThingDef SeatingDef =>
            Tier >= EnclaveDevelopmentTier.TierIII
                ? ThingDefOf.DiningChair
                : ThingDefOf.Stool;

        public string DiagnosticSummary =>
            DensityLabel +
            "; area scale " +
            AreaScalePercent +
            "%; spacing " +
            InternalSpacing +
            "; gathering " +
            GatheringTableDef.defName +
            "/" +
            GatheringSeatCount +
            " seats/" +
            GatheringLightCount +
            " lights; sleeping " +
            SleepingDef.defName +
            "/" +
            SleepingSpacing +
            " spacing/" +
            SleepingLightCount +
            " lights; storage " +
            StorageStackCount +
            " stacks; ritual " +
            RitualSeatCount +
            " seats/" +
            RitualLightCount +
            " total lights";
    }

    public sealed class EnclaveVisualStorageStack
    {
        public ThingDef ThingDef { get; }
        public int StackCount { get; }

        public EnclaveVisualStorageStack(
            ThingDef thingDef,
            int stackCount
        )
        {
            ThingDef = thingDef;
            StackCount = stackCount;
        }
    }

    public static class EnclaveDevelopmentVisualUtility
    {
        private sealed class StorageResourceRule
        {
            public ThingDef ThingDef { get; }
            public EnclaveDevelopmentTier MinimumTier { get; }
            public int Weight { get; }
            private readonly int[] quantities;

            public StorageResourceRule(
                ThingDef thingDef,
                EnclaveDevelopmentTier minimumTier,
                int weight,
                int tierI,
                int tierII,
                int tierIII,
                int tierIV
            )
            {
                ThingDef = thingDef;
                MinimumTier = minimumTier;
                Weight = weight;
                quantities = new[]
                {
                    0,
                    tierI,
                    tierII,
                    tierIII,
                    tierIV
                };
            }

            public int GetQuantity(EnclaveDevelopmentTier tier)
            {
                int index = Math.Max(1, Math.Min(4, (int)tier));
                return quantities[index];
            }
        }

        private static readonly StorageResourceRule[] storageRules =
        {
            new StorageResourceRule(
                ThingDefOf.WoodLog,
                EnclaveDevelopmentTier.TierI,
                100,
                35,
                75,
                90,
                110
            ),
            new StorageResourceRule(
                ThingDefOf.Steel,
                EnclaveDevelopmentTier.TierI,
                65,
                20,
                50,
                65,
                80
            ),
            new StorageResourceRule(
                ThingDefOf.MealSurvivalPack,
                EnclaveDevelopmentTier.TierI,
                55,
                5,
                10,
                14,
                18
            ),
            new StorageResourceRule(
                ThingDefOf.Cloth,
                EnclaveDevelopmentTier.TierII,
                45,
                0,
                25,
                35,
                50
            ),
            new StorageResourceRule(
                ThingDefOf.MedicineHerbal,
                EnclaveDevelopmentTier.TierIII,
                30,
                0,
                0,
                5,
                7
            ),
            new StorageResourceRule(
                ThingDefOf.ComponentIndustrial,
                EnclaveDevelopmentTier.TierIII,
                20,
                0,
                0,
                2,
                4
            ),
            new StorageResourceRule(
                ThingDefOf.MedicineIndustrial,
                EnclaveDevelopmentTier.TierIV,
                12,
                0,
                0,
                0,
                3
            )
        };

        public static EnclaveDevelopmentVisualProfile GetProfile(
            EnclaveData data
        )
        {
            EnclaveDevelopmentTier tier =
                EnclaveDevelopmentUtility.GetTier(data);

            if (tier == EnclaveDevelopmentTier.Unassigned)
            {
                tier = EnclaveDevelopmentTier.TierII;
            }

            EnclaveDevelopmentVisualProfile profile =
                CreateTierProfile(tier);

            profile.IdeologyType =
                EnclaveIdeologyUtility.GetIdeologyType(data);

            ApplyIdeologyModifiers(profile);
            return profile;
        }

        public static ThingDef GetFurnitureStuff(
            EnclaveDevelopmentVisualProfile profile,
            ThingDef furnitureDef
        )
        {
            if (furnitureDef == null || !furnitureDef.MadeFromStuff)
            {
                return null;
            }

            if (furnitureDef == ThingDefOf.Bedroll)
            {
                return ThingDefOf.Cloth;
            }

            if (
                profile.Tier >= EnclaveDevelopmentTier.TierIII &&
                (
                    profile.IdeologyType ==
                        EnclaveIdeologyType.Martial ||
                    profile.IdeologyType ==
                        EnclaveIdeologyType.Transhumanist
                )
            )
            {
                return ThingDefOf.Steel;
            }

            return ThingDefOf.WoodLog;
        }

        public static List<EnclaveVisualStorageStack>
            CreateStorageStacks(
                EnclaveDevelopmentVisualProfile profile,
                Random random
            )
        {
            List<StorageResourceRule> selected =
                new List<StorageResourceRule>();
            List<StorageResourceRule> available =
                new List<StorageResourceRule>();

            foreach (StorageResourceRule rule in storageRules)
            {
                if (rule.MinimumTier <= profile.Tier)
                {
                    available.Add(rule);
                }
            }

            AddRule(selected, ThingDefOf.WoodLog);

            if (profile.Tier >= EnclaveDevelopmentTier.TierII)
            {
                AddRule(selected, ThingDefOf.Steel);
                AddRule(selected, ThingDefOf.MealSurvivalPack);
            }

            int targetCount = Math.Min(
                profile.StorageStackCount,
                available.Count
            );

            while (selected.Count < targetCount)
            {
                StorageResourceRule selectedRule =
                    SelectWeightedRule(
                        available,
                        selected,
                        profile.IdeologyType,
                        random
                    );

                if (selectedRule == null)
                {
                    break;
                }

                selected.Add(selectedRule);
            }

            List<EnclaveVisualStorageStack> result =
                new List<EnclaveVisualStorageStack>();

            foreach (StorageResourceRule rule in selected)
            {
                int quantity = GetAdjustedQuantity(
                    rule,
                    profile,
                    random
                );

                if (quantity > 0)
                {
                    result.Add(
                        new EnclaveVisualStorageStack(
                            rule.ThingDef,
                            Math.Min(
                                quantity,
                                rule.ThingDef.stackLimit
                            )
                        )
                    );
                }
            }

            return result;
        }

        private static EnclaveDevelopmentVisualProfile CreateTierProfile(
            EnclaveDevelopmentTier tier
        )
        {
            switch (tier)
            {
                case EnclaveDevelopmentTier.TierI:
                    return new EnclaveDevelopmentVisualProfile
                    {
                        Tier = tier,
                        DensityLabel = "Sparse and improvised",
                        OrganizationLabel = "Loose",
                        AreaScalePercent = 80,
                        InternalSpacing = 2,
                        GatheringWidth = 8,
                        GatheringHeight = 8,
                        GatheringTableDef = ThingDefOf.Table1x2c,
                        GatheringSeatCount = 2,
                        GatheringLightCount = 0,
                        SleepingWidth = 10,
                        SleepingHeight = 10,
                        SleepingDef = ThingDefOf.Bedroll,
                        SleepingColumns = 3,
                        SleepingSpacing = 2,
                        StaggerSleepingRows = true,
                        SleepingLightCount = 0,
                        StorageWidth = 6,
                        StorageHeight = 6,
                        StorageStackCount = 2,
                        RitualWidth = 8,
                        RitualHeight = 8,
                        RitualSeatCount = 2,
                        RitualLightCount = 1
                    };
                case EnclaveDevelopmentTier.TierIII:
                    return new EnclaveDevelopmentVisualProfile
                    {
                        Tier = tier,
                        DensityLabel = "Prosperous and developed",
                        OrganizationLabel = "Deliberate",
                        AreaScalePercent = 120,
                        InternalSpacing = 3,
                        GatheringWidth = 12,
                        GatheringHeight = 10,
                        GatheringTableDef = ThingDefOf.Table2x2c,
                        GatheringSeatCount = 6,
                        GatheringLightCount = 2,
                        SleepingWidth = 14,
                        SleepingHeight = 12,
                        SleepingDef = ThingDefOf.Bed,
                        SleepingColumns = 4,
                        SleepingSpacing = 3,
                        StaggerSleepingRows = false,
                        SleepingLightCount = 1,
                        StorageWidth = 10,
                        StorageHeight = 10,
                        StorageStackCount = 5,
                        RitualWidth = 12,
                        RitualHeight = 10,
                        RitualSeatCount = 6,
                        RitualLightCount = 3
                    };
                case EnclaveDevelopmentTier.TierIV:
                    return new EnclaveDevelopmentVisualProfile
                    {
                        Tier = tier,
                        DensityLabel = "Substantial and prosperous",
                        OrganizationLabel = "Highly organized",
                        AreaScalePercent = 140,
                        InternalSpacing = 3,
                        GatheringWidth = 14,
                        GatheringHeight = 12,
                        GatheringTableDef = ThingDefOf.Table2x4c,
                        GatheringSeatCount = 10,
                        GatheringLightCount = 4,
                        SleepingWidth = 14,
                        SleepingHeight = 14,
                        SleepingDef = ThingDefOf.Bed,
                        SleepingColumns = 4,
                        SleepingSpacing = 3,
                        StaggerSleepingRows = false,
                        SleepingLightCount = 2,
                        StorageWidth = 12,
                        StorageHeight = 12,
                        StorageStackCount = 7,
                        RitualWidth = 14,
                        RitualHeight = 12,
                        RitualSeatCount = 10,
                        RitualLightCount = 5
                    };
                default:
                    return new EnclaveDevelopmentVisualProfile
                    {
                        Tier = EnclaveDevelopmentTier.TierII,
                        DensityLabel = "Functional and modest",
                        OrganizationLabel = "Established",
                        AreaScalePercent = 100,
                        InternalSpacing = 2,
                        GatheringWidth = 10,
                        GatheringHeight = 8,
                        GatheringTableDef = ThingDefOf.Table1x2c,
                        GatheringSeatCount = 4,
                        GatheringLightCount = 0,
                        SleepingWidth = 12,
                        SleepingHeight = 10,
                        SleepingDef = ThingDefOf.Bed,
                        SleepingColumns = 4,
                        SleepingSpacing = 2,
                        StaggerSleepingRows = false,
                        SleepingLightCount = 0,
                        StorageWidth = 8,
                        StorageHeight = 8,
                        StorageStackCount = 3,
                        RitualWidth = 10,
                        RitualHeight = 8,
                        RitualSeatCount = 4,
                        RitualLightCount = 1
                    };
            }
        }

        private static void ApplyIdeologyModifiers(
            EnclaveDevelopmentVisualProfile profile
        )
        {
            switch (profile.IdeologyType)
            {
                case EnclaveIdeologyType.Communal:
                    profile.GatheringSeatCount += 2;

                    if (profile.Tier >= EnclaveDevelopmentTier.TierIII)
                    {
                        profile.GatheringLightCount++;
                    }
                    break;
                case EnclaveIdeologyType.Isolationist:
                    profile.GatheringSeatCount = Math.Max(
                        2,
                        profile.GatheringSeatCount - 1
                    );
                    profile.SleepingSpacing = Math.Min(
                        4,
                        profile.SleepingSpacing + 1
                    );
                    profile.OrganizationLabel += ", separated";
                    break;
                case EnclaveIdeologyType.Martial:
                    profile.OrganizationLabel += ", austere symmetry";
                    break;
                case EnclaveIdeologyType.Mercantile:
                    profile.StorageStackCount++;
                    break;
                case EnclaveIdeologyType.Nature:
                    profile.GatheringLightCount = Math.Max(
                        0,
                        profile.GatheringLightCount - 1
                    );
                    profile.SleepingLightCount = Math.Max(
                        0,
                        profile.SleepingLightCount - 1
                    );
                    profile.RitualLightCount = Math.Max(
                        1,
                        profile.RitualLightCount - 1
                    );
                    break;
                case EnclaveIdeologyType.Spiritual:
                    profile.RitualSeatCount += 2;
                    profile.RitualLightCount++;
                    break;
                case EnclaveIdeologyType.Transhumanist:
                    if (profile.Tier >= EnclaveDevelopmentTier.TierIII)
                    {
                        profile.GatheringLightCount++;
                        profile.SleepingLightCount++;
                    }
                    break;
            }
        }

        private static void AddRule(
            List<StorageResourceRule> selected,
            ThingDef thingDef
        )
        {
            foreach (StorageResourceRule rule in storageRules)
            {
                if (rule.ThingDef == thingDef)
                {
                    selected.Add(rule);
                    return;
                }
            }
        }

        private static StorageResourceRule SelectWeightedRule(
            List<StorageResourceRule> available,
            List<StorageResourceRule> selected,
            EnclaveIdeologyType ideologyType,
            Random random
        )
        {
            int totalWeight = 0;

            foreach (StorageResourceRule rule in available)
            {
                if (!selected.Contains(rule))
                {
                    totalWeight += GetWeight(rule, ideologyType);
                }
            }

            if (totalWeight <= 0)
            {
                return null;
            }

            int roll = random.Next(totalWeight);

            foreach (StorageResourceRule rule in available)
            {
                if (selected.Contains(rule))
                {
                    continue;
                }

                roll -= GetWeight(rule, ideologyType);

                if (roll < 0)
                {
                    return rule;
                }
            }

            return null;
        }

        private static int GetWeight(
            StorageResourceRule rule,
            EnclaveIdeologyType ideologyType
        )
        {
            int weight = rule.Weight;

            if (ideologyType == EnclaveIdeologyType.Nature)
            {
                if (
                    rule.ThingDef == ThingDefOf.Cloth ||
                    rule.ThingDef == ThingDefOf.MedicineHerbal
                )
                {
                    weight += 35;
                }
                else if (
                    rule.ThingDef == ThingDefOf.ComponentIndustrial ||
                    rule.ThingDef == ThingDefOf.MedicineIndustrial
                )
                {
                    weight = Math.Max(2, weight / 4);
                }
            }
            else if (ideologyType == EnclaveIdeologyType.Martial)
            {
                if (
                    rule.ThingDef == ThingDefOf.Steel ||
                    rule.ThingDef == ThingDefOf.ComponentIndustrial
                )
                {
                    weight += 30;
                }
            }
            else if (ideologyType == EnclaveIdeologyType.Mercantile)
            {
                if (rule.ThingDef == ThingDefOf.Cloth)
                {
                    weight += 40;
                }
            }
            else if (
                ideologyType == EnclaveIdeologyType.Transhumanist &&
                (
                    rule.ThingDef == ThingDefOf.ComponentIndustrial ||
                    rule.ThingDef == ThingDefOf.MedicineIndustrial
                )
            )
            {
                weight += 50;
            }

            return Math.Max(1, weight);
        }

        private static int GetAdjustedQuantity(
            StorageResourceRule rule,
            EnclaveDevelopmentVisualProfile profile,
            Random random
        )
        {
            int quantity = rule.GetQuantity(profile.Tier);
            int percent = 100;

            if (
                profile.IdeologyType == EnclaveIdeologyType.Nature &&
                rule.ThingDef == ThingDefOf.WoodLog
            )
            {
                percent += 15;
            }
            else if (
                profile.IdeologyType == EnclaveIdeologyType.Martial &&
                rule.ThingDef == ThingDefOf.Steel
            )
            {
                percent += 15;
            }
            else if (
                profile.IdeologyType == EnclaveIdeologyType.Mercantile
            )
            {
                percent += 10;
            }
            else if (
                profile.IdeologyType ==
                    EnclaveIdeologyType.Transhumanist &&
                rule.ThingDef == ThingDefOf.ComponentIndustrial
            )
            {
                percent += 25;
            }

            if (profile.Tier != EnclaveDevelopmentTier.TierII)
            {
                percent = percent * random.Next(95, 106) / 100;
            }

            return Math.Max(1, quantity * percent / 100);
        }
    }
}
