using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public class PilgrimCamp : MapParent
    {
        protected override bool UseGenericEnterMapFloatMenuOption =>
            false;

        public EnclaveData Data;
        public EnclavePawnRoleAssignments PawnRoles =
            new EnclavePawnRoleAssignments();
        public EnclaveRecruitmentCandidates RecruitmentCandidates =
            new EnclaveRecruitmentCandidates();
        public EnclaveVisitingGroup VisitingGroup =
            new EnclaveVisitingGroup();
        public EnclaveHarmPenaltyTracker HarmPenalties =
            new EnclaveHarmPenaltyTracker();
        public EnclavePawnMembers PawnMembers =
            new EnclavePawnMembers();

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Deep.Look(ref Data, "enclaveData");
            Scribe_Deep.Look(ref PawnRoles, "pawnRoles");
            Scribe_Deep.Look(
                ref RecruitmentCandidates,
                "recruitmentCandidates"
            );
            Scribe_Deep.Look(ref VisitingGroup, "visitingGroup");
            Scribe_Deep.Look(ref HarmPenalties, "harmPenalties");
            Scribe_Deep.Look(ref PawnMembers, "pawnMembers");

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

                if (VisitingGroup == null)
                {
                    VisitingGroup = new EnclaveVisitingGroup();
                }

                if (HarmPenalties == null)
                {
                    HarmPenalties = new EnclaveHarmPenaltyTracker();
                }

                if (PawnMembers == null)
                {
                    PawnMembers = new EnclavePawnMembers();
                }

                if (!VisitingGroup.HasStoredMembers && Map != null)
                {
                    VisitingGroup.RecoverFromMap(Map);

                    Log.Message(
                        "[IEE] Recovered visiting-group references for " +
                        (Data?.Name ?? "an enclave") +
                        " from its loaded map."
                    );
                }

                EnclaveTradeService.SuppressVanillaTradeOption(
                    PawnRoles.GetPawn(EnclavePawnRole.Trader)
                );

                PilgrimCamp loadedCamp = this;

                LongEventHandler.ExecuteWhenFinished(
                    delegate
                    {
                        EnclaveFactionUtility.EnsureCampFaction(
                            loadedCamp
                        );
                        EnclaveIdeologyUtility
                            .EnsureCampPawnAlignment(
                                loadedCamp,
                                "existing-save migration"
                            );
                        EnclaveLocalHostilityService
                            .UpdateCampCombatState(loadedCamp);
                    }
                );
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
            bool locallyHostile =
                EnclaveRelationshipUtility.IsLocallyHostile(this);

            yield return new FloatMenuOption(
                "Visit " +
                enclaveName +
                (locallyHostile ? " (Warning: Hostile)" : string.Empty),
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
                defaultLabel = "Enclave Overview",
                defaultDesc =
                    "Open the detailed enclave identity, relationship, " +
                    "and nearby influence overview.",
                icon = BaseContent.BadTex,
                action = delegate
                {
                    EnsureDataExists();
                    EnclaveDialogs.OpenOverview(this);
                }
            };

            yield return new Command_Action
            {
                defaultLabel = "Visit enclave",
                defaultDesc =
                    EnclaveRelationshipUtility.IsLocallyHostile(this)
                        ? "Warning: this enclave is Hostile and its " +
                            "members will attack a visiting caravan."
                        : "Prepare to send a caravan into the pilgrim camp.",
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
                    defaultLabel = "DEV: Enclave Testing",
                    defaultDesc =
                        "Open the organized enclave testing tools menu.",
                    icon = BaseContent.BadTex,
                    action = delegate
                    {
                        EnsureDataExists();
                        EnclaveWorldTestTools.ShowTestingMenu(this);
                    }
                };
            }
        }

        public override void Notify_CaravanFormed(Caravan caravan)
        {
            base.Notify_CaravanFormed(caravan);
            EnclaveLocalHostilityService.UpdateCampCombatState(this);
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
                    "Site: Pilgrim Camp\n" +
                    "Leader: " + Data.Leader + "\n" +
                    "Population: " + Data.Population + "\n" +
                    "Ideology: " +
                    EnclaveIdeologyUtility.GetActualIdeoLabel(Data) +
                    "\n" +
                    "Type: " +
                    EnclaveIdeologyUtility.GetTypeLabel(Data) +
                    "\n" +
                    "Development: " +
                    EnclaveDevelopmentUtility.GetDisplayName(Data) +
                    "\n" +
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
            EnsureDataExists();

            string unavailableReason;

            if (
                !EnclaveTradeService.TradingIsAvailable(
                    this,
                    out unavailableReason
                )
            )
            {
                EnclaveTradeService.NotifyTradeBlocked(this);
                return;
            }

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
                int bonusPercent =
                    EnclaveTradeService.GetTradeBonusPercent(this);

                CameraJumper.TryJump(trader.Position, map);
                Messages.Message(
                    "Enclave Trader located: " +
                    trader.LabelShort +
                    ". Select a colonist and use the normal Trade " +
                    "interaction." +
                    (bonusPercent > 0
                        ? " " +
                            Data.ReputationTierLabel +
                            " reputation trade bonus: " +
                            bonusPercent +
                            "%."
                        : string.Empty),
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

    }
}
