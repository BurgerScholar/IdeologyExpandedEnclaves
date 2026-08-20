using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace IdeologyExpandedEnclaves
{
    public class LordJob_EnclaveIntervention : LordJob
    {
        private int recordId;
        private EnclaveInterventionSide side;

        public int RecordId => recordId;
        public EnclaveInterventionSide Side => side;

        public LordJob_EnclaveIntervention()
        {
        }

        public LordJob_EnclaveIntervention(
            int recordId,
            EnclaveInterventionSide side
        )
        {
            this.recordId = recordId;
            this.side = side;
        }

        public override StateGraph CreateGraph()
        {
            StateGraph graph = new StateGraph();
            graph.AddToil(new LordToil_EnclaveIntervention());
            return graph;
        }

        public override bool ValidateAttackTarget(
            Pawn searcher,
            Thing target
        )
        {
            return EnclaveInterventionService.IsValidCombatTarget(
                searcher,
                target as Pawn,
                recordId
            );
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref recordId, "raidRecordId", 0);
            Scribe_Values.Look(
                ref side,
                "interventionSide",
                EnclaveInterventionSide.None
            );
        }
    }

    public class LordToil_EnclaveIntervention : LordToil
    {
        public override bool AllowSatisfyLongNeeds => false;
        public override bool ForceHighStoryDanger => true;

        public override void UpdateAllDuties()
        {
            foreach (Pawn pawn in lord.ownedPawns)
            {
                if (pawn?.mindState != null)
                {
                    pawn.mindState.duty = new PawnDuty(
                        EnclaveDutyDefOf.IEE_EnclaveInterventionCombat
                    );
                }
            }
        }
    }

    public class JobGiver_EnclaveInterventionCombat
        : JobGiver_AIFightEnemies
    {
        public JobGiver_EnclaveInterventionCombat()
        {
            targetAcquireRadius = 9999f;
            targetKeepRadius = 9999f;
            chaseTarget = true;
            allowTurrets = false;
            ignoreNonCombatants = false;
            humanlikesOnly = false;
        }

        protected override Thing FindAttackTarget(Pawn pawn)
        {
            LordJob_EnclaveIntervention job =
                pawn?.GetLord()?.LordJob as
                    LordJob_EnclaveIntervention;

            return job == null
                ? null
                : EnclaveInterventionService.FindClosestCombatTarget(
                    pawn,
                    job.RecordId
                );
        }

        protected override bool ExtraTargetValidator(
            Pawn pawn,
            Thing target
        )
        {
            LordJob_EnclaveIntervention job =
                pawn?.GetLord()?.LordJob as
                    LordJob_EnclaveIntervention;

            return
                job != null &&
                EnclaveInterventionService.IsValidCombatTarget(
                    pawn,
                    target as Pawn,
                    job.RecordId
                );
        }

        protected override bool ShouldLoseTarget(Pawn pawn)
        {
            LordJob_EnclaveIntervention job =
                pawn?.GetLord()?.LordJob as
                    LordJob_EnclaveIntervention;

            return
                job == null ||
                !EnclaveInterventionService.IsValidCombatTarget(
                    pawn,
                    pawn?.mindState?.enemyTarget as Pawn,
                    job.RecordId
                );
        }
    }
}
