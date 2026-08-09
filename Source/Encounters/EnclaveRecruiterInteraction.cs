using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace IdeologyExpandedEnclaves
{
    [DefOf]
    public static class EnclaveJobDefOf
    {
        public static JobDef IEE_ViewEnclaveRecruitmentCandidates;

        static EnclaveJobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(
                typeof(EnclaveJobDefOf)
            );
        }
    }

    public class FloatMenuOptionProvider_EnclaveRecruiter
        : FloatMenuOptionProvider
    {
        protected override bool Drafted => true;
        protected override bool Undrafted => true;
        protected override bool Multiselect => false;

        public override IEnumerable<FloatMenuOption> GetOptionsFor(
            Pawn clickedPawn,
            FloatMenuContext context
        )
        {
            PilgrimCamp camp = clickedPawn?.Map?.Parent as PilgrimCamp;

            if (
                camp == null ||
                camp.PawnRoles?.GetPawn(EnclavePawnRole.Recruiter) !=
                    clickedPawn
            )
            {
                yield break;
            }

            Pawn colonist = context?.FirstSelectedPawn;

            if (
                colonist == null ||
                !colonist.IsColonistPlayerControlled ||
                !colonist.RaceProps.Humanlike ||
                colonist.Map != clickedPawn.Map
            )
            {
                yield break;
            }

            string label =
                "Ask " +
                clickedPawn.LabelShort +
                " about recruitment";

            if (
                !colonist.CanReach(
                    clickedPawn,
                    PathEndMode.Touch,
                    Danger.Deadly
                )
            )
            {
                yield return new FloatMenuOption(
                    label + ": No path",
                    null
                );

                yield break;
            }

            yield return new FloatMenuOption(
                label,
                delegate
                {
                    Job job = JobMaker.MakeJob(
                        EnclaveJobDefOf
                            .IEE_ViewEnclaveRecruitmentCandidates,
                        clickedPawn
                    );
                    job.playerForced = true;
                    colonist.jobs.TryTakeOrderedJob(job);
                }
            );
        }
    }

    public class JobDriver_ViewEnclaveRecruitmentCandidates
        : JobDriver
    {
        public override bool TryMakePreToilReservations(
            bool errorOnFailed
        )
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);

            yield return Toils_Goto.GotoThing(
                TargetIndex.A,
                PathEndMode.Touch
            );
            yield return Toils_General.Do(OpenCandidateBrowser);
        }

        private void OpenCandidateBrowser()
        {
            Pawn recruiter = TargetPawnA;
            PilgrimCamp camp = pawn?.Map?.Parent as PilgrimCamp;

            if (
                camp == null ||
                recruiter == null ||
                recruiter.Dead ||
                !recruiter.Spawned ||
                camp.PawnRoles?.GetPawn(EnclavePawnRole.Recruiter) !=
                    recruiter
            )
            {
                Messages.Message(
                    "The enclave Recruiter is no longer available.",
                    MessageTypeDefOf.RejectInput
                );

                return;
            }

            EnclaveDialogs.OpenRecruitmentCandidates(
                camp,
                recruiter
            );
        }
    }
}
