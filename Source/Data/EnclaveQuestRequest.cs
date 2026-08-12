using System;
using RimWorld;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public enum EnclaveQuestType
    {
        SupplyRequest
    }

    public enum EnclaveQuestRequestStatus
    {
        Active,
        Completed,
        Expired,
        Failed
    }

    public sealed class EnclaveQuestRequest : IExposable
    {
        private int requestId;
        private int originatingEnclaveId = -1;
        private EnclaveQuestType questType;
        private EnclaveNeedType associatedNeed;
        private ThingDef requestedThingDef;
        private int requestedQuantity;
        private int reputationReward;
        private int createdTick;
        private int expirationTick;
        private EnclaveQuestRequestStatus status;
        private Quest quest;

        public int RequestId => requestId;
        public int OriginatingEnclaveId => originatingEnclaveId;
        public EnclaveQuestType QuestType => questType;
        public EnclaveNeedType AssociatedNeed => associatedNeed;
        public ThingDef RequestedThingDef => requestedThingDef;
        public int RequestedQuantity => requestedQuantity;
        public int ReputationReward => reputationReward;
        public int CreatedTick => createdTick;
        public int ExpirationTick => expirationTick;
        public EnclaveQuestRequestStatus Status => status;
        public Quest Quest => quest;
        public bool IsActive => status == EnclaveQuestRequestStatus.Active;

        public EnclaveQuestRequest()
        {
        }

        public EnclaveQuestRequest(
            int requestId,
            int originatingEnclaveId,
            EnclaveNeedType associatedNeed,
            ThingDef requestedThingDef,
            int requestedQuantity,
            int reputationReward,
            int createdTick,
            int expirationTick
        )
        {
            this.requestId = requestId;
            this.originatingEnclaveId = originatingEnclaveId;
            questType = EnclaveQuestType.SupplyRequest;
            this.associatedNeed = associatedNeed;
            this.requestedThingDef = requestedThingDef;
            this.requestedQuantity = requestedQuantity;
            this.reputationReward = reputationReward;
            this.createdTick = createdTick;
            this.expirationTick = expirationTick;
            status = EnclaveQuestRequestStatus.Active;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref requestId, "requestId", 0);
            Scribe_Values.Look(
                ref originatingEnclaveId,
                "originatingEnclaveId",
                -1
            );
            Scribe_Values.Look(
                ref questType,
                "questType",
                EnclaveQuestType.SupplyRequest
            );
            Scribe_Values.Look(
                ref associatedNeed,
                "associatedNeed",
                EnclaveNeedType.Food
            );
            Scribe_Defs.Look(
                ref requestedThingDef,
                "requestedThingDef"
            );
            Scribe_Values.Look(
                ref requestedQuantity,
                "requestedQuantity",
                0
            );
            Scribe_Values.Look(
                ref reputationReward,
                "reputationReward",
                0
            );
            Scribe_Values.Look(ref createdTick, "createdTick", 0);
            Scribe_Values.Look(
                ref expirationTick,
                "expirationTick",
                0
            );
            Scribe_Values.Look(
                ref status,
                "status",
                EnclaveQuestRequestStatus.Active
            );
            Scribe_References.Look(ref quest, "quest");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (
                    !Enum.IsDefined(typeof(EnclaveQuestType), questType)
                )
                {
                    questType = EnclaveQuestType.SupplyRequest;
                }

                if (
                    !Enum.IsDefined(
                        typeof(EnclaveNeedType),
                        associatedNeed
                    )
                )
                {
                    associatedNeed = EnclaveNeedType.Food;
                }

                if (
                    !Enum.IsDefined(
                        typeof(EnclaveQuestRequestStatus),
                        status
                    )
                )
                {
                    status = EnclaveQuestRequestStatus.Failed;
                }

                requestedQuantity = Math.Max(0, requestedQuantity);
                reputationReward = Math.Max(0, reputationReward);

                if (
                    status == EnclaveQuestRequestStatus.Active &&
                    (
                        requestedThingDef == null ||
                        requestedQuantity <= 0 ||
                        originatingEnclaveId < 0
                    )
                )
                {
                    status = EnclaveQuestRequestStatus.Failed;
                }
            }
        }

        internal void AttachQuest(Quest newQuest)
        {
            quest = newQuest;
        }

        internal void MarkCompleted()
        {
            status = EnclaveQuestRequestStatus.Completed;
        }

        internal void MarkExpired()
        {
            if (IsActive)
            {
                status = EnclaveQuestRequestStatus.Expired;
            }
        }

        internal void MarkFailed()
        {
            if (IsActive)
            {
                status = EnclaveQuestRequestStatus.Failed;
            }
        }
    }
}
