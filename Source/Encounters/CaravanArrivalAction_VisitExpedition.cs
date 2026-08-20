using RimWorld;
using RimWorld.Planet;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public sealed class CaravanArrivalAction_VisitExpedition
        : CaravanArrivalAction
    {
        private EnclaveExpeditionSite site;

        public CaravanArrivalAction_VisitExpedition()
        {
        }

        public CaravanArrivalAction_VisitExpedition(
            EnclaveExpeditionSite site
        )
        {
            this.site = site;
        }

        public override string Label =>
            "Visit " + (site?.Label ?? "expedition site");

        public override string ReportString =>
            "Caravan traveling to visit " +
            (site?.Label ?? "an enclave expedition site") +
            ".";

        public override void Arrived(Caravan caravan)
        {
            string reason = null;

            if (site == null || !site.CanPlayerVisit(out reason))
            {
                Messages.Message(
                    reason ?? "The expedition site is unavailable.",
                    MessageTypeDefOf.RejectInput
                );
                return;
            }

            LongEventHandler.QueueLongEvent(
                delegate
                {
                    Map map = EnclaveExpeditionEncounterMapUtility
                        .EnsureMapGenerated(site);

                    if (map == null)
                    {
                        Messages.Message(
                            "The expedition map could not be generated.",
                            MessageTypeDefOf.RejectInput
                        );
                        return;
                    }

                    site.VisitingGroup.Capture(caravan);

                    CaravanEnterMapUtility.Enter(
                        caravan,
                        map,
                        CaravanEnterMode.Edge,
                        CaravanDropInventoryMode.DoNotDrop,
                        draftColonists: false
                    );

                    CameraJumper.TryJump(map.Center, map);
                    Messages.Message(
                        "Your caravan has entered " + site.Label + ".",
                        MessageTypeDefOf.PositiveEvent
                    );
                },
                "GeneratingMap",
                doAsynchronously: false,
                exceptionHandler: null
            );
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref site, "expeditionSite");
        }
    }
}
