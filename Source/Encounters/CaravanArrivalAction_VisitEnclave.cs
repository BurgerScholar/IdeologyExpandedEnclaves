using RimWorld;
using RimWorld.Planet;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public class CaravanArrivalAction_VisitEnclave : CaravanArrivalAction
    {
        private PilgrimCamp camp;

        public CaravanArrivalAction_VisitEnclave()
        {
        }

        public CaravanArrivalAction_VisitEnclave(PilgrimCamp camp)
        {
            this.camp = camp;
        }

        public override string Label =>
            EnclaveRelationshipUtility.IsLocallyHostile(camp)
                ? "Visit enclave (Warning: Hostile)"
                : "Visit enclave";

        public override string ReportString
        {
            get
            {
                string enclaveName =
                    camp?.Data?.Name ?? "the pilgrim camp";

                return
                    "Caravan traveling to visit " +
                    enclaveName +
                    "." +
                    (EnclaveRelationshipUtility.IsLocallyHostile(camp)
                        ? " Warning: this enclave is Hostile."
                        : string.Empty);
            }
        }

        public override void Arrived(Caravan caravan)
        {
            if (camp == null)
            {
                Log.Error(
                    "[Ideology Expanded: Enclaves] " +
                    "Arrival failed because the camp reference was missing."
                );

                return;
            }

            LongEventHandler.QueueLongEvent(
                delegate
                {
                    Map map =
                        EnclaveEncounterMapUtility.EnsureMapGenerated(camp);

                    if (camp.VisitingGroup == null)
                    {
                        camp.VisitingGroup = new EnclaveVisitingGroup();
                    }

                    camp.VisitingGroup.Capture(caravan);

                    Log.Message(
                        "[IEE] Captured " +
                        camp.VisitingGroup.Members.Count +
                        " visiting caravan members for " +
                        (camp.Data?.Name ?? "an enclave") +
                        "."
                    );

                    CaravanEnterMapUtility.Enter(
                        caravan,
                        map,
                        CaravanEnterMode.Edge,
                        CaravanDropInventoryMode.DoNotDrop,
                        draftColonists: false
                    );

                    EnclaveLocalHostilityService.UpdateCampCombatState(
                        camp,
                        notifyPlayer: true
                    );

                    CameraJumper.TryJump(map.Center, map);

                    if (!EnclaveRelationshipUtility.IsLocallyHostile(camp))
                    {
                        Messages.Message(
                            "Your caravan has entered " +
                            (camp.Data?.Name ?? "the pilgrim camp") +
                            ".",
                            MessageTypeDefOf.PositiveEvent
                        );
                    }
                },
                "GeneratingMap",
                doAsynchronously: false,
                exceptionHandler: null
            );
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_References.Look(
                ref camp,
                "pilgrimCamp"
            );
        }
    }
}
