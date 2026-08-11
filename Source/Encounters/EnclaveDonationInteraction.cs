using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace IdeologyExpandedEnclaves
{
    public class FloatMenuOptionProvider_EnclaveDonation
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
                camp.PawnRoles?.GetPawn(EnclavePawnRole.Leader) !=
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
                "Donate silver to " +
                (camp.Data?.Name ?? "the enclave");

            if (EnclaveRelationshipUtility.IsLocallyHostile(camp))
            {
                yield return new FloatMenuOption(
                    "Donations unavailable (Hostile enclave)",
                    delegate
                    {
                        Messages.Message(
                            "Donations unavailable. This enclave is " +
                            "hostile toward your visiting group.",
                            clickedPawn,
                            MessageTypeDefOf.RejectInput
                        );
                    }
                );
                yield break;
            }

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
                        EnclaveJobDefOf.IEE_DonateSilverToEnclave,
                        clickedPawn
                    );
                    job.playerForced = true;
                    colonist.jobs.TryTakeOrderedJob(job);
                }
            );
        }
    }

    public class JobDriver_DonateSilverToEnclave : JobDriver
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
            yield return Toils_General.Do(OpenDonationDialog);
        }

        private void OpenDonationDialog()
        {
            Pawn leader = TargetPawnA;
            PilgrimCamp camp = pawn?.Map?.Parent as PilgrimCamp;
            string failureReason;

            if (
                !EnclaveDonationService.DonationContactIsValid(
                    camp,
                    leader,
                    out failureReason
                )
            )
            {
                Messages.Message(
                    failureReason,
                    MessageTypeDefOf.RejectInput
                );
                return;
            }

            EnclaveDialogs.OpenSilverDonation(camp, leader);
        }
    }
}
