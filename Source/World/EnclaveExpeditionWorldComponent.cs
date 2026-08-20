using System;
using RimWorld.Planet;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public sealed class EnclaveExpeditionWorldComponent
        : WorldComponent
    {
        private int nextEvaluationTick = -1;
        private int nextExpeditionId = 1;

        public int NextEvaluationTick => nextEvaluationTick;

        public EnclaveExpeditionWorldComponent(World world)
            : base(world)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(
                ref nextEvaluationTick,
                "nextEnclaveExpeditionEvaluationTick",
                -1
            );
            Scribe_Values.Look(
                ref nextExpeditionId,
                "nextEnclaveExpeditionId",
                1
            );

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                nextExpeditionId = Math.Max(
                    nextExpeditionId,
                    EnclaveExpeditionService.GetNextSafeExpeditionId()
                );

                LongEventHandler.ExecuteWhenFinished(
                    EnclaveExpeditionService.ReconcileAll
                );
            }
        }

        public int AllocateExpeditionId()
        {
            int allocated = Math.Max(1, nextExpeditionId);
            nextExpeditionId = allocated + 1;
            return allocated;
        }

        public override void WorldComponentTick()
        {
            int currentTick = Find.TickManager?.TicksGame ?? 0;

            if (nextEvaluationTick < 0)
            {
                nextEvaluationTick =
                    currentTick +
                    EnclaveExpeditionUtility.DailyEvaluationTicks;
                return;
            }

            if (currentTick < nextEvaluationTick)
            {
                return;
            }

            int scheduledEvaluationTick = nextEvaluationTick;
            nextEvaluationTick =
                currentTick +
                EnclaveExpeditionUtility.DailyEvaluationTicks;

            EnclaveExpeditionService.EvaluateScheduledCycle(
                scheduledEvaluationTick,
                currentTick
            );
        }
    }
}
