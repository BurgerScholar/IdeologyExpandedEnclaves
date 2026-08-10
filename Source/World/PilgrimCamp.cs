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
        public EnclaveRecruitmentCandidates RecruitmentCandidates =
            new EnclaveRecruitmentCandidates();

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Deep.Look(ref Data, "enclaveData");
            Scribe_Deep.Look(ref PawnRoles, "pawnRoles");
            Scribe_Deep.Look(
                ref RecruitmentCandidates,
                "recruitmentCandidates"
            );

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

                if (RecruitmentCandidates == null)
                {
                    RecruitmentCandidates =
                        new EnclaveRecruitmentCandidates();
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
                defaultDesc =
                    "Locate the enclave Trader and trade through " +
                    "the normal pawn interaction.",
                icon = BaseContent.BadTex,
                action = HandleTradeGizmo
            };

            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Set reputation",
                    defaultDesc =
                        "Set this enclave's reputation to a " +
                        "representative value for tier testing.",
                    icon = BaseContent.BadTex,
                    action = ShowDevReputationMenu
                };

                yield return new Command_Action
                {
                    defaultLabel = "DEV: Remove camp",
                    defaultDesc = "Remove this test pilgrim camp from the world.",
                    icon = BaseContent.BadTex,
                    action = RemoveCamp
                };
            }
        }

        private void ShowDevReputationMenu()
        {
            EnsureDataExists();

            List<FloatMenuOption> options =
                new List<FloatMenuOption>
                {
                    CreateDevReputationOption("Hostile", -50),
                    CreateDevReputationOption("Wary", -10),
                    CreateDevReputationOption("Neutral", 0),
                    CreateDevReputationOption("Friendly", 30),
                    CreateDevReputationOption("Trusted", 60),
                    CreateDevReputationOption("Revered", 90)
                };

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private FloatMenuOption CreateDevReputationOption(
            string tierLabel,
            int value
        )
        {
            return new FloatMenuOption(
                tierLabel + ": " + value,
                delegate
                {
                    SetDevReputation(value);
                }
            );
        }

        private void SetDevReputation(int value)
        {
            EnsureDataExists();

            Data.SetReputation(
                value,
                "developer reputation test control"
            );

            Messages.Message(
                "Enclave reputation set to " +
                Data.Reputation +
                " — " +
                Data.ReputationTierLabel +
                ".",
                MessageTypeDefOf.NeutralEvent
            );
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
                    "Reputation: " +
                    Data.Reputation +
                    " — " +
                    Data.ReputationTierLabel +
                    "\n" +
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

        private void HandleTradeGizmo()
        {
            Map map = Map;

            if (map == null)
            {
                Messages.Message(
                    "Visit the enclave with a caravan, then speak " +
                    "with its designated Trader to trade.",
                    MessageTypeDefOf.NeutralEvent
                );

                return;
            }

            Pawn trader = PawnRoles?.GetPawn(EnclavePawnRole.Trader);

            if (
                trader != null &&
                trader.Spawned &&
                trader.Map == map
            )
            {
                CameraJumper.TryJump(trader.Position, map);
                Messages.Message(
                    "Enclave Trader located: " +
                    trader.LabelShort +
                    ". Select a colonist and use the normal Trade " +
                    "interaction.",
                    trader,
                    MessageTypeDefOf.NeutralEvent
                );

                return;
            }

            Messages.Message(
                "The enclave map exists, but its designated Trader " +
                "could not be located. Enter the camp and look for " +
                "the Trader before attempting to trade.",
                MessageTypeDefOf.RejectInput
            );
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
