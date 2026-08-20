using System;
using System.Collections.Generic;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public sealed class EnclaveInterventionMapComponent : MapComponent
    {
        private const int ProcessingIntervalTicks = 60;

        private List<EnclaveInterventionRecord> records =
            new List<EnclaveInterventionRecord>();
        private int nextRecordId = 1;

        public Map ParentMap => map;
        public IReadOnlyList<EnclaveInterventionRecord> Records =>
            records;

        public EnclaveInterventionMapComponent(Map map)
            : base(map)
        {
        }

        public EnclaveInterventionRecord RegisterRaid(
            IEnumerable<Pawn> raidPawns
        )
        {
            EnsureCollection();

            foreach (EnclaveInterventionRecord existing in records)
            {
                if (existing?.SharesRaidPawn(raidPawns) == true)
                {
                    return existing;
                }
            }

            int currentTick = Find.TickManager?.TicksGame ?? 0;
            EnclaveInterventionRecord record =
                new EnclaveInterventionRecord(
                    nextRecordId++,
                    Rand.Int,
                    currentTick,
                    raidPawns
                );

            records.Add(record);
            return record;
        }

        public EnclaveInterventionRecord GetRecord(int id)
        {
            if (records == null)
            {
                return null;
            }

            return records.Find(record => record?.Id == id);
        }

        public EnclaveInterventionRecord GetLatestActiveRaidRecord()
        {
            if (records == null)
            {
                return null;
            }

            EnclaveInterventionRecord latest = null;

            foreach (EnclaveInterventionRecord record in records)
            {
                if (
                    record == null ||
                    !EnclaveInterventionService.IsOriginalRaidActive(
                        map,
                        record
                    )
                )
                {
                    continue;
                }

                if (latest == null || record.Id > latest.Id)
                {
                    latest = record;
                }
            }

            return latest;
        }

        public void RemoveRecord(EnclaveInterventionRecord record)
        {
            records?.Remove(record);
        }

        public override void ExposeData()
        {
            base.ExposeData();

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                PrepareReferencesForSave();
            }

            Scribe_Collections.Look(
                ref records,
                "enclaveRaidInterventions",
                LookMode.Deep
            );
            Scribe_Values.Look(
                ref nextRecordId,
                "nextEnclaveRaidInterventionId",
                1
            );

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                EnsureCollection();
                records.RemoveAll(record => record == null);

                int highestId = 0;

                foreach (EnclaveInterventionRecord record in records)
                {
                    highestId = Math.Max(highestId, record.Id);
                }

                nextRecordId = Math.Max(
                    Math.Max(1, nextRecordId),
                    highestId + 1
                );
            }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            int currentTick = Find.TickManager?.TicksGame ?? 0;

            if (
                records == null ||
                records.Count == 0 ||
                (currentTick + map.uniqueID) % ProcessingIntervalTicks != 0
            )
            {
                return;
            }

            EnclaveInterventionService.ProcessRecords(
                this,
                currentTick
            );
        }

        private void EnsureCollection()
        {
            if (records == null)
            {
                records = new List<EnclaveInterventionRecord>();
            }
        }

        private void PrepareReferencesForSave()
        {
            EnsureCollection();

            foreach (EnclaveInterventionRecord record in records)
            {
                if (record == null)
                {
                    continue;
                }

                record.ClearDestroyedSourceReference();
                record.PrunePartyReferences(map);

                if (
                    !EnclaveInterventionService.IsOriginalRaidActive(
                        map,
                        record
                    )
                )
                {
                    record.ClearRaidPawns();
                }
            }

            records.RemoveAll(
                record =>
                    record == null ||
                    (
                        record.State ==
                            EnclaveRaidInterventionState.NoIntervention &&
                        !EnclaveInterventionService
                            .IsOriginalRaidActive(map, record)
                    )
            );
        }
    }
}
