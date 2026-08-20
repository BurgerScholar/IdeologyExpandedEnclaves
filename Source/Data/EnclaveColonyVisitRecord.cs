using System;
using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public enum EnclaveColonyVisitState
    {
        Active,
        Departing,
        DetachedCleanup
    }

    public sealed class EnclaveColonyVisitRecord : IExposable
    {
        private int expeditionId;
        private EnclaveExpeditionPurpose purpose;
        private PilgrimCamp sourceCamp;
        private Settlement destination;
        private int startTick;
        private int departureTick;
        private int departureStartedTick = -1;
        private int sourcePopulationAtStart;
        private EnclaveColonyVisitState state;
        private List<Pawn> visitors = new List<Pawn>();
        private Pawn trader;

        public int ExpeditionId => expeditionId;
        public EnclaveExpeditionPurpose Purpose => purpose;
        public PilgrimCamp SourceCamp => sourceCamp;
        public Settlement Destination => destination;
        public int StartTick => startTick;
        public int DepartureTick => departureTick;
        public int DepartureStartedTick => departureStartedTick;
        public int SourcePopulationAtStart => sourcePopulationAtStart;
        public EnclaveColonyVisitState State => state;
        public IReadOnlyList<Pawn> Visitors => visitors;
        public Pawn Trader => trader;
        public bool IsSourceExpeditionActive =>
            state != EnclaveColonyVisitState.DetachedCleanup;

        public EnclaveColonyVisitRecord()
        {
        }

        public EnclaveColonyVisitRecord(
            int expeditionId,
            EnclaveExpeditionPurpose purpose,
            PilgrimCamp sourceCamp,
            Settlement destination,
            int startTick,
            int departureTick,
            IEnumerable<Pawn> visitors,
            Pawn trader
        )
        {
            this.expeditionId = Math.Max(1, expeditionId);
            this.purpose = purpose;
            this.sourceCamp = sourceCamp;
            this.destination = destination;
            this.startTick = Math.Max(0, startTick);
            this.departureTick = Math.Max(
                this.startTick + 1,
                departureTick
            );
            sourcePopulationAtStart = sourceCamp?.Data?.Population ?? 0;
            state = EnclaveColonyVisitState.Active;
            AddUniqueVisitors(visitors);
            this.trader = this.visitors.Contains(trader) ? trader : null;
        }

        public bool ContainsVisitor(Pawn pawn)
        {
            return pawn != null && visitors?.Contains(pawn) == true;
        }

        public void BeginDeparture(int currentTick)
        {
            if (state != EnclaveColonyVisitState.Active)
            {
                return;
            }

            state = EnclaveColonyVisitState.Departing;
            departureStartedTick = Math.Max(0, currentTick);
        }

        public void DetachFromSource()
        {
            state = EnclaveColonyVisitState.DetachedCleanup;
        }

        public void RemoveVisitor(Pawn pawn)
        {
            if (pawn == null || visitors == null)
            {
                return;
            }

            visitors.Remove(pawn);

            if (trader == pawn)
            {
                trader = null;
            }
        }

        public void ClearDestroyedSourceReference()
        {
            if (sourceCamp?.Destroyed == true)
            {
                sourceCamp = null;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref expeditionId, "expeditionId", 0);
            Scribe_Values.Look(
                ref purpose,
                "purpose",
                EnclaveExpeditionPurpose.Relief
            );
            Scribe_References.Look(ref sourceCamp, "sourceCamp");
            Scribe_References.Look(ref destination, "destination");
            Scribe_Values.Look(ref startTick, "startTick", 0);
            Scribe_Values.Look(
                ref departureTick,
                "departureTick",
                0
            );
            Scribe_Values.Look(
                ref departureStartedTick,
                "departureStartedTick",
                -1
            );
            Scribe_Values.Look(
                ref sourcePopulationAtStart,
                "sourcePopulationAtStart",
                0
            );
            Scribe_Values.Look(
                ref state,
                "state",
                EnclaveColonyVisitState.Active
            );
            Scribe_Collections.Look(
                ref visitors,
                "visitors",
                LookMode.Reference
            );
            Scribe_References.Look(ref trader, "trader");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (
                    !Enum.IsDefined(
                        typeof(EnclaveColonyVisitState),
                        state
                    )
                )
                {
                    state = EnclaveColonyVisitState.Departing;
                }

                startTick = Math.Max(0, startTick);
                departureTick = Math.Max(startTick + 1, departureTick);
                sourcePopulationAtStart = Math.Max(
                    0,
                    sourcePopulationAtStart
                );
                EnsureVisitors();

                HashSet<Pawn> seen = new HashSet<Pawn>();
                visitors.RemoveAll(
                    pawn =>
                        pawn == null ||
                        pawn.Destroyed ||
                        !seen.Add(pawn)
                );

                if (trader != null && !visitors.Contains(trader))
                {
                    trader = null;
                }

                EnclaveTradeService.SuppressVanillaTradeOption(trader);
            }
        }

        private void AddUniqueVisitors(IEnumerable<Pawn> pawns)
        {
            EnsureVisitors();

            if (pawns == null)
            {
                return;
            }

            foreach (Pawn pawn in pawns)
            {
                if (
                    pawn != null &&
                    !pawn.Destroyed &&
                    !visitors.Contains(pawn)
                )
                {
                    visitors.Add(pawn);
                }
            }
        }

        private void EnsureVisitors()
        {
            if (visitors == null)
            {
                visitors = new List<Pawn>();
            }
        }
    }
}
