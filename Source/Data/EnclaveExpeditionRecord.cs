using System;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public enum EnclaveExpeditionPurpose
    {
        Relief,
        Trade,
        Patrol
    }

    public enum EnclaveExpeditionState
    {
        None,
        ActiveSite,
        Completed
    }

    public sealed class EnclaveExpeditionRecord : IExposable
    {
        private int expeditionId;
        private EnclaveExpeditionPurpose purpose;
        private int siteWorldObjectId = -1;
        private int creationTick;
        private int expirationTick;
        private EnclaveExpeditionState state;

        public int ExpeditionId => expeditionId;
        public EnclaveExpeditionPurpose Purpose => purpose;
        public int SiteWorldObjectId => siteWorldObjectId;
        public int CreationTick => creationTick;
        public int ExpirationTick => expirationTick;
        public EnclaveExpeditionState State => state;
        public bool IsActive =>
            state == EnclaveExpeditionState.ActiveSite &&
            expeditionId > 0 &&
            siteWorldObjectId >= 0;

        public EnclaveExpeditionRecord()
        {
        }

        public EnclaveExpeditionRecord(
            int expeditionId,
            EnclaveExpeditionPurpose purpose,
            int siteWorldObjectId,
            int creationTick,
            int expirationTick
        )
        {
            this.expeditionId = Math.Max(1, expeditionId);
            this.purpose = purpose;
            this.siteWorldObjectId = siteWorldObjectId;
            this.creationTick = Math.Max(0, creationTick);
            this.expirationTick = Math.Max(
                this.creationTick + 1,
                expirationTick
            );
            state = EnclaveExpeditionState.ActiveSite;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(
                ref expeditionId,
                "expeditionId",
                0
            );
            Scribe_Values.Look(
                ref purpose,
                "purpose",
                EnclaveExpeditionPurpose.Relief
            );
            Scribe_Values.Look(
                ref siteWorldObjectId,
                "siteWorldObjectId",
                -1
            );
            Scribe_Values.Look(
                ref creationTick,
                "creationTick",
                0
            );
            Scribe_Values.Look(
                ref expirationTick,
                "expirationTick",
                0
            );
            Scribe_Values.Look(
                ref state,
                "state",
                EnclaveExpeditionState.None
            );

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Normalize();
            }
        }

        public void MarkCompleted()
        {
            state = EnclaveExpeditionState.Completed;
        }

        public void Normalize()
        {
            creationTick = Math.Max(0, creationTick);
            expirationTick = Math.Max(creationTick, expirationTick);

            if (
                !Enum.IsDefined(
                    typeof(EnclaveExpeditionPurpose),
                    purpose
                ) ||
                !Enum.IsDefined(
                    typeof(EnclaveExpeditionState),
                    state
                )
            )
            {
                state = EnclaveExpeditionState.Completed;
            }

            if (
                state == EnclaveExpeditionState.ActiveSite &&
                (
                    expeditionId <= 0 ||
                    siteWorldObjectId < 0 ||
                    expirationTick <= creationTick
                )
            )
            {
                state = EnclaveExpeditionState.Completed;
            }
        }
    }
}
