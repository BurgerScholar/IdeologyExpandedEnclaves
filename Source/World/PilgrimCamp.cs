using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public class PilgrimCamp : MapParent
    {
        public EnclaveData Data;
        public EnclavePawnRoleAssignments PawnRoles =
            new EnclavePawnRoleAssignments();

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Deep.Look(ref Data, "enclaveData");
            Scribe_Deep.Look(ref PawnRoles, "pawnRoles");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (Data == null)
                {
                    Data = EnclaveGenerator.Generate();
                }

                if (PawnRoles == null)
                {
                    PawnRoles = new EnclavePawnRoleAssignments();
                }
            }
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(
            Caravan caravan
        )
        {
            foreach (FloatMenuOption option in base.GetFloatMenuOptions(caravan))
            {
                yield return option;
            }

            EnsureDataExists();

            string enclaveName = Data?.Name ?? "the pilgrim camp";

            yield return new FloatMenuOption(
                "Visit " + enclaveName,
                delegate
                {
                    caravan.pather.StartPath(
                        Tile,
                        new CaravanArrivalAction_VisitEnclave(this),
                        repathImmediately: true
                    );
                }
            );
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            yield return new Command_Action
            {
                defaultLabel = "Inspect enclave",
                defaultDesc = "View information about this ideological community.",
                icon = BaseContent.BadTex,
                action = ShowEnclaveInformation
            };

            yield return new Command_Action
            {
                defaultLabel = "Visit enclave",
                defaultDesc = "Prepare to send a caravan into the pilgrim camp.",
                icon = BaseContent.BadTex,
                action = delegate
                {
                    Messages.Message(
                        "Create a caravan, then right-click this camp on the world map.",
                        MessageTypeDefOf.NeutralEvent
                    );
                }
            };

            yield return new Command_Action
            {
                defaultLabel = "Trade",
                defaultDesc = "Attempt to trade with the pilgrims.",
                icon = BaseContent.BadTex,
                action = delegate
                {
                    Messages.Message(
                        "The pilgrims are not ready to trade yet.",
                        MessageTypeDefOf.RejectInput
                    );
                }
            };

            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Remove camp",
                    defaultDesc = "Remove this test pilgrim camp from the world.",
                    icon = BaseContent.BadTex,
                    action = RemoveCamp
                };
            }
        }

        private void ShowEnclaveInformation()
        {
            EnsureDataExists();

            string hospitality = Data.Friendly
                ? "Friendly"
                : "Unfriendly";

            Find.WindowStack.Add(
                new Dialog_MessageBox(
                    Data.Name + "\n\n" +
                    "Type: Pilgrim Camp\n" +
                    "Leader: " + Data.Leader + "\n" +
                    "Population: " + Data.Population + "\n" +
                    "Ideology: " + Data.Ideology + "\n" +
                    "Hospitality: " + hospitality
                )
            );
        }

        private void EnsureDataExists()
        {
            if (Data == null)
            {
                Data = EnclaveGenerator.Generate();
            }
        }

        private void RemoveCamp()
        {
            Find.WorldObjects.Remove(this);

            Messages.Message(
                "The test pilgrim camp was removed.",
                MessageTypeDefOf.NeutralEvent
            );
        }
    }
}
