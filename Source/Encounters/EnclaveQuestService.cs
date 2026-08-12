using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveQuestService
    {
        public const int SupplyRequestExpirationTicks = 1200000;
        public const int SupplyRequestCooldownTicks = 1800000;

        public const int ModerateReputationReward = 5;
        public const int SevereReputationReward = 8;
        public const int CriticalReputationReward = 13;

        private const string SupplyRequestQuestDefName =
            "IEE_EnclaveSupplyRequest";

        public static bool TryGenerateSupplyRequest(
            PilgrimCamp camp,
            out EnclaveQuestRequest generatedRequest
        )
        {
            generatedRequest = null;

            if (
                camp?.Data == null ||
                camp.Destroyed ||
                !camp.Spawned ||
                camp.ID < 0 ||
                Find.QuestManager == null ||
                EnclaveRelationshipUtility.IsLocallyHostile(camp)
            )
            {
                return false;
            }

            ReconcileRequestState(camp);

            EnclaveQuestRequest existing =
                camp.Data.ActiveQuestRequest;
            int currentTick = Find.TickManager?.TicksGame ?? 0;

            if (
                existing?.IsActive == true ||
                currentTick < camp.Data.NextQuestRequestEligibleTick
            )
            {
                return false;
            }

            EnclaveNeedRecord need =
                EnclaveNeedsUtility.GetMostUrgentNeed(camp);

            if (
                need == null ||
                need.Severity < EnclaveNeedSeverity.Moderate
            )
            {
                return false;
            }

            ThingDef requestedThing = GetRequestedThingDef(
                need.Type,
                need.Severity
            );
            int quantity = CalculateRequestedQuantity(
                camp.Data,
                need.Type,
                need.Severity
            );
            int reputationReward = GetReputationReward(
                need.Severity
            );

            if (
                requestedThing == null ||
                quantity <= 0 ||
                reputationReward <= 0
            )
            {
                return false;
            }

            int expirationTick = AddTicksClamped(
                currentTick,
                SupplyRequestExpirationTicks
            );
            EnclaveQuestRequest request = new EnclaveQuestRequest(
                camp.Data.AllocateQuestRequestId(),
                camp.ID,
                need.Type,
                requestedThing,
                quantity,
                reputationReward,
                currentTick,
                expirationTick
            );
            Quest quest = CreateSupplyRequestQuest(
                camp,
                request,
                SupplyRequestExpirationTicks
            );

            if (quest == null)
            {
                request.MarkFailed();
                return false;
            }

            Find.QuestManager.Add(quest);
            request.AttachQuest(quest);
            camp.Data.SetActiveQuestRequest(request);
            camp.Data.SetNextQuestRequestEligibleTick(
                AddTicksClamped(
                    currentTick,
                    SupplyRequestCooldownTicks
                )
            );
            QuestUtility.SendLetterQuestAvailable(
                quest,
                camp.Data.Name
            );

            generatedRequest = request;

            Log.Message(
                "[IEE] Generated Supply Request for " +
                camp.Data.Name +
                ": " +
                quantity +
                " " +
                requestedThing.label +
                " for " +
                EnclaveNeedsUtility.GetNeedLabel(need.Type) +
                " (" +
                need.Severity +
                "), reward +" +
                reputationReward +
                " reputation, expires at tick " +
                expirationTick +
                "."
            );

            return true;
        }

        public static EnclaveQuestRequest GetActiveSupplyRequest(
            PilgrimCamp camp
        )
        {
            ReconcileRequestState(camp);
            EnclaveQuestRequest request =
                camp?.Data?.ActiveQuestRequest;

            return
                request?.IsActive == true &&
                request.QuestType == EnclaveQuestType.SupplyRequest
                    ? request
                    : null;
        }

        public static bool SupplyDeliveryContactIsValid(
            PilgrimCamp camp,
            Pawn leader,
            out string failureReason
        )
        {
            failureReason = null;

            if (
                camp == null ||
                camp.Destroyed ||
                !camp.Spawned ||
                camp.Data == null ||
                camp.Map == null
            )
            {
                failureReason = "The enclave is no longer available.";
                return false;
            }

            if (EnclaveRelationshipUtility.IsLocallyHostile(camp))
            {
                failureReason =
                    "Supply delivery is unavailable while this enclave " +
                    "is hostile toward the visiting group.";
                return false;
            }

            if (
                leader == null ||
                leader.Destroyed ||
                leader.Dead ||
                !leader.Spawned ||
                leader.Map != camp.Map ||
                !leader.RaceProps.Humanlike ||
                leader.Faction == Faction.OfPlayer ||
                camp.PawnRoles?.GetPawn(EnclavePawnRole.Leader) != leader
            )
            {
                failureReason =
                    "The enclave Leader is no longer available.";
                return false;
            }

            if (
                camp.VisitingGroup == null ||
                !camp.VisitingGroup.HasStoredMembers ||
                !camp.VisitingGroup.HasActiveMembers(camp)
            )
            {
                failureReason =
                    "No active visiting caravan group is registered for " +
                    "this enclave.";
                return false;
            }

            EnclaveQuestRequest request = GetActiveSupplyRequest(camp);

            if (request == null)
            {
                failureReason =
                    "This enclave has no active Supply Request.";
                return false;
            }

            if (
                request.OriginatingEnclaveId != camp.ID ||
                request.RequestedThingDef == null ||
                request.RequestedQuantity <= 0 ||
                EnclaveNeedsUtility.GetNeed(
                    camp.Data,
                    request.AssociatedNeed
                ) == null
            )
            {
                failureReason =
                    "The saved Supply Request is invalid and cannot be " +
                    "completed.";
                return false;
            }

            if (request.Quest == null)
            {
                failureReason =
                    "The Supply Request's vanilla quest record is " +
                    "unavailable.";
                return false;
            }

            if (request.Quest.State == QuestState.NotYetAccepted)
            {
                failureReason =
                    "Accept the Enclave Supply Request in the Quests tab " +
                    "before delivering supplies.";
                return false;
            }

            if (request.Quest.State != QuestState.Ongoing)
            {
                failureReason =
                    "The Enclave Supply Request is no longer active.";
                return false;
            }

            int currentTick = Find.TickManager?.TicksGame ?? 0;

            if (currentTick >= request.ExpirationTick)
            {
                ExpireRequest(camp, request.RequestId, request.Quest);
                failureReason =
                    "The Enclave Supply Request has expired.";
                return false;
            }

            return true;
        }

        public static int GetAvailableRequestedItems(PilgrimCamp camp)
        {
            EnclaveQuestRequest request = GetActiveSupplyRequest(camp);

            return
                request?.RequestedThingDef == null
                    ? 0
                    : camp.VisitingGroup?.CountInventoryThing(
                        camp,
                        request.RequestedThingDef
                    ) ?? 0;
        }

        public static bool TryCompleteSupplyRequest(
            PilgrimCamp camp,
            Pawn leader,
            out string resultMessage
        )
        {
            if (
                !SupplyDeliveryContactIsValid(
                    camp,
                    leader,
                    out resultMessage
                )
            )
            {
                return false;
            }

            EnclaveQuestRequest request = GetActiveSupplyRequest(camp);
            int available = GetAvailableRequestedItems(camp);

            if (available < request.RequestedQuantity)
            {
                resultMessage =
                    "The visiting group has only " +
                    available +
                    " of the required " +
                    request.RequestedQuantity +
                    " " +
                    request.RequestedThingDef.label +
                    ". No items were consumed.";
                return false;
            }

            if (
                !camp.VisitingGroup.TryConsumeInventoryThing(
                    camp,
                    request.RequestedThingDef,
                    request.RequestedQuantity
                )
            )
            {
                resultMessage =
                    "The visiting group's supplies changed before the " +
                    "delivery could be completed. No quest state or " +
                    "reputation was changed.";
                return false;
            }

            int previousReputation = camp.Data.Reputation;
            EnclaveReputationTier previousTier =
                camp.Data.ReputationTier;

            EnclaveNeedsUtility.AdjustNeed(
                camp.Data,
                request.AssociatedNeed,
                request.RequestedQuantity,
                "Supply Request delivery"
            );
            request.MarkCompleted();
            camp.Data.SetNextQuestRequestEligibleTick(
                Math.Max(
                    camp.Data.NextQuestRequestEligibleTick,
                    AddTicksClamped(
                        Find.TickManager?.TicksGame ?? 0,
                        SupplyRequestCooldownTicks
                    )
                )
            );

            int updatedReputation = camp.Data.ChangeReputation(
                request.ReputationReward,
                "completed enclave Supply Request"
            );
            EnclaveLocalHostilityService.NotifyReputationChanged(
                camp,
                previousTier
            );
            int appliedReward = updatedReputation - previousReputation;

            if (!request.Quest.Historical)
            {
                request.Quest.End(QuestEndOutcome.Success);
            }

            resultMessage =
                "Delivered " +
                request.RequestedQuantity +
                " " +
                request.RequestedThingDef.label +
                " to " +
                camp.Data.Name +
                ". Reputation increased by " +
                appliedReward +
                ": " +
                previousReputation +
                " -> " +
                updatedReputation +
                " (" +
                camp.Data.ReputationTierLabel +
                ").";

            Log.Message(
                "[IEE] Completed Supply Request " +
                request.RequestId +
                " for " +
                camp.Data.Name +
                " through Leader " +
                leader.LabelShort +
                ". Consumed " +
                request.RequestedQuantity +
                " " +
                request.RequestedThingDef.defName +
                "; reputation " +
                previousReputation +
                " -> " +
                updatedReputation +
                "."
            );

            return true;
        }

        public static bool ExpireRequest(
            PilgrimCamp camp,
            int requestId,
            Quest quest
        )
        {
            EnclaveQuestRequest request =
                camp?.Data?.ActiveQuestRequest;

            if (
                request == null ||
                !request.IsActive ||
                request.RequestId != requestId ||
                request.Quest != quest
            )
            {
                return false;
            }

            request.MarkExpired();
            camp.Data.SetNextQuestRequestEligibleTick(
                Math.Max(
                    camp.Data.NextQuestRequestEligibleTick,
                    AddTicksClamped(
                        Find.TickManager?.TicksGame ?? 0,
                        SupplyRequestCooldownTicks
                    )
                )
            );

            if (quest != null && !quest.Historical)
            {
                quest.End(
                    QuestEndOutcome.Fail,
                    sendLetter: false,
                    playSound: false
                );
            }

            Messages.Message(
                "The Supply Request from " +
                (camp.Data?.Name ?? "an enclave") +
                " has expired. No reputation was lost.",
                MessageTypeDefOf.NeutralEvent
            );

            Log.Message(
                "[IEE] Supply Request " +
                requestId +
                " for " +
                (camp.Data?.Name ?? "an enclave") +
                " expired without a reputation penalty."
            );

            return true;
        }

        public static void NotifyQuestCleanup(
            PilgrimCamp camp,
            int requestId,
            Quest quest
        )
        {
            EnclaveQuestRequest request =
                camp?.Data?.ActiveQuestRequest;

            if (
                request == null ||
                !request.IsActive ||
                request.RequestId != requestId ||
                request.Quest != quest
            )
            {
                return;
            }

            if (
                quest == null ||
                quest.State != QuestState.NotYetAccepted &&
                quest.State != QuestState.Ongoing
            )
            {
                request.MarkExpired();
                camp.Data.SetNextQuestRequestEligibleTick(
                    Math.Max(
                        camp.Data.NextQuestRequestEligibleTick,
                        AddTicksClamped(
                            Find.TickManager?.TicksGame ?? 0,
                            SupplyRequestCooldownTicks
                        )
                    )
                );
            }
        }

        public static ThingDef GetRequestedThingDef(
            EnclaveNeedType needType,
            EnclaveNeedSeverity severity
        )
        {
            switch (needType)
            {
                case EnclaveNeedType.Food:
                    return ThingDefOf.MealSurvivalPack;
                case EnclaveNeedType.Medicine:
                    return severity >= EnclaveNeedSeverity.Severe
                        ? ThingDefOf.MedicineIndustrial
                        : ThingDefOf.MedicineHerbal;
                case EnclaveNeedType.BuildingMaterials:
                    return ThingDefOf.Steel;
                case EnclaveNeedType.Textiles:
                    return ThingDefOf.Cloth;
                case EnclaveNeedType.Components:
                    return ThingDefOf.ComponentIndustrial;
                default:
                    return null;
            }
        }

        public static int CalculateRequestedQuantity(
            EnclaveData data,
            EnclaveNeedType needType,
            EnclaveNeedSeverity severity
        )
        {
            EnclaveNeedProfile profile =
                EnclaveNeedsUtility.GetNeedProfile(needType);

            if (profile == null)
            {
                return 0;
            }

            int quantity = DivideRoundUp(
                (long)profile.BaseRequestQuantity *
                GetSeverityQuantityPercent(severity) *
                GetDevelopmentQuantityPercent(data),
                10000
            );

            if (
                needType == EnclaveNeedType.Food ||
                needType == EnclaveNeedType.BuildingMaterials ||
                needType == EnclaveNeedType.Textiles
            )
            {
                quantity = RoundUpToMultiple(quantity, 5);
            }

            return Math.Max(1, quantity);
        }

        public static int GetReputationReward(
            EnclaveNeedSeverity severity
        )
        {
            switch (severity)
            {
                case EnclaveNeedSeverity.Critical:
                    return CriticalReputationReward;
                case EnclaveNeedSeverity.Severe:
                    return SevereReputationReward;
                case EnclaveNeedSeverity.Moderate:
                    return ModerateReputationReward;
                default:
                    return 0;
            }
        }

        public static string DescribeRequest(
            EnclaveQuestRequest request
        )
        {
            if (request == null)
            {
                return "No active request";
            }

            return
                request.RequestedQuantity +
                " " +
                (request.RequestedThingDef?.label ?? "unknown supplies") +
                " for " +
                EnclaveNeedsUtility.GetNeedLabel(
                    request.AssociatedNeed
                ) +
                "; +" +
                request.ReputationReward +
                " reputation; " +
                request.Status;
        }

        public static void MigrateMalformedSupplyRequest(
            PilgrimCamp camp
        )
        {
            EnclaveQuestRequest request =
                camp?.Data?.ActiveQuestRequest;
            Quest legacyQuest = request?.Quest;

            if (
                legacyQuest == null ||
                legacyQuest.root != null ||
                !IsLegacyRawSupplyRequest(
                    legacyQuest,
                    camp,
                    request.RequestId
                )
            )
            {
                return;
            }

            QuestState legacyState = legacyQuest.State;
            bool shouldReplace =
                request.IsActive &&
                (
                    legacyState == QuestState.NotYetAccepted ||
                    legacyState == QuestState.Ongoing
                );
            int currentTick = Find.TickManager?.TicksGame ?? 0;
            int remainingTicks = Math.Max(
                0,
                request.ExpirationTick - currentTick
            );

            QuestManager questManager = Find.QuestManager;

            if (questManager == null)
            {
                Log.Error(
                    "[IEE] Cannot migrate malformed legacy Supply " +
                    "Request " +
                    request.RequestId +
                    " because QuestManager is unavailable."
                );
                return;
            }

            if (
                questManager.Contains(legacyQuest)
            )
            {
                // Do not End or clean up this malformed quest. VEF's
                // lifecycle patches also assume quest.root is non-null.
                questManager.Remove(legacyQuest);
            }

            request.AttachQuest(null);

            if (!shouldReplace || remainingTicks <= 0)
            {
                request.MarkExpired();

                Log.Warning(
                    "[IEE] Removed malformed legacy Supply Request " +
                    request.RequestId +
                    " for " +
                    (camp.Data?.Name ?? "an enclave") +
                    " without replacing it because it was no longer " +
                    "active."
                );
                return;
            }

            Quest replacement = CreateSupplyRequestQuest(
                camp,
                request,
                remainingTicks
            );

            if (replacement == null || replacement.root == null)
            {
                request.MarkFailed();

                Log.Error(
                    "[IEE] Could not replace malformed legacy Supply " +
                    "Request " +
                    request.RequestId +
                    " for " +
                    (camp.Data?.Name ?? "an enclave") +
                    ". The request was marked failed."
                );
                return;
            }

            if (legacyState == QuestState.Ongoing)
            {
                replacement.SetInitiallyAccepted();
            }
            else
            {
                replacement.SetNotYetAccepted();
            }

            replacement.acceptanceExpireTick = request.ExpirationTick;
            request.AttachQuest(replacement);
            questManager.Add(replacement);

            Log.Warning(
                "[IEE] Replaced malformed legacy Supply Request " +
                request.RequestId +
                " for " +
                (camp.Data?.Name ?? "an enclave") +
                " with a QuestGen quest rooted at " +
                replacement.root.defName +
                "."
            );
        }

        private static Quest CreateSupplyRequestQuest(
            PilgrimCamp camp,
            EnclaveQuestRequest request,
            int deadlineTicks
        )
        {
            QuestScriptDef questDef =
                DefDatabase<QuestScriptDef>.GetNamedSilentFail(
                    SupplyRequestQuestDefName
                );

            if (questDef == null)
            {
                Log.Error(
                    "[IEE] Cannot generate a Supply Request because " +
                    SupplyRequestQuestDefName +
                    " is missing."
                );
                return null;
            }

            string questName =
                "Enclave Supply Request: " +
                EnclaveNeedsUtility.GetNeedLabel(
                    request.AssociatedNeed
                );
            string questDescription = BuildQuestDescription(
                camp,
                request
            );
            Slate slate = new Slate();
            slate.Set("resolvedQuestName", questName);
            slate.Set(
                "resolvedQuestDescription",
                questDescription
            );
            slate.Set("ieeOriginatingEnclave", camp);

            Quest quest = QuestGen.Generate(questDef, slate);

            if (quest == null || quest.root != questDef)
            {
                Log.Error(
                    "[IEE] QuestGen failed to create a rooted Supply " +
                    "Request for " +
                    (camp?.Data?.Name ?? "an enclave") +
                    "."
                );
                return null;
            }

            quest.SetNotYetAccepted();
            quest.acceptanceExpireTick = request.ExpirationTick;

            int safeDeadlineTicks = Math.Max(1, deadlineTicks);

            QuestPart_EnclaveSupplyRequestDeadline deadline =
                new QuestPart_EnclaveSupplyRequestDeadline
                {
                    Camp = camp,
                    RequestId = request.RequestId,
                    delayTicks = safeDeadlineTicks,
                    inSignalEnable = quest.AddedSignal,
                    expiryInfoPart =
                        "This supply request expires in " +
                        GenDate.ToStringTicksToPeriod(
                            safeDeadlineTicks
                        ) +
                        ".",
                    expiryInfoPartTip =
                        "Deliver the requested supplies to the enclave " +
                        "Leader before the deadline.",
                    alertLabel = "Enclave supply request expiring",
                    alertExplanation =
                        camp.Data.Name +
                        " is still waiting for " +
                        request.RequestedQuantity +
                        " " +
                        request.RequestedThingDef.label +
                        ".",
                    alertCulprits = new List<GlobalTargetInfo>
                    {
                        new GlobalTargetInfo(camp)
                    },
                    ticksLeftAlertCritical = 120000
                };

            quest.AddPart(deadline);

            if (Prefs.DevMode)
            {
                Log.Message(
                    "[IEE] DEV Supply Request quest metadata: id=" +
                    quest.id +
                    ", root=" +
                    quest.root.defName +
                    ", state=" +
                    quest.State +
                    ", parts=" +
                    quest.PartsListForReading.Count +
                    ", tags=" +
                    quest.tags.Count +
                    ", lookTargets=" +
                    quest.QuestLookTargets.Count() +
                    "."
                );
            }

            return quest;
        }

        private static bool IsLegacyRawSupplyRequest(
            Quest quest,
            PilgrimCamp camp,
            int requestId
        )
        {
            if (quest == null || quest.root != null)
            {
                return false;
            }

            List<QuestPart> parts = quest.PartsListForReading;

            for (int index = 0; index < parts.Count; index++)
            {
                QuestPart_EnclaveSupplyRequestDeadline deadline =
                    parts[index]
                        as QuestPart_EnclaveSupplyRequestDeadline;

                if (
                    deadline != null &&
                    deadline.Camp == camp &&
                    deadline.RequestId == requestId
                )
                {
                    return true;
                }
            }

            return false;
        }

        private static string BuildQuestDescription(
            PilgrimCamp camp,
            EnclaveQuestRequest request
        )
        {
            return
                camp.Data.Name +
                " is experiencing a " +
                EnclaveNeedsUtility
                    .GetNeedLabel(request.AssociatedNeed)
                    .ToLowerInvariant() +
                " shortage. " +
                GetIdeologyFlavor(camp.Data) +
                "\n\nThey request " +
                request.RequestedQuantity +
                " " +
                request.RequestedThingDef.label +
                ". Visit this specific Pilgrim Camp with the supplies " +
                "carried by colonists or pack animals, accept the quest, " +
                "then speak with the enclave Leader to deliver them." +
                "\n\nReward: +" +
                request.ReputationReward +
                " reputation with " +
                camp.Data.Name +
                "." +
                "\nExpiration: " +
                GenDate.ToStringTicksToPeriod(
                    SupplyRequestExpirationTicks
                ) +
                ".";
        }

        private static string GetIdeologyFlavor(EnclaveData data)
        {
            switch (EnclaveIdeologyUtility.GetIdeologyType(data))
            {
                case EnclaveIdeologyType.Communal:
                    return "They appeal to the value of sustaining a " +
                        "community together.";
                case EnclaveIdeologyType.Isolationist:
                    return "Their stores must be restored if the enclave " +
                        "is to remain self-reliant.";
                case EnclaveIdeologyType.Martial:
                    return "They frame the request as necessary for " +
                        "readiness and survival.";
                case EnclaveIdeologyType.Mercantile:
                    return "They offer the request as a practical act of " +
                        "regional cooperation.";
                case EnclaveIdeologyType.Nature:
                    return "They seek supplies that will preserve the " +
                        "health of their community.";
                case EnclaveIdeologyType.Spiritual:
                    return "They describe meeting the need as a shared " +
                        "moral obligation.";
                case EnclaveIdeologyType.Transhumanist:
                    return "They need the supplies to maintain their " +
                        "community's technical progress.";
                default:
                    return "They are asking neighboring communities for " +
                        "assistance.";
            }
        }

        private static void ReconcileRequestState(PilgrimCamp camp)
        {
            EnclaveQuestRequest request =
                camp?.Data?.ActiveQuestRequest;

            if (request?.IsActive != true)
            {
                return;
            }

            Quest quest = request.Quest;

            if (quest == null)
            {
                request.MarkFailed();
                return;
            }

            if (
                quest.State != QuestState.NotYetAccepted &&
                quest.State != QuestState.Ongoing
            )
            {
                request.MarkExpired();
            }
        }

        private static int GetSeverityQuantityPercent(
            EnclaveNeedSeverity severity
        )
        {
            switch (severity)
            {
                case EnclaveNeedSeverity.Critical:
                    return 180;
                case EnclaveNeedSeverity.Severe:
                    return 140;
                default:
                    return 100;
            }
        }

        private static int GetDevelopmentQuantityPercent(
            EnclaveData data
        )
        {
            switch (EnclaveDevelopmentUtility.GetTier(data))
            {
                case EnclaveDevelopmentTier.TierII:
                    return 120;
                case EnclaveDevelopmentTier.TierIII:
                    return 150;
                case EnclaveDevelopmentTier.TierIV:
                    return 180;
                default:
                    return 100;
            }
        }

        private static int AddTicksClamped(int currentTick, int amount)
        {
            long result = (long)currentTick + amount;
            return result >= int.MaxValue
                ? int.MaxValue
                : (int)result;
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

        private static int RoundUpToMultiple(int value, int multiple)
        {
            if (value <= 0 || multiple <= 1)
            {
                return value;
            }

            long rounded =
                ((long)value + multiple - 1) /
                multiple *
                multiple;

            return rounded >= int.MaxValue
                ? int.MaxValue
                : (int)rounded;
        }
    }

    public sealed class QuestPart_EnclaveSupplyRequestDeadline
        : QuestPart_Delay
    {
        public PilgrimCamp Camp;
        public int RequestId;

        public override IEnumerable<GlobalTargetInfo> QuestLookTargets
        {
            get
            {
                if (Camp != null)
                {
                    yield return new GlobalTargetInfo(Camp);
                }
            }
        }

        protected override void DelayFinished()
        {
            if (
                !EnclaveQuestService.ExpireRequest(
                    Camp,
                    RequestId,
                    quest
                ) &&
                quest != null &&
                !quest.Historical
            )
            {
                quest.End(
                    QuestEndOutcome.Fail,
                    sendLetter: false,
                    playSound: false
                );
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();
            EnclaveQuestService.NotifyQuestCleanup(
                Camp,
                RequestId,
                quest
            );
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref Camp, "pilgrimCamp");
            Scribe_Values.Look(ref RequestId, "requestId", 0);
        }
    }
}
