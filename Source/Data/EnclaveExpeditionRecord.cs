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
        Completed,
        ActiveColonyVisit
    }

    public enum EnclaveExpeditionOutcome
    {
        TemporarySite,
        ColonyVisit
    }

    public sealed class EnclaveExpeditionRecord : IExposable
    {
        private int expeditionId;
        private EnclaveExpeditionPurpose purpose;
        private int siteWorldObjectId = -1;
        private int creationTick;
        private int expirationTick;
        private EnclaveExpeditionState state;
        private EnclaveExpeditionOutcome outcome;
        private int destinationWorldObjectId = -1;
        private int destinationMapId = -1;

        public int ExpeditionId => expeditionId;
        public EnclaveExpeditionPurpose Purpose => purpose;
        public int SiteWorldObjectId => siteWorldObjectId;
        public int CreationTick => creationTick;
        public int ExpirationTick => expirationTick;
        public EnclaveExpeditionState State => state;
        public EnclaveExpeditionOutcome Outcome => outcome;
        public int DestinationWorldObjectId =>
            destinationWorldObjectId;
        public int DestinationMapId => destinationMapId;
        public bool IsActive =>
            expeditionId > 0 &&
            (
                (
                    state == EnclaveExpeditionState.ActiveSite &&
                    outcome ==
                        EnclaveExpeditionOutcome.TemporarySite &&
                    siteWorldObjectId >= 0
                ) ||
                (
                    state ==
                        EnclaveExpeditionState.ActiveColonyVisit &&
                    outcome ==
                        EnclaveExpeditionOutcome.ColonyVisit &&
                    destinationWorldObjectId >= 0 &&
                    destinationMapId >= 0
                )
            );
        public bool IsTemporarySite =>
            IsActive &&
            outcome == EnclaveExpeditionOutcome.TemporarySite;
        public bool IsColonyVisit =>
            IsActive &&
            outcome == EnclaveExpeditionOutcome.ColonyVisit;

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
            outcome = EnclaveExpeditionOutcome.TemporarySite;
            state = EnclaveExpeditionState.ActiveSite;
        }

        public static EnclaveExpeditionRecord CreateColonyVisit(
            int expeditionId,
            EnclaveExpeditionPurpose purpose,
            int destinationWorldObjectId,
            int destinationMapId,
            int creationTick,
            int departureTick
        )
        {
            return new EnclaveExpeditionRecord
            {
                expeditionId = Math.Max(1, expeditionId),
                purpose = purpose,
                siteWorldObjectId = -1,
                creationTick = Math.Max(0, creationTick),
                expirationTick = Math.Max(
                    Math.Max(0, creationTick) + 1,
                    departureTick
                ),
                state = EnclaveExpeditionState.ActiveColonyVisit,
                outcome = EnclaveExpeditionOutcome.ColonyVisit,
                destinationWorldObjectId = destinationWorldObjectId,
                destinationMapId = destinationMapId
            };
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
            Scribe_Values.Look(
                ref outcome,
                "outcome",
                EnclaveExpeditionOutcome.TemporarySite
            );
            Scribe_Values.Look(
                ref destinationWorldObjectId,
                "destinationWorldObjectId",
                -1
            );
            Scribe_Values.Look(
                ref destinationMapId,
                "destinationMapId",
                -1
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
                ) ||
                !Enum.IsDefined(
                    typeof(EnclaveExpeditionOutcome),
                    outcome
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

            if (
                state == EnclaveExpeditionState.ActiveColonyVisit &&
                (
                    outcome != EnclaveExpeditionOutcome.ColonyVisit ||
                    expeditionId <= 0 ||
                    destinationWorldObjectId < 0 ||
                    destinationMapId < 0 ||
                    expirationTick <= creationTick
                )
            )
            {
                state = EnclaveExpeditionState.Completed;
            }

            if (state == EnclaveExpeditionState.ActiveSite)
            {
                outcome = EnclaveExpeditionOutcome.TemporarySite;
                destinationWorldObjectId = -1;
                destinationMapId = -1;
            }
        }
    }
}
