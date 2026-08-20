using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveArchetypeRegressionTest
    {
        private const string TestReason =
            "developer archetype regression test";

        private sealed class TestResult
        {
            public string Section;
            public string Label;
            public string Expected;
            public string Actual;
            public bool Passed;
        }

        private sealed class NeedSnapshot
        {
            public EnclaveNeedRecord Record;
            public EnclaveNeedType Type;
            public EnclaveNeedSeverity Severity;
            public int TargetAmount;
            public int EstimatedSupply;
            public int LastEvaluationTick;

            public bool Matches(EnclaveNeedRecord record)
            {
                return
                    object.ReferenceEquals(Record, record) &&
                    record != null &&
                    record.Type == Type &&
                    record.Severity == Severity &&
                    record.TargetAmount == TargetAmount &&
                    record.EstimatedSupply == EstimatedSupply &&
                    record.LastEvaluationTick == LastEvaluationTick;
            }
        }

        private sealed class ThingSnapshot
        {
            public Thing Thing;
            public int StackCount;

            public bool Matches(Thing thing)
            {
                return
                    object.ReferenceEquals(Thing, thing) &&
                    thing != null &&
                    thing.stackCount == StackCount;
            }
        }

        private sealed class CampSnapshot
        {
            public EnclaveArchetype Archetype;
            public int Reputation;
            public EnclaveDevelopmentTier DevelopmentTier;
            public EnclaveIdeologyProfile IdeologyProfile;
            public EnclaveIdeologyType IdeologyType;
            public Ideo ActualIdeo;
            public IReadOnlyList<EnclaveNeedRecord> Needs;
            public readonly List<NeedSnapshot> NeedRecords =
                new List<NeedSnapshot>();
            public EnclaveQuestRequest ActiveRequest;
            public int ActiveRequestId;
            public Pawn Trader;
            public object TraderInventory;
            public object TraderInnerContainer;
            public readonly List<ThingSnapshot> TraderThings =
                new List<ThingSnapshot>();
            public bool HadMap;
            public Map Map;

            public static CampSnapshot Capture(PilgrimCamp camp)
            {
                EnclaveData data = camp.Data;
                CampSnapshot snapshot = new CampSnapshot
                {
                    Archetype = data.Archetype,
                    Reputation = data.Reputation,
                    DevelopmentTier = data.DevelopmentTier,
                    IdeologyProfile = data.IdeologyProfile,
                    IdeologyType =
                        EnclaveIdeologyUtility.GetIdeologyType(data),
                    ActualIdeo =
                        EnclaveIdeologyUtility.GetActualIdeo(data),
                    Needs = data.Needs,
                    ActiveRequest = data.ActiveQuestRequest,
                    ActiveRequestId =
                        data.ActiveQuestRequest?.RequestId ?? -1,
                    Trader = camp.PawnRoles?.GetPawn(
                        EnclavePawnRole.Trader
                    ),
                    HadMap = camp.HasMap,
                    Map = camp.HasMap ? camp.Map : null
                };

                if (snapshot.Needs != null)
                {
                    foreach (EnclaveNeedRecord need in snapshot.Needs)
                    {
                        snapshot.NeedRecords.Add(
                            new NeedSnapshot
                            {
                                Record = need,
                                Type = need.Type,
                                Severity = need.Severity,
                                TargetAmount = need.TargetAmount,
                                EstimatedSupply = need.EstimatedSupply,
                                LastEvaluationTick =
                                    need.LastEvaluationTick
                            }
                        );
                    }
                }

                snapshot.TraderInventory = snapshot.Trader?.inventory;
                snapshot.TraderInnerContainer =
                    snapshot.Trader?.inventory?.innerContainer;

                if (snapshot.Trader?.inventory?.innerContainer != null)
                {
                    foreach (
                        Thing thing in
                        snapshot.Trader.inventory.innerContainer
                    )
                    {
                        snapshot.TraderThings.Add(
                            new ThingSnapshot
                            {
                                Thing = thing,
                                StackCount = thing.stackCount
                            }
                        );
                    }
                }

                return snapshot;
            }

            public bool RestoreScalars(EnclaveData data)
            {
                bool ideologyRestored =
                    EnclaveIdeologyUtility.SetIdeologyType(
                        data,
                        IdeologyType,
                        TestReason + " restore"
                    );
                bool tierRestored = EnclaveDevelopmentUtility.SetTier(
                    data,
                    DevelopmentTier,
                    TestReason + " restore"
                );
                bool reputationRestored = data.SetReputation(
                    Reputation,
                    TestReason + " restore"
                ) == Reputation;
                bool archetypeRestored =
                    EnclaveArchetypeUtility.SetArchetype(
                        data,
                        Archetype,
                        TestReason + " restore"
                    );

                return
                    ideologyRestored &&
                    tierRestored &&
                    reputationRestored &&
                    archetypeRestored;
            }

            public bool NeedsMatch(EnclaveData data)
            {
                IReadOnlyList<EnclaveNeedRecord> current = data.Needs;

                if (
                    !object.ReferenceEquals(Needs, current) ||
                    current == null ||
                    current.Count != NeedRecords.Count
                )
                {
                    return Needs == null && current == null;
                }

                for (int index = 0; index < current.Count; index++)
                {
                    if (!NeedRecords[index].Matches(current[index]))
                    {
                        return false;
                    }
                }

                return true;
            }

            public bool RequestMatches(EnclaveData data)
            {
                return
                    object.ReferenceEquals(
                        ActiveRequest,
                        data.ActiveQuestRequest
                    ) &&
                    (data.ActiveQuestRequest?.RequestId ?? -1) ==
                        ActiveRequestId;
            }

            public bool TraderMatches(PilgrimCamp camp)
            {
                Pawn currentTrader = camp.PawnRoles?.GetPawn(
                    EnclavePawnRole.Trader
                );

                if (
                    !object.ReferenceEquals(Trader, currentTrader) ||
                    !object.ReferenceEquals(
                        TraderInventory,
                        currentTrader?.inventory
                    ) ||
                    !object.ReferenceEquals(
                        TraderInnerContainer,
                        currentTrader?.inventory?.innerContainer
                    )
                )
                {
                    return false;
                }

                if (currentTrader?.inventory?.innerContainer == null)
                {
                    return TraderThings.Count == 0;
                }

                if (
                    currentTrader.inventory.innerContainer.Count !=
                    TraderThings.Count
                )
                {
                    return false;
                }

                for (
                    int index = 0;
                    index < TraderThings.Count;
                    index++
                )
                {
                    if (
                        !TraderThings[index].Matches(
                            currentTrader.inventory.innerContainer[index]
                        )
                    )
                    {
                        return false;
                    }
                }

                return true;
            }

            public bool MapMatches(PilgrimCamp camp)
            {
                return
                    camp.HasMap == HadMap &&
                    object.ReferenceEquals(
                        Map,
                        camp.HasMap ? camp.Map : null
                    );
            }
        }

        public static void Run(PilgrimCamp camp)
        {
            if (!Prefs.DevMode || camp?.Data == null)
            {
                return;
            }

            EnclaveData data = camp.Data;
            CampSnapshot snapshot = CampSnapshot.Capture(camp);
            List<TestResult> results = new List<TestResult>();
            bool originalArchetypeValid = IsValidArchetype(
                snapshot.Archetype
            );
            bool validArchetypeStayedAssigned = true;
            bool archetypeChangesPreservedIdentity = true;
            bool restoreSucceeded = false;
            Exception executionException = null;

            try
            {
                if (!originalArchetypeValid)
                {
                    throw new InvalidOperationException(
                        "The selected enclave does not have a valid " +
                        "persistent archetype."
                    );
                }

                if (
                    snapshot.IdeologyProfile == null ||
                    snapshot.IdeologyType ==
                        EnclaveIdeologyType.Unassigned ||
                    snapshot.DevelopmentTier ==
                        EnclaveDevelopmentTier.Unassigned
                )
                {
                    throw new InvalidOperationException(
                        "The selected enclave does not have valid " +
                        "persistent ideology and development identities."
                    );
                }

                SetScenario(
                    data,
                    90,
                    EnclaveArchetype.Hearthbound
                );
                validArchetypeStayedAssigned &=
                    VerifyNoRederivation(camp);
                archetypeChangesPreservedIdentity &=
                    ArchetypeChangePreservedIdentity(snapshot, data);

                EnclaveArchetypeProfile hearthbound =
                    EnclaveArchetypeUtility.GetProfile(data);
                int hearthboundStorageStacks =
                    EnclaveDevelopmentVisualUtility
                        .GetProfile(data)
                        .StorageStackCount;

                AddResult(
                    results,
                    "Hearthbound",
                    "Recruitment: 300 silver; archetype -10%",
                    "300 silver and -10%",
                    EnclaveRecruitmentService
                        .GetEffectiveRecruitmentCost(camp, null) +
                        " silver and " +
                        FormatSignedPercent(
                            EnclaveRecruitmentService
                                .GetArchetypePriceAdjustmentPercent(camp)
                        ),
                    EnclaveRecruitmentService
                        .GetEffectiveRecruitmentCost(camp, null) == 300 &&
                    EnclaveRecruitmentService
                        .GetArchetypePriceAdjustmentPercent(camp) == -10
                );
                AddResult(
                    results,
                    "Hearthbound",
                    "Needs: Food +10%; Textiles +5%; Medicine supply +5%",
                    "+10%, +5%, +5%",
                    FormatSignedPercent(
                        hearthbound.GetNeedDemandBonusPercent(
                            EnclaveNeedType.Food
                        )
                    ) +
                        ", " +
                        FormatSignedPercent(
                            hearthbound.GetNeedDemandBonusPercent(
                                EnclaveNeedType.Textiles
                            )
                        ) +
                        ", " +
                        FormatSignedPercent(
                            hearthbound
                                .GetNeedSupplyCapacityBonusPercent(
                                    EnclaveNeedType.Medicine
                                )
                        ),
                    hearthbound.GetNeedDemandBonusPercent(
                        EnclaveNeedType.Food
                    ) == 10 &&
                    hearthbound.GetNeedDemandBonusPercent(
                        EnclaveNeedType.Textiles
                    ) == 5 &&
                    hearthbound.GetNeedSupplyCapacityBonusPercent(
                        EnclaveNeedType.Medicine
                    ) == 5
                );
                AddResult(
                    results,
                    "Hearthbound",
                    "Intervention: friendly +5%; hostile -3%",
                    "+5%, -3%",
                    FormatSignedPercent(
                        hearthbound.FriendlyInterventionChance
                    ) +
                        ", " +
                        FormatSignedPercent(
                            hearthbound.HostileInterventionChance
                        ),
                    NearlyEqual(
                        hearthbound.FriendlyInterventionChance,
                        0.05f
                    ) &&
                    NearlyEqual(
                        hearthbound.HostileInterventionChance,
                        -0.03f
                    )
                );
                AddResult(
                    results,
                    "Hearthbound",
                    "Supply Request cooldown: 30d",
                    "30d",
                    FormatDays(
                        EnclaveQuestService
                            .GetSupplyRequestCooldownTicks(data)
                    ),
                    EnclaveQuestService
                        .GetSupplyRequestCooldownTicks(data) ==
                        EnclaveArchetypeUtility
                            .DefaultSupplyRequestCooldownTicks
                );

                SetScenario(
                    data,
                    90,
                    EnclaveArchetype.TradeCompact
                );
                validArchetypeStayedAssigned &=
                    VerifyNoRederivation(camp);
                archetypeChangesPreservedIdentity &=
                    ArchetypeChangePreservedIdentity(snapshot, data);

                EnclaveArchetypeProfile tradeCompact =
                    EnclaveArchetypeUtility.GetProfile(data);
                EnclaveDevelopmentVisualProfile tradeVisual =
                    EnclaveDevelopmentVisualUtility.GetProfile(data);
                int componentPriority =
                    EnclaveNeedsUtility.GetSupplyRequestPriority(
                        data,
                        EnclaveNeedType.Components
                    );
                int textilePriority =
                    EnclaveNeedsUtility.GetSupplyRequestPriority(
                        data,
                        EnclaveNeedType.Textiles
                    );
                int buildingPriority =
                    EnclaveNeedsUtility.GetSupplyRequestPriority(
                        data,
                        EnclaveNeedType.BuildingMaterials
                    );

                AddResult(
                    results,
                    "Trade Compact",
                    "Recruitment: 350 silver",
                    "350 silver",
                    EnclaveRecruitmentService
                        .GetEffectiveRecruitmentCost(camp, null) +
                        " silver",
                    EnclaveRecruitmentService
                        .GetEffectiveRecruitmentCost(camp, null) == 350
                );
                AddResult(
                    results,
                    "Trade Compact",
                    "Final favorable trade modifier: 20%",
                    "20%",
                    EnclaveTradeService.GetTradeBonusPercent(camp) + "%",
                    EnclaveTradeService.GetTradeBonusPercent(camp) == 20
                );
                AddResult(
                    results,
                    "Trade Compact",
                    "Priority: Components > Textiles > Building Materials",
                    "Components > Textiles > Building Materials",
                    componentPriority + " > " + textilePriority + " > " +
                        buildingPriority,
                    componentPriority > textilePriority &&
                    textilePriority > buildingPriority
                );
                AddResult(
                    results,
                    "Trade Compact",
                    "Profile: 25d cooldown; intervention +2%; storage +1",
                    "25d, +2%, +1 stack",
                    FormatDays(
                        EnclaveQuestService
                            .GetSupplyRequestCooldownTicks(data)
                    ) +
                        ", " +
                        FormatSignedPercent(
                            tradeCompact.FriendlyInterventionChance
                        ) +
                        ", " +
                        (tradeVisual.StorageStackCount -
                            hearthboundStorageStacks) +
                        " stack",
                    EnclaveQuestService
                        .GetSupplyRequestCooldownTicks(data) ==
                        EnclaveArchetypeUtility
                            .TradeCompactSupplyRequestCooldownTicks &&
                    NearlyEqual(
                        tradeCompact.FriendlyInterventionChance,
                        0.02f
                    ) &&
                    tradeCompact.StorageStackBonus == 1 &&
                    tradeVisual.Archetype ==
                        EnclaveArchetype.TradeCompact &&
                    tradeVisual.StorageStackCount ==
                        hearthboundStorageStacks + 1
                );

                SetScenario(
                    data,
                    0,
                    EnclaveArchetype.WarriorCovenant
                );
                validArchetypeStayedAssigned &=
                    VerifyNoRederivation(camp);
                archetypeChangesPreservedIdentity &=
                    ArchetypeChangePreservedIdentity(snapshot, data);

                AddResult(
                    results,
                    "Warrior Covenant",
                    "Neutral recruitment: 550 silver",
                    "550 silver",
                    EnclaveRecruitmentService
                        .GetEffectiveRecruitmentCost(camp, null) +
                        " silver",
                    EnclaveRecruitmentService
                        .GetEffectiveRecruitmentCost(camp, null) == 550
                );

                data.SetReputation(-50, TestReason);
                EnclaveArchetypeProfile warrior =
                    EnclaveArchetypeUtility.GetProfile(data);
                int ordinaryMaximum = GetMaximumPartySize(
                    camp,
                    EnclaveDistanceBand.Strong
                );

                AddResult(
                    results,
                    "Warrior Covenant",
                    "Intervention: hostile +5%; friendly +4%; party +1",
                    "+5%, +4%, +1",
                    FormatSignedPercent(
                        warrior.HostileInterventionChance
                    ) +
                        ", " +
                        FormatSignedPercent(
                            warrior.FriendlyInterventionChance
                        ) +
                        ", " +
                        FormatSignedNumber(
                            warrior.InterventionPartyStrengthBonus
                        ),
                    NearlyEqual(
                        warrior.HostileInterventionChance,
                        0.05f
                    ) &&
                    NearlyEqual(
                        warrior.FriendlyInterventionChance,
                        0.04f
                    ) &&
                    warrior.InterventionPartyStrengthBonus == 1
                );

                if (
                    !EnclaveDevelopmentUtility.SetTier(
                        data,
                        EnclaveDevelopmentTier.TierIV,
                        TestReason
                    ) ||
                    !EnclaveIdeologyUtility.SetIdeologyType(
                        data,
                        EnclaveIdeologyType.Martial,
                        TestReason
                    )
                )
                {
                    throw new InvalidOperationException(
                        "Could not apply the temporary worst-case " +
                        "intervention scenario."
                    );
                }
                int worstCaseMaximum = GetMaximumPartySize(
                    camp,
                    EnclaveDistanceBand.Strong
                );

                AddResult(
                    results,
                    "Warrior Covenant",
                    "Party cap: 7",
                    "both scenarios <= 7",
                    "hostile max " +
                        ordinaryMaximum +
                        "; Tier IV/Strong/Martial max " +
                        worstCaseMaximum,
                    ordinaryMaximum <=
                        EnclaveArchetypeUtility
                            .MaximumInterventionPartySize &&
                    worstCaseMaximum <=
                        EnclaveArchetypeUtility
                            .MaximumInterventionPartySize
                );
            }
            catch (Exception exception)
            {
                executionException = exception;
            }
            finally
            {
                try
                {
                    restoreSucceeded = snapshot.RestoreScalars(data);
                }
                catch (Exception restoreException)
                {
                    executionException = executionException ??
                        restoreException;
                }
            }

            bool noRederivationAfterRestore =
                originalArchetypeValid &&
                !EnclaveArchetypeUtility.EnsureArchetype(
                    data,
                    camp.ID,
                    TestReason + " identity verification"
                );

            AddResult(
                results,
                "Safety",
                "Archetype valid/stable; identity and reputation restored",
                snapshot.Archetype + ", " + snapshot.Reputation +
                    "; no re-derivation",
                data.Archetype + ", " + data.Reputation +
                    "; stable " + noRederivationAfterRestore,
                restoreSucceeded &&
                data.Archetype == snapshot.Archetype &&
                data.Reputation == snapshot.Reputation &&
                validArchetypeStayedAssigned &&
                noRederivationAfterRestore
            );
            AddResult(
                results,
                "Safety",
                "Ideo reference/type unchanged",
                DescribeIdeology(snapshot),
                DescribeIdeology(data),
                archetypeChangesPreservedIdentity &&
                object.ReferenceEquals(
                    snapshot.IdeologyProfile,
                    data.IdeologyProfile
                ) &&
                object.ReferenceEquals(
                    snapshot.ActualIdeo,
                    EnclaveIdeologyUtility.GetActualIdeo(data)
                ) &&
                EnclaveIdeologyUtility.GetIdeologyType(data) ==
                    snapshot.IdeologyType
            );
            AddResult(
                results,
                "Safety",
                "Development Tier unchanged",
                snapshot.DevelopmentTier.ToString(),
                data.DevelopmentTier.ToString(),
                archetypeChangesPreservedIdentity &&
                data.DevelopmentTier == snapshot.DevelopmentTier
            );
            AddResult(
                results,
                "Safety",
                "Needs unchanged",
                "same list, records, and values",
                snapshot.NeedsMatch(data)
                    ? "same list, records, and values"
                    : "changed",
                snapshot.NeedsMatch(data)
            );
            AddResult(
                results,
                "Safety",
                "Supply Request unchanged",
                DescribeRequest(snapshot.ActiveRequest),
                DescribeRequest(data.ActiveQuestRequest),
                snapshot.RequestMatches(data)
            );
            AddResult(
                results,
                "Safety",
                "Trader pawn/inventory unchanged",
                DescribeTrader(snapshot.Trader, snapshot.TraderThings.Count),
                DescribeTrader(
                    camp.PawnRoles?.GetPawn(EnclavePawnRole.Trader),
                    camp.PawnRoles
                            ?.GetPawn(EnclavePawnRole.Trader)
                            ?.inventory
                            ?.innerContainer
                            ?.Count ?? 0
                ),
                snapshot.TraderMatches(camp)
            );
            AddResult(
                results,
                "Safety",
                "Generated-map state unchanged",
                snapshot.HadMap ? "same generated map" : "no map",
                camp.HasMap ? "generated map present" : "no map",
                snapshot.MapMatches(camp)
            );

            if (executionException != null)
            {
                AddResult(
                    results,
                    "Failure",
                    "Regression runner completed without exception",
                    "no exception",
                    executionException.GetType().Name + ": " +
                        executionException.Message,
                    false
                );
            }

            ShowReport(camp, results);
        }

        private static void SetScenario(
            EnclaveData data,
            int reputation,
            EnclaveArchetype archetype
        )
        {
            if (
                data.SetReputation(reputation, TestReason) != reputation ||
                !EnclaveArchetypeUtility.SetArchetype(
                    data,
                    archetype,
                    TestReason
                )
            )
            {
                throw new InvalidOperationException(
                    "Could not apply the temporary " +
                    archetype +
                    " scenario."
                );
            }
        }

        private static bool VerifyNoRederivation(PilgrimCamp camp)
        {
            EnclaveArchetype before = camp.Data.Archetype;

            return
                !EnclaveArchetypeUtility.EnsureArchetype(
                    camp.Data,
                    camp.ID,
                    TestReason + " stable identity check"
                ) &&
                camp.Data.Archetype == before;
        }

        private static bool ArchetypeChangePreservedIdentity(
            CampSnapshot snapshot,
            EnclaveData data
        )
        {
            return
                object.ReferenceEquals(
                    snapshot.IdeologyProfile,
                    data.IdeologyProfile
                ) &&
                object.ReferenceEquals(
                    snapshot.ActualIdeo,
                    EnclaveIdeologyUtility.GetActualIdeo(data)
                ) &&
                EnclaveIdeologyUtility.GetIdeologyType(data) ==
                    snapshot.IdeologyType &&
                data.DevelopmentTier == snapshot.DevelopmentTier;
        }

        private static int GetMaximumPartySize(
            PilgrimCamp camp,
            EnclaveDistanceBand distanceBand
        )
        {
            int maximum = 0;

            for (int seed = 0; seed < 512; seed++)
            {
                EnclaveInterventionProfile profile =
                    EnclaveInterventionProfileUtility.CreateProfile(
                        camp,
                        5f,
                        distanceBand,
                        seed
                    );

                maximum = Math.Max(maximum, profile.PartyStrength);
            }

            return maximum;
        }

        private static void AddResult(
            List<TestResult> results,
            string section,
            string label,
            string expected,
            string actual,
            bool passed
        )
        {
            results.Add(
                new TestResult
                {
                    Section = section,
                    Label = label,
                    Expected = expected,
                    Actual = actual,
                    Passed = passed
                }
            );
        }

        private static void ShowReport(
            PilgrimCamp camp,
            List<TestResult> results
        )
        {
            int passed = 0;

            foreach (TestResult result in results)
            {
                if (result.Passed)
                {
                    passed++;
                }
            }

            bool allPassed = passed == results.Count;
            StringBuilder report = new StringBuilder();
            report.AppendLine(
                "Archetype Regression Test \u2014 " +
                (allPassed ? "PASS" : "FAIL")
            );
            report.AppendLine(camp.Data.Name);

            string section = null;

            foreach (TestResult result in results)
            {
                if (section != result.Section)
                {
                    section = result.Section;
                    report.AppendLine();
                    report.AppendLine(section);
                }

                report.Append(
                    result.Passed ? "\u2713 " : "\u2717 "
                );
                report.Append(result.Label);

                if (!result.Passed)
                {
                    report.Append(
                        " \u2014 expected " +
                        result.Expected +
                        "; actual " +
                        result.Actual
                    );
                }

                report.AppendLine();
            }

            report.AppendLine();
            report.Append(
                "Result: " +
                passed +
                "/" +
                results.Count +
                " passed"
            );

            string text = report.ToString();
            Log.Message(
                "[IEE] DEV archetype regression test\n" + text
            );
            Find.WindowStack.Add(new Dialog_MessageBox(text));
        }

        private static bool IsValidArchetype(
            EnclaveArchetype archetype
        )
        {
            return
                archetype != EnclaveArchetype.Unassigned &&
                Enum.IsDefined(typeof(EnclaveArchetype), archetype);
        }

        private static bool NearlyEqual(float first, float second)
        {
            return Math.Abs(first - second) < 0.0001f;
        }

        private static string FormatDays(int ticks)
        {
            return (ticks / 60000f).ToString("0.#") + "d";
        }

        private static string FormatSignedPercent(int value)
        {
            return (value >= 0 ? "+" : string.Empty) + value + "%";
        }

        private static string FormatSignedPercent(float value)
        {
            return value >= 0f
                ? "+" + value.ToString("P0")
                : value.ToString("P0");
        }

        private static string FormatSignedNumber(int value)
        {
            return (value >= 0 ? "+" : string.Empty) + value;
        }

        private static string DescribeIdeology(CampSnapshot snapshot)
        {
            return
                snapshot.IdeologyType +
                "/" +
                (snapshot.ActualIdeo?.name ?? "no Ideo");
        }

        private static string DescribeIdeology(EnclaveData data)
        {
            return
                EnclaveIdeologyUtility.GetIdeologyType(data) +
                "/" +
                (EnclaveIdeologyUtility.GetActualIdeo(data)?.name ??
                    "no Ideo");
        }

        private static string DescribeRequest(
            EnclaveQuestRequest request
        )
        {
            return request == null
                ? "none"
                : "request " + request.RequestId;
        }

        private static string DescribeTrader(Pawn trader, int itemCount)
        {
            return trader == null
                ? "not initialized"
                : trader.LabelShort + "/" + itemCount + " inventory things";
        }
    }
}
