using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace IdeologyExpandedEnclaves
{
    public sealed class FloatMenuOptionProvider_EnclaveSupplyRequest
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

            EnclaveQuestRequest request =
                EnclaveQuestService.GetActiveSupplyRequest(camp);

            if (request == null)
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
                "Deliver " +
                request.RequestedQuantity +
                " " +
                request.RequestedThingDef.label +
                " to " +
                (camp.Data?.Name ?? "the enclave");
            string failureReason;

            if (
                !EnclaveQuestService.SupplyDeliveryContactIsValid(
                    camp,
                    clickedPawn,
                    out failureReason
                )
            )
            {
                yield return new FloatMenuOption(
                    "Supply delivery unavailable",
                    delegate
                    {
                        Messages.Message(
                            failureReason,
                            clickedPawn,
                            MessageTypeDefOf.RejectInput
                        );
                    }
                );
                yield break;
            }

            int available =
                EnclaveQuestService.GetAvailableRequestedItems(camp);

            if (available < request.RequestedQuantity)
            {
                yield return new FloatMenuOption(
                    label +
                    " (carrying " +
                    available +
                    ")",
                    delegate
                    {
                        Messages.Message(
                            "The visiting group is carrying " +
                            available +
                            " of the required " +
                            request.RequestedQuantity +
                            " " +
                            request.RequestedThingDef.label +
                            ".",
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
                        EnclaveJobDefOf.IEE_DeliverEnclaveSupplies,
                        clickedPawn
                    );
                    job.playerForced = true;
                    colonist.jobs.TryTakeOrderedJob(job);
                }
            );
        }
    }

    public sealed class JobDriver_DeliverEnclaveSupplies : JobDriver
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
            yield return Toils_General.Do(OpenDeliveryConfirmation);
        }

        private void OpenDeliveryConfirmation()
        {
            Pawn leader = TargetPawnA;
            PilgrimCamp camp = pawn?.Map?.Parent as PilgrimCamp;
            string failureReason;

            if (
                !EnclaveQuestService.SupplyDeliveryContactIsValid(
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

            EnclaveDialogs.ConfirmSupplyRequestDelivery(camp, leader);
        }
    }
}
