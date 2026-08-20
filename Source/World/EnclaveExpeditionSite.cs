using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public sealed class EnclaveExpeditionSite : MapParent
    {
        private PilgrimCamp sourceCamp;
        private int expeditionId;
        private EnclaveExpeditionPurpose purpose;
        private int creationTick;
        private int expirationTick;
        private bool pendingExpiration;
        private bool partyInitialized;
        private Pawn expeditionTrader;
        private EnclavePawnMembers expeditionMembers =
            new EnclavePawnMembers();
        private EnclaveVisitingGroup visitingGroup =
            new EnclaveVisitingGroup();

        protected override bool UseGenericEnterMapFloatMenuOption =>
            false;

        protected override int UpdateRateTicks => 250;

        public PilgrimCamp SourceCamp => sourceCamp;
        public int ExpeditionId => expeditionId;
        public EnclaveExpeditionPurpose Purpose => purpose;
        public int CreationTick => creationTick;
        public int ExpirationTick => expirationTick;
        public bool PendingExpiration => pendingExpiration;
        public bool PartyInitialized => partyInitialized;
        public Pawn ExpeditionTrader => expeditionTrader;
        public EnclavePawnMembers ExpeditionMembers =>
            expeditionMembers;
        public EnclaveVisitingGroup VisitingGroup => visitingGroup;

        public override string Label =>
            (sourceCamp?.Data?.Name ?? "Enclave") +
            " " +
            EnclaveExpeditionUtility.GetSiteTypeLabel(purpose);

        public void Initialize(
            PilgrimCamp source,
            int id,
            EnclaveExpeditionPurpose expeditionPurpose,
            int createdAt,
            int expiresAt
        )
        {
            sourceCamp = source;
            expeditionId = id;
            purpose = expeditionPurpose;
            creationTick = createdAt;
            expirationTick = expiresAt;
            pendingExpiration = false;
            partyInitialized = false;
        }

        public void SetTemporaryParty(
            IEnumerable<Pawn> members,
            Pawn trader
        )
        {
            if (expeditionMembers == null)
            {
                expeditionMembers = new EnclavePawnMembers();
            }

            expeditionMembers.SetMembers(members);
            expeditionTrader = trader;
            partyInitialized = true;
        }

        public void ClearTemporaryParty()
        {
            expeditionMembers?.SetMembers(null);
            expeditionTrader = null;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_References.Look(
                ref sourceCamp,
                "sourcePilgrimCamp"
            );
            Scribe_Values.Look(ref expeditionId, "expeditionId", 0);
            Scribe_Values.Look(
                ref purpose,
                "purpose",
                EnclaveExpeditionPurpose.Relief
            );
            Scribe_Values.Look(ref creationTick, "creationTick", 0);
            Scribe_Values.Look(
                ref expirationTick,
                "expirationTick",
                0
            );
            Scribe_Values.Look(
                ref pendingExpiration,
                "pendingExpiration",
                false
            );
            Scribe_Values.Look(
                ref partyInitialized,
                "partyInitialized",
                false
            );
            Scribe_References.Look(
                ref expeditionTrader,
                "expeditionTrader"
            );
            Scribe_Deep.Look(
                ref expeditionMembers,
                "expeditionMembers"
            );
            Scribe_Deep.Look(
                ref visitingGroup,
                "visitingGroup"
            );

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (expeditionMembers == null)
                {
                    expeditionMembers = new EnclavePawnMembers();
                }

                if (visitingGroup == null)
                {
                    visitingGroup = new EnclaveVisitingGroup();
                }

                if (!visitingGroup.HasStoredMembers && Map != null)
                {
                    visitingGroup.RecoverFromMap(Map);
                }

                EnclaveTradeService.SuppressVanillaTradeOption(
                    expeditionTrader
                );
            }
        }

        public override IEnumerable<FloatMenuOption>
            GetFloatMenuOptions(Caravan caravan)
        {
            foreach (
                FloatMenuOption option in
                base.GetFloatMenuOptions(caravan)
            )
            {
                yield return option;
            }

            string reason;

            if (!CanPlayerVisit(out reason))
            {
                yield return new FloatMenuOption(
                    "Visit " + Label + " (unavailable)",
                    delegate
                    {
                        Messages.Message(
                            reason,
                            MessageTypeDefOf.RejectInput
                        );
                    }
                );
                yield break;
            }

            yield return new FloatMenuOption(
                "Visit " + Label,
                delegate
                {
                    caravan.pather.StartPath(
                        Tile,
                        new CaravanArrivalAction_VisitExpedition(this),
                        repathImmediately: true
                    );
                }
            );
        }

        public bool CanPlayerVisit(out string reason)
        {
            if (
                Destroyed ||
                pendingExpiration ||
                expirationTick <=
                    (Find.TickManager?.TicksGame ?? 0)
            )
            {
                reason =
                    "This expedition site is expiring and no longer " +
                    "accepts visitors.";
                return false;
            }

            if (
                sourceCamp == null ||
                sourceCamp.Destroyed ||
                sourceCamp.Data == null
            )
            {
                reason =
                    "The expedition's originating enclave no longer " +
                    "exists.";
                return false;
            }

            if (
                EnclaveRelationshipUtility.IsLocallyHostile(sourceCamp)
            )
            {
                reason =
                    "This expedition belongs to a Hostile enclave. " +
                    "Voluntary visits to hostile expedition sites are " +
                    "not available in this stage.";
                return false;
            }

            reason = null;
            return true;
        }

        public bool HasPlayerPresence()
        {
            return
                Map?.mapPawns?.AnyPawnBlockingMapRemoval == true;
        }

        public void BeginExpiration()
        {
            if (Destroyed)
            {
                return;
            }

            pendingExpiration = true;

            if (!HasMap)
            {
                Destroy();
                return;
            }

            CheckRemoveMapNow();
        }

        public override bool ShouldRemoveMapNow(
            out bool alsoRemoveWorldObject
        )
        {
            alsoRemoveWorldObject = pendingExpiration;

            return
                pendingExpiration &&
                HasMap &&
                !Map.mapPawns.AnyPawnBlockingMapRemoval;
        }

        protected override void TickInterval(int delta)
        {
            int currentTick = Find.TickManager?.TicksGame ?? 0;

            if (
                !pendingExpiration &&
                (
                    sourceCamp == null ||
                    sourceCamp.Destroyed ||
                    sourceCamp.Data == null ||
                    currentTick >= expirationTick
                )
            )
            {
                BeginExpiration();
            }

            if (!Destroyed)
            {
                base.TickInterval(delta);
            }
        }

        public override void Notify_CaravanFormed(Caravan caravan)
        {
            base.Notify_CaravanFormed(caravan);

            if (pendingExpiration)
            {
                CheckRemoveMapNow();
            }
        }

        public override void Notify_MyMapAboutToBeRemoved()
        {
            EnclaveExpeditionMapPopulator.CleanupTemporaryPawns(this);
            base.Notify_MyMapAboutToBeRemoved();
        }

        public override void Destroy()
        {
            if (Destroyed)
            {
                return;
            }

            if (HasMap)
            {
                pendingExpiration = true;
                CheckRemoveMapNow();
                return;
            }

            EnclaveExpeditionService.NotifySiteEnded(this);
            base.Destroy();
        }

        public override string GetInspectString()
        {
            string sourceName =
                sourceCamp?.Data?.Name ?? "Unknown enclave";
            string expiration = pendingExpiration
                ? "Pending safe departure"
                : EnclaveExpeditionUtility.FormatRemainingTime(
                    expirationTick
                );

            return
                "Origin: " + sourceName + "\n" +
                "Purpose: " +
                EnclaveExpeditionUtility.GetPurposeLabel(purpose) +
                "\nExpires in: " + expiration +
                (sourceCamp?.Data == null
                    ? string.Empty
                    : "\nExpedition Size: " +
                        EnclaveExpeditionUtility.GetSizeLabel(
                            sourceCamp.Data
                        ));
        }
    }
}
