using System;
using System.Collections.Generic;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public enum EnclaveInterventionSide
    {
        None,
        Friendly,
        Hostile
    }

    public enum EnclaveRaidInterventionState
    {
        PendingEvaluation,
        NoIntervention,
        Active,
        Exiting
    }

    public sealed class EnclaveInterventionRecord : IExposable
    {
        private int id;
        private int rollSeed;
        private int registeredTick;
        private EnclaveRaidInterventionState state;
        private EnclaveInterventionSide side;
        private PilgrimCamp sourceCamp;
        private string sourceEnclaveName;
        private List<Pawn> raidPawns = new List<Pawn>();
        private List<Pawn> partyPawns = new List<Pawn>();

        public int Id => id;
        public int RollSeed => rollSeed;
        public int RegisteredTick => registeredTick;
        public EnclaveRaidInterventionState State
        {
            get => state;
            set => state = value;
        }

        public EnclaveInterventionSide Side => side;
        public PilgrimCamp SourceCamp => sourceCamp;
        public string SourceEnclaveName =>
            sourceEnclaveName ?? sourceCamp?.Data?.Name ?? "an enclave";
        public IReadOnlyList<Pawn> RaidPawns => raidPawns;
        public IReadOnlyList<Pawn> PartyPawns => partyPawns;

        public EnclaveInterventionRecord()
        {
        }

        public EnclaveInterventionRecord(
            int id,
            int rollSeed,
            int registeredTick,
            IEnumerable<Pawn> raidPawns
        )
        {
            this.id = id;
            this.rollSeed = rollSeed;
            this.registeredTick = registeredTick;
            state = EnclaveRaidInterventionState.PendingEvaluation;
            AddUniquePawns(this.raidPawns, raidPawns);
        }

        public void Activate(
            PilgrimCamp camp,
            EnclaveInterventionSide interventionSide,
            IEnumerable<Pawn> pawns
        )
        {
            sourceCamp = camp;
            sourceEnclaveName = camp?.Data?.Name ?? "an enclave";
            side = interventionSide;
            partyPawns.Clear();
            AddUniquePawns(partyPawns, pawns);
            state = EnclaveRaidInterventionState.Active;
        }

        public bool SharesRaidPawn(IEnumerable<Pawn> pawns)
        {
            if (pawns == null || raidPawns == null)
            {
                return false;
            }

            foreach (Pawn pawn in pawns)
            {
                if (pawn != null && raidPawns.Contains(pawn))
                {
                    return true;
                }
            }

            return false;
        }

        public bool ContainsRaidPawn(Pawn pawn)
        {
            return pawn != null && raidPawns?.Contains(pawn) == true;
        }

        public bool ContainsPartyPawn(Pawn pawn)
        {
            return pawn != null && partyPawns?.Contains(pawn) == true;
        }

        public void ClearRaidPawns()
        {
            raidPawns?.Clear();
        }

        public void PrunePartyReferences(Map map)
        {
            if (partyPawns == null)
            {
                return;
            }

            partyPawns.RemoveAll(
                pawn =>
                    pawn == null ||
                    pawn.Destroyed ||
                    pawn.Dead ||
                    pawn.MapHeld != map ||
                    pawn.IsPrisonerOfColony ||
                    !EnclaveFactionUtility.IsEnclaveFaction(pawn.Faction)
            );
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
            Scribe_Values.Look(ref id, "id", 0);
            Scribe_Values.Look(ref rollSeed, "rollSeed", 0);
            Scribe_Values.Look(
                ref registeredTick,
                "registeredTick",
                0
            );
            Scribe_Values.Look(
                ref state,
                "state",
                EnclaveRaidInterventionState.PendingEvaluation
            );
            Scribe_Values.Look(
                ref side,
                "side",
                EnclaveInterventionSide.None
            );
            Scribe_References.Look(ref sourceCamp, "sourceCamp");
            Scribe_Values.Look(
                ref sourceEnclaveName,
                "sourceEnclaveName"
            );
            Scribe_Collections.Look(
                ref raidPawns,
                "raidPawns",
                LookMode.Reference
            );
            Scribe_Collections.Look(
                ref partyPawns,
                "partyPawns",
                LookMode.Reference
            );

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (
                    !Enum.IsDefined(
                        typeof(EnclaveRaidInterventionState),
                        state
                    )
                )
                {
                    state = EnclaveRaidInterventionState.NoIntervention;
                }

                if (
                    !Enum.IsDefined(
                        typeof(EnclaveInterventionSide),
                        side
                    )
                )
                {
                    side = EnclaveInterventionSide.None;
                }

                EnsureCollections();
                RemoveInvalidAndDuplicatePawns(raidPawns);
                RemoveInvalidAndDuplicatePawns(partyPawns);
            }
        }

        private void EnsureCollections()
        {
            if (raidPawns == null)
            {
                raidPawns = new List<Pawn>();
            }

            if (partyPawns == null)
            {
                partyPawns = new List<Pawn>();
            }
        }

        private static void AddUniquePawns(
            List<Pawn> destination,
            IEnumerable<Pawn> pawns
        )
        {
            if (destination == null || pawns == null)
            {
                return;
            }

            foreach (Pawn pawn in pawns)
            {
                if (
                    pawn != null &&
                    !pawn.Destroyed &&
                    !destination.Contains(pawn)
                )
                {
                    destination.Add(pawn);
                }
            }
        }

        private static void RemoveInvalidAndDuplicatePawns(
            List<Pawn> pawns
        )
        {
            if (pawns == null)
            {
                return;
            }

            HashSet<Pawn> seen = new HashSet<Pawn>();

            pawns.RemoveAll(
                pawn =>
                    pawn == null ||
                    pawn.Destroyed ||
                    !seen.Add(pawn)
            );
        }
    }
}
