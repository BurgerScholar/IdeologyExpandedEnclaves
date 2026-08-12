using RimWorld.Planet;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public sealed class EnclaveNeedsWorldComponent : WorldComponent
    {
        private int nextNeedsPulseTick = -1;

        public int NextNeedsPulseTick => nextNeedsPulseTick;

        public EnclaveNeedsWorldComponent(World world)
            : base(world)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(
                ref nextNeedsPulseTick,
                "nextEnclaveNeedsPulseTick",
                -1
            );
        }

        public override void WorldComponentTick()
        {
            int currentTick = Find.TickManager?.TicksGame ?? 0;

            if (nextNeedsPulseTick < 0)
            {
                nextNeedsPulseTick =
                    currentTick +
                    EnclaveNeedsService.PulseIntervalTicks;
                return;
            }

            if (currentTick < nextNeedsPulseTick)
            {
                return;
            }

            // An overdue save receives one current evaluation rather than a
            // burst of simulated missed quadrums.
            nextNeedsPulseTick =
                currentTick +
                EnclaveNeedsService.PulseIntervalTicks;

            EnclaveNeedsService.EvaluateAllEnclaves();
        }
    }
}
