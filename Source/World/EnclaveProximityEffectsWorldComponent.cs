using RimWorld.Planet;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public sealed class EnclaveProximityEffectsWorldComponent
        : WorldComponent
    {
        private int nextPulseTick = -1;

        public int NextPulseTick => nextPulseTick;

        public EnclaveProximityEffectsWorldComponent(World world)
            : base(world)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(
                ref nextPulseTick,
                "nextEnclaveProximityPulseTick",
                -1
            );
        }

        public override void WorldComponentTick()
        {
            int currentTick = Find.TickManager?.TicksGame ?? 0;

            if (nextPulseTick < 0)
            {
                nextPulseTick =
                    currentTick +
                    EnclaveProximityProfileUtility.PulseIntervalTicks;
                return;
            }

            if (currentTick < nextPulseTick)
            {
                return;
            }

            // Schedule from the current tick so an overdue save receives at
            // most one pulse instead of replaying every missed interval.
            nextPulseTick =
                currentTick +
                EnclaveProximityProfileUtility.PulseIntervalTicks;

            EnclaveProximityEffectsService.ApplyPulse();
        }
    }
}
