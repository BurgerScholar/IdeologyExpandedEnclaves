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

        public override string Label => "Visit enclave";

        public override string ReportString
        {
            get
            {
                string enclaveName =
                    camp?.Data?.Name ?? "the pilgrim camp";

                return "Caravan traveling to visit " + enclaveName + ".";
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
                    Map map = camp.Map;

                    if (map == null)
                    {
                        map = MapGenerator.GenerateMap(
                            new IntVec3(120, 1, 120),
                            camp,
                            camp.MapGeneratorDef,
                            null,
                            null,
                            false,
                            false
                        );
                        EnclaveMapPopulator.PopulateNewCamp(map, camp);
                    }

                    CaravanEnterMapUtility.Enter(
                        caravan,
                        map,
                        CaravanEnterMode.Edge,
                        CaravanDropInventoryMode.DoNotDrop,
                        draftColonists: false
                    );

                    CameraJumper.TryJump(map.Center, map);

                    Messages.Message(
                        "Your caravan has entered " +
                        (camp.Data?.Name ?? "the pilgrim camp") +
                        ".",
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

            Scribe_References.Look(
                ref camp,
                "pilgrimCamp"
            );
        }
    }
}