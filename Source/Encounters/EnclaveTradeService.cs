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

        public static int GetReputationTradeBonusPercent(
            PilgrimCamp camp
        )
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

        public static int GetArchetypeTradeBonusPercent(
            PilgrimCamp camp
        )
        {
            return camp?.Data == null
                ? 0
                : EnclaveArchetypeUtility
                    .GetProfile(camp.Data)
                    .TradeFavorableBonusPercent;
        }

        public static int GetTradeBonusPercent(PilgrimCamp camp)
        {
            return System.Math.Min(
                EnclaveArchetypeUtility
                    .MaximumTradeFavorableBonusPercent,
                GetReputationTradeBonusPercent(camp) +
                GetArchetypeTradeBonusPercent(camp)
            );
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

        public static bool TryGetTradeHostForTrader(
            Pawn trader,
            out MapParent tradeHost,
            out PilgrimCamp sourceCamp
        )
        {
            tradeHost = trader?.Map?.Parent;
            sourceCamp = null;

            if (tradeHost is PilgrimCamp homeCamp)
            {
                if (
                    homeCamp.PawnRoles?.GetPawn(
                        EnclavePawnRole.Trader
                    ) != trader
                )
                {
                    tradeHost = null;
                    return false;
                }

                sourceCamp = homeCamp;
                return true;
            }

            EnclaveExpeditionSite expedition =
                tradeHost as EnclaveExpeditionSite;

            if (
                expedition == null ||
                expedition.Destroyed ||
                expedition.ExpeditionTrader != trader ||
                expedition.SourceCamp?.Data == null ||
                expedition.SourceCamp.Destroyed ||
                expedition.PendingExpiration
            )
            {
                tradeHost = null;
                return false;
            }

            sourceCamp = expedition.SourceCamp;
            return true;
        }

        public static bool TradingIsAvailable(
            MapParent tradeHost,
            out string unavailableReason
        )
        {
            EnclaveExpeditionSite expedition =
                tradeHost as EnclaveExpeditionSite;

            if (expedition != null)
            {
                if (
                    expedition.PendingExpiration ||
                    expedition.Destroyed
                )
                {
                    unavailableReason =
                        "Trading unavailable. This expedition site is " +
                        "expiring.";
                    return false;
                }

                return TradingIsAvailable(
                    expedition.SourceCamp,
                    out unavailableReason
                );
            }

            return TradingIsAvailable(
                tradeHost as PilgrimCamp,
                out unavailableReason
            );
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

        public static void NotifyTradeBlocked(
            MapParent tradeHost,
            Pawn trader = null
        )
        {
            PilgrimCamp sourceCamp = GetSourceCamp(tradeHost);
            string reason;

            TradingIsAvailable(tradeHost, out reason);

            Messages.Message(
                reason ?? "Trading is currently unavailable.",
                trader,
                MessageTypeDefOf.RejectInput
            );

            Log.Message(
                "[IEE] Blocked enclave trade at " +
                (tradeHost?.Label ?? "an unavailable trade host") +
                " for " +
                (sourceCamp?.Data?.Name ?? "an enclave") +
                "."
            );
        }

        public static bool TryOpenTrade(
            MapParent tradeHost,
            Pawn trader,
            Pawn negotiator
        )
        {
            PilgrimCamp camp;
            MapParent resolvedHost;
            string unavailableReason;

            if (
                !TryGetTradeHostForTrader(
                    trader,
                    out resolvedHost,
                    out camp
                ) ||
                resolvedHost != tradeHost
            )
            {
                Messages.Message(
                    "The enclave Trader is no longer available.",
                    MessageTypeDefOf.RejectInput
                );
                return false;
            }

            if (!TradingIsAvailable(tradeHost, out unavailableReason))
            {
                NotifyTradeBlocked(tradeHost, trader);
                return false;
            }

            if (
                trader == null ||
                negotiator == null ||
                trader.Map != tradeHost.Map ||
                negotiator.Map != tradeHost.Map ||
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
                tradeHost is PilgrimCamp &&
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
                new EnclaveReputationTrader(
                    tradeHost,
                    camp,
                    trader
                );

            if (bonusPercent > 0)
            {
                Log.Message(
                    "[IEE] Applied " +
                    bonusPercent +
                    "% enclave trade modifier for " +
                    (camp.Data?.Name ?? "an enclave") +
                    " at reputation tier " +
                    camp.Data.ReputationTierLabel +
                    " and archetype " +
                    EnclaveArchetypeUtility.GetDisplayName(camp.Data) +
                    " (reputation " +
                    GetReputationTradeBonusPercent(camp) +
                    "%, archetype " +
                    GetArchetypeTradeBonusPercent(camp) +
                    "%)" +
                    ". Trader: " +
                    trader.LabelShort +
                    " (" +
                    trader.GetUniqueLoadID() +
                    ")."
                );
            }

            EnclaveTradeSessionContext.Begin(
                tradeHost,
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

        internal static bool RestoreTraderAfterFactionChange(
            PilgrimCamp camp
        )
        {
            Pawn trader = camp?.PawnRoles?.GetPawn(
                EnclavePawnRole.Trader
            );

            if (
                trader == null ||
                trader.Destroyed ||
                trader.Dead
            )
            {
                return true;
            }

            return EnsureTraderTracker(trader);
        }

        private static bool EnsureTraderTracker(Pawn trader)
        {
            if (trader?.mindState == null)
            {
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

            TraderKindDef traderKind = trader.trader.traderKind;

            if (traderKind == null)
            {
                string defName = TraderKindDefName;
                EnclaveExpeditionSite expedition =
                    trader.Map?.Parent as EnclaveExpeditionSite;

                if (expedition != null)
                {
                    defName = EnclaveExpeditionUtility
                        .GetTraderKindDefName(expedition.Purpose);
                }

                traderKind =
                    DefDatabase<TraderKindDef>.GetNamedSilentFail(
                        defName
                    );

                if (traderKind == null)
                {
                    Log.Error(
                        "[IEE] Missing TraderKindDef " + defName + "."
                    );
                    return false;
                }
            }

            trader.trader.traderKind = traderKind;
            SuppressVanillaTradeOption(trader);
            return true;
        }

        private class EnclaveReputationTrader : ITrader
        {
            private readonly MapParent tradeHost;
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

                    return TradingIsAvailable(tradeHost, out reason) &&
                        TraderCanTradeNow(trader);
                }
            }

            public EnclaveReputationTrader(
                MapParent tradeHost,
                PilgrimCamp camp,
                Pawn trader
            )
            {
                this.tradeHost = tradeHost;
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

                EnclaveVisitingGroup visitingGroup =
                    GetVisitingGroup(tradeHost);

                if (visitingGroup == null)
                {
                    yield break;
                }

                foreach (
                    Thing thing in
                    visitingGroup.InventoryThings(tradeHost)
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
                    GetVisitingGroup(tradeHost)
                        ?.ActiveMembersList(tradeHost) ??
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

        internal static EnclaveVisitingGroup GetVisitingGroup(
            MapParent tradeHost
        )
        {
            PilgrimCamp camp = tradeHost as PilgrimCamp;

            if (camp != null)
            {
                return camp.VisitingGroup;
            }

            return (
                tradeHost as EnclaveExpeditionSite
            )?.VisitingGroup;
        }

        internal static PilgrimCamp GetSourceCamp(
            MapParent tradeHost
        )
        {
            PilgrimCamp camp = tradeHost as PilgrimCamp;

            return camp ??
                (tradeHost as EnclaveExpeditionSite)?.SourceCamp;
        }

        internal static bool IsDesignatedTrader(
            MapParent tradeHost,
            Pawn trader
        )
        {
            PilgrimCamp source;
            MapParent resolved;

            return
                TryGetTradeHostForTrader(
                    trader,
                    out resolved,
                    out source
                ) &&
                resolved == tradeHost;
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
            MapParent tradeHost;

            if (
                !EnclaveTradeService.TryGetTradeHostForTrader(
                    clickedPawn,
                    out tradeHost,
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
                    tradeHost,
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
                            tradeHost,
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
                    "enclave benefit: " +
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
            MapParent tradeHost = pawn?.Map?.Parent;

            EnclaveTradeService.TryOpenTrade(
                tradeHost,
                trader,
                pawn
            );
        }
    }
}
