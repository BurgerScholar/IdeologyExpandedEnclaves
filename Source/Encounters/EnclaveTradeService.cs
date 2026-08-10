using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveTradeService
    {
        private const string TraderKindDefName =
            "IEE_PilgrimCampTrader";

        public static int GetTradeBonusPercent(PilgrimCamp camp)
        {
            if (camp?.Data == null)
            {
                return 0;
            }

            switch (camp.Data.ReputationTier)
            {
                case EnclaveReputationTier.Friendly:
                    return 5;
                case EnclaveReputationTier.Trusted:
                    return 10;
                case EnclaveReputationTier.Revered:
                    return 15;
                default:
                    return 0;
            }
        }

        public static bool TradingIsAvailable(
            PilgrimCamp camp,
            out string unavailableReason
        )
        {
            unavailableReason = null;

            if (camp?.Data == null)
            {
                unavailableReason =
                    "The enclave reputation data is unavailable.";
                return false;
            }

            EnclaveReputationTier tier = camp.Data.ReputationTier;

            if (
                tier == EnclaveReputationTier.Hostile ||
                tier == EnclaveReputationTier.Wary
            )
            {
                unavailableReason =
                    "Trading unavailable. This enclave does not trust " +
                    "you enough to trade. Current reputation: " +
                    camp.Data.Reputation +
                    " — " +
                    tier +
                    ".";
                return false;
            }

            return true;
        }

        public static bool TryGetEnclaveForTrader(
            Pawn trader,
            out PilgrimCamp camp
        )
        {
            camp = trader?.Map?.Parent as PilgrimCamp;

            return camp != null &&
                camp.PawnRoles?.GetPawn(EnclavePawnRole.Trader) ==
                    trader;
        }

        public static void SuppressVanillaTradeOption(Pawn trader)
        {
            if (trader?.mindState != null)
            {
                trader.mindState.wantsToTradeWithColony = false;
            }
        }

        public static void NotifyTradeBlocked(
            PilgrimCamp camp,
            Pawn trader = null
        )
        {
            string reason;

            TradingIsAvailable(camp, out reason);

            Messages.Message(
                reason ?? "Trading is currently unavailable.",
                trader,
                MessageTypeDefOf.RejectInput
            );

            Log.Message(
                "[IEE] Blocked enclave trade for " +
                (camp?.Data?.Name ?? "an enclave") +
                " at reputation " +
                (camp?.Data?.Reputation ?? 0) +
                " (" +
                (camp?.Data?.ReputationTierLabel ?? "Unknown") +
                ")."
            );
        }

        public static bool TryOpenTrade(
            PilgrimCamp camp,
            Pawn trader,
            Pawn negotiator
        )
        {
            string unavailableReason;

            if (!TradingIsAvailable(camp, out unavailableReason))
            {
                NotifyTradeBlocked(camp, trader);
                return false;
            }

            if (
                trader == null ||
                negotiator == null ||
                camp?.PawnRoles?.GetPawn(EnclavePawnRole.Trader) !=
                    trader ||
                trader.Map != camp.Map ||
                negotiator.Map != camp.Map ||
                !TraderCanTradeNow(trader)
            )
            {
                Messages.Message(
                    "The enclave Trader is no longer available.",
                    MessageTypeDefOf.RejectInput
                );
                return false;
            }

            if (
                !EnclaveTraderStockService.EnsureStockForCurrentTier(
                    camp,
                    trader
                )
            )
            {
                Messages.Message(
                    "The enclave Trader's reputation stock could " +
                    "not be initialized. Check the error log.",
                    trader,
                    MessageTypeDefOf.RejectInput
                );
                return false;
            }

            int bonusPercent = GetTradeBonusPercent(camp);
            ITrader reputationTrader =
                new EnclaveReputationTrader(camp, trader);

            if (bonusPercent > 0)
            {
                Log.Message(
                    "[IEE] Applied " +
                    bonusPercent +
                    "% reputation trade modifier for " +
                    (camp.Data?.Name ?? "an enclave") +
                    " at tier " +
                    camp.Data.ReputationTierLabel +
                    ". Trader: " +
                    trader.LabelShort +
                    " (" +
                    trader.GetUniqueLoadID() +
                    ")."
                );
            }

            EnclaveTradeSessionContext.Begin(
                camp,
                trader,
                reputationTrader
            );

            try
            {
                Find.WindowStack.Add(
                    new Dialog_EnclaveTrade(
                        negotiator,
                        reputationTrader,
                        false
                    )
                );
            }
            catch
            {
                EnclaveTradeSessionContext.Clear(reputationTrader);
                throw;
            }

            return true;
        }

        private class Dialog_EnclaveTrade : Dialog_Trade
        {
            private readonly ITrader enclaveSessionTrader;

            public Dialog_EnclaveTrade(
                Pawn negotiator,
                ITrader trader,
                bool giftsOnly
            )
                : base(negotiator, trader, giftsOnly)
            {
                enclaveSessionTrader = trader;
            }

            public override void Close(bool doCloseSound = true)
            {
                try
                {
                    base.Close(doCloseSound);
                }
                finally
                {
                    EnclaveTradeSessionContext.Clear(
                        enclaveSessionTrader
                    );
                }
            }
        }

        internal static bool TraderCanTradeNow(Pawn trader)
        {
            if (!EnsureTraderTracker(trader))
            {
                return false;
            }

            trader.mindState.wantsToTradeWithColony = true;

            try
            {
                return ((ITrader)trader).CanTradeNow;
            }
            finally
            {
                SuppressVanillaTradeOption(trader);
            }
        }

        private static bool EnsureTraderTracker(Pawn trader)
        {
            if (trader?.mindState == null)
            {
                return false;
            }

            TraderKindDef traderKind =
                DefDatabase<TraderKindDef>.GetNamedSilentFail(
                    TraderKindDefName
                );

            if (traderKind == null)
            {
                Log.Error(
                    "[IEE] Missing TraderKindDef " +
                    TraderKindDefName +
                    "."
                );
                return false;
            }

            if (trader.trader == null)
            {
                trader.mindState.wantsToTradeWithColony = true;
                PawnComponentsUtility.AddAndRemoveDynamicComponents(
                    trader
                );
            }

            if (trader.trader == null)
            {
                Log.Error(
                    "[IEE] Could not restore the enclave Trader tracker " +
                    "for " +
                    trader.LabelShort +
                    "."
                );
                return false;
            }

            trader.trader.traderKind = traderKind;
            SuppressVanillaTradeOption(trader);
            return true;
        }

        private class EnclaveReputationTrader : ITrader
        {
            private readonly PilgrimCamp camp;
            private readonly Pawn trader;

            private ITrader InnerTrader => (ITrader)trader;

            public TraderKindDef TraderKind => InnerTrader.TraderKind;
            public IEnumerable<Thing> Goods => InnerTrader.Goods;
            public int RandomPriceFactorSeed =>
                InnerTrader.RandomPriceFactorSeed;
            public string TraderName => InnerTrader.TraderName;
            public Faction Faction => InnerTrader.Faction;
            public TradeCurrency TradeCurrency =>
                InnerTrader.TradeCurrency;
            public float TradePriceImprovementOffsetForPlayer =>
                GetTradeBonusPercent(camp) / 100f;

            public bool CanTradeNow
            {
                get
                {
                    string reason;

                    return TradingIsAvailable(camp, out reason) &&
                        TraderCanTradeNow(trader);
                }
            }

            public EnclaveReputationTrader(
                PilgrimCamp camp,
                Pawn trader
            )
            {
                this.camp = camp;
                this.trader = trader;
            }

            public IEnumerable<Thing> ColonyThingsWillingToBuy(
                Pawn playerNegotiator
            )
            {
                HashSet<Thing> yielded = new HashSet<Thing>();

                foreach (
                    Thing thing in
                    InnerTrader.ColonyThingsWillingToBuy(
                        playerNegotiator
                    )
                )
                {
                    if (thing != null && yielded.Add(thing))
                    {
                        yield return thing;
                    }
                }

                if (camp?.VisitingGroup == null)
                {
                    yield break;
                }

                foreach (
                    Thing thing in
                    camp.VisitingGroup.InventoryThings(camp)
                )
                {
                    if (thing != null && yielded.Add(thing))
                    {
                        yield return thing;
                    }
                }
            }

            public void GiveSoldThingToTrader(
                Thing toGive,
                int countToGive,
                Pawn playerNegotiator
            )
            {
                InnerTrader.GiveSoldThingToTrader(
                    toGive,
                    countToGive,
                    playerNegotiator
                );
            }

            public void GiveSoldThingToPlayer(
                Thing toGive,
                int countToGive,
                Pawn playerNegotiator
            )
            {
                if (toGive is Pawn)
                {
                    InnerTrader.GiveSoldThingToPlayer(
                        toGive,
                        countToGive,
                        playerNegotiator
                    );
                    return;
                }

                Thing purchased = toGive.SplitOff(countToGive);

                purchased.PreTraded(
                    TradeAction.PlayerBuys,
                    playerNegotiator,
                    trader
                );

                List<Pawn> receivers =
                    camp?.VisitingGroup?.ActiveMembersList(camp) ??
                    new List<Pawn>();
                Pawn receiver =
                    CaravanInventoryUtility.FindPawnToMoveInventoryTo(
                        purchased,
                        receivers,
                        null,
                        null
                    );

                if (
                    receiver?.inventory != null &&
                    receiver.inventory.innerContainer.TryAdd(
                        purchased,
                        true
                    )
                )
                {
                    return;
                }

                Map map = trader.Map;

                if (
                    map != null &&
                    GenPlace.TryPlaceThing(
                        purchased,
                        trader.Position,
                        map,
                        ThingPlaceMode.Near
                    )
                )
                {
                    Log.Warning(
                        "[IEE] No visiting-group inventory could accept " +
                        purchased.Label +
                        "; the purchase was placed near the Trader."
                    );
                    return;
                }

                if (
                    trader.inventory != null &&
                    trader.inventory.innerContainer.TryAdd(
                        purchased,
                        true
                    )
                )
                {
                    Log.Error(
                        "[IEE] Could not deliver purchased enclave item " +
                        purchased.Label +
                        "; it was returned to the Trader's inventory."
                    );
                    return;
                }

                Log.Error(
                    "[IEE] Could not deliver or safely restore purchased " +
                    "enclave trade item " +
                    purchased.Label +
                    "."
                );
            }
        }
    }

    public class FloatMenuOptionProvider_EnclaveTrade
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
            PilgrimCamp camp;

            if (
                !EnclaveTradeService.TryGetEnclaveForTrader(
                    clickedPawn,
                    out camp
                )
            )
            {
                yield break;
            }

            EnclaveTradeService.SuppressVanillaTradeOption(clickedPawn);

            Pawn negotiator = context?.FirstSelectedPawn;

            if (
                negotiator == null ||
                !negotiator.IsColonistPlayerControlled ||
                !negotiator.RaceProps.Humanlike ||
                negotiator.Map != clickedPawn.Map
            )
            {
                yield break;
            }

            string unavailableReason;

            if (
                !EnclaveTradeService.TradingIsAvailable(
                    camp,
                    out unavailableReason
                )
            )
            {
                yield return new FloatMenuOption(
                    "Trading unavailable (" +
                    camp.Data.ReputationTierLabel +
                    ")",
                    delegate
                    {
                        EnclaveTradeService.NotifyTradeBlocked(
                            camp,
                            clickedPawn
                        );
                    }
                );
                yield break;
            }

            string label = "Trade with " + clickedPawn.LabelShort;
            int bonusPercent =
                EnclaveTradeService.GetTradeBonusPercent(camp);

            if (bonusPercent > 0)
            {
                label +=
                    " (" +
                    camp.Data.ReputationTierLabel +
                    " bonus: " +
                    bonusPercent +
                    "%)";
            }

            if (!EnclaveTradeService.TraderCanTradeNow(clickedPawn))
            {
                yield return new FloatMenuOption(
                    label + ": Trader unavailable",
                    null
                );
                yield break;
            }

            if (
                !negotiator.CanReach(
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
                        EnclaveJobDefOf.IEE_TradeWithEnclaveTrader,
                        clickedPawn
                    );
                    job.playerForced = true;
                    negotiator.jobs.TryTakeOrderedJob(job);
                }
            );
        }
    }

    public class JobDriver_TradeWithEnclaveTrader : JobDriver
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
            yield return Toils_General.Do(OpenTradeDialog);
        }

        private void OpenTradeDialog()
        {
            Pawn trader = TargetPawnA;
            PilgrimCamp camp = pawn?.Map?.Parent as PilgrimCamp;

            EnclaveTradeService.TryOpenTrade(camp, trader, pawn);
        }
    }
}
