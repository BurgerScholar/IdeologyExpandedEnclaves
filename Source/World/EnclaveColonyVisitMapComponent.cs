using System.Collections.Generic;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public sealed class EnclaveColonyVisitMapComponent : MapComponent
    {
        private const int ProcessingIntervalTicks = 60;

        private List<EnclaveColonyVisitRecord> visits =
            new List<EnclaveColonyVisitRecord>();

        public IReadOnlyList<EnclaveColonyVisitRecord> Visits => visits;
        public Map ParentMap => map;

        public EnclaveColonyVisitMapComponent(Map map)
            : base(map)
        {
        }

        public void AddVisit(EnclaveColonyVisitRecord visit)
        {
            EnsureCollection();

            if (visit != null && !visits.Contains(visit))
            {
                visits.Add(visit);
            }
        }

        public void RemoveVisit(EnclaveColonyVisitRecord visit)
        {
            visits?.Remove(visit);
        }

        public override void ExposeData()
        {
            base.ExposeData();

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                EnsureCollection();

                foreach (EnclaveColonyVisitRecord visit in visits)
                {
                    visit?.ClearDestroyedSourceReference();
                }
            }

            Scribe_Collections.Look(
                ref visits,
                "enclaveColonyVisits",
                LookMode.Deep
            );

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                EnsureCollection();
                visits.RemoveAll(visit => visit == null);
            }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            int currentTick = Find.TickManager?.TicksGame ?? 0;

            if (
                visits == null ||
                visits.Count == 0 ||
                (currentTick + map.uniqueID) %
                    ProcessingIntervalTicks != 0
            )
            {
                return;
            }

            EnclaveColonyVisitService.ProcessVisits(
                this,
                currentTick
            );
        }

        private void EnsureCollection()
        {
            if (visits == null)
            {
                visits = new List<EnclaveColonyVisitRecord>();
            }
        }
    }
}
