using System;
using System.Collections.Generic;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public enum EnclaveLayoutPosition
    {
        Unassigned,
        North,
        South,
        East,
        West
    }

    public enum EnclaveTraderStockGrantTier
    {
        None,
        Friendly,
        Trusted,
        Revered
    }

    public class EnclaveData : IExposable
    {
        public string Name;
        public string Leader;
        // Retained for compatibility with saves created before profiles.
        public string Ideology;
        public EnclaveIdeologyProfile IdeologyProfile;
        public EnclaveArchetype Archetype;
        public int Population;
        public EnclaveDevelopmentTier DevelopmentTier;
        public int DevelopmentTierInitialPopulation = -1;
        public bool Friendly;
        public int Reputation;
        public EnclaveTraderStockGrantTier HighestTraderStockTierGranted;
        public EnclaveLayoutPosition GatheringPosition;
        public EnclaveLayoutPosition SleepingPosition;
        public EnclaveLayoutPosition StoragePosition;
        public EnclaveLayoutPosition RitualPosition;
        private List<EnclaveNeedRecord> needs =
            new List<EnclaveNeedRecord>();
        private EnclaveQuestRequest activeQuestRequest;
        private int nextQuestRequestId = 1;
        private int nextQuestRequestEligibleTick;

        public IReadOnlyList<EnclaveNeedRecord> Needs => needs;
        public EnclaveQuestRequest ActiveQuestRequest =>
            activeQuestRequest;
        public int NextQuestRequestEligibleTick =>
            nextQuestRequestEligibleTick;

        public EnclaveReputationTier ReputationTier =>
            EnclaveReputation.GetTier(Reputation);

        public string ReputationTierLabel =>
            EnclaveReputation.GetTierLabel(Reputation);

        public void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                Reputation = EnclaveReputation.Clamp(Reputation);
            }

            Scribe_Values.Look(ref Name, "name", "Unnamed Enclave");
            Scribe_Values.Look(ref Leader, "leader", "Unknown");
            Scribe_Values.Look(ref Ideology, "ideology", "Unknown");
            Scribe_Deep.Look(
                ref IdeologyProfile,
                "ideologyProfile"
            );
            Scribe_Values.Look(
                ref Archetype,
                "archetype",
                EnclaveArchetype.Unassigned
            );
            Scribe_Values.Look(ref Population, "population", 0);
            Scribe_Values.Look(
                ref DevelopmentTier,
                "developmentTier",
                EnclaveDevelopmentTier.Unassigned
            );
            Scribe_Values.Look(
                ref DevelopmentTierInitialPopulation,
                "developmentTierInitialPopulation",
                -1
            );
            Scribe_Values.Look(ref Friendly, "friendly", true);
            Scribe_Values.Look(
                ref Reputation,
                "reputation",
                EnclaveReputation.InitialValue
            );
            Scribe_Values.Look(
                ref HighestTraderStockTierGranted,
                "highestTraderStockTierGranted",
                EnclaveTraderStockGrantTier.None
            );
            Scribe_Values.Look(
                ref GatheringPosition,
                "gatheringPosition",
                EnclaveLayoutPosition.Unassigned
            );
            Scribe_Values.Look(
                ref SleepingPosition,
                "sleepingPosition",
                EnclaveLayoutPosition.Unassigned
            );
            Scribe_Values.Look(
                ref StoragePosition,
                "storagePosition",
                EnclaveLayoutPosition.Unassigned
            );
            Scribe_Values.Look(
                ref RitualPosition,
                "ritualPosition",
                EnclaveLayoutPosition.Unassigned
            );
            Scribe_Collections.Look(
                ref needs,
                "needs",
                LookMode.Deep
            );
            Scribe_Deep.Look(
                ref activeQuestRequest,
                "activeQuestRequest"
            );
            Scribe_Values.Look(
                ref nextQuestRequestId,
                "nextQuestRequestId",
                1
            );
            Scribe_Values.Look(
                ref nextQuestRequestEligibleTick,
                "nextQuestRequestEligibleTick",
                0
            );

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                int loadedReputation = Reputation;

                Reputation = EnclaveReputation.Clamp(Reputation);

                EnclaveIdeologyUtility.EnsureProfile(
                    this,
                    reason: "existing-save migration"
                );

                if (
                    Archetype != EnclaveArchetype.Unassigned &&
                    !Enum.IsDefined(typeof(EnclaveArchetype), Archetype)
                )
                {
                    Log.Warning(
                        "[IEE] Reset invalid loaded archetype for " +
                        (Name ?? "an enclave") +
                        "."
                    );
                    Archetype = EnclaveArchetype.Unassigned;
                }

                EnclaveDevelopmentUtility.EnsureTier(
                    this,
                    reason: "existing-save migration"
                );
                EnclaveNeedsUtility.EnsureNeeds(
                    this,
                    "existing-save migration"
                );

                nextQuestRequestId = Math.Max(
                    1,
                    nextQuestRequestId
                );
                nextQuestRequestEligibleTick = Math.Max(
                    0,
                    nextQuestRequestEligibleTick
                );

                if (
                    !Enum.IsDefined(
                        typeof(EnclaveTraderStockGrantTier),
                        HighestTraderStockTierGranted
                    )
                )
                {
                    Log.Warning(
                        "[IEE] Reset invalid trader stock grant tier " +
                        "for " +
                        (Name ?? "an enclave") +
                        "."
                    );

                    HighestTraderStockTierGranted =
                        EnclaveTraderStockGrantTier.None;
                }

                if (loadedReputation != Reputation)
                {
                    Log.Warning(
                        "[IEE] Clamped invalid loaded reputation for " +
                        (Name ?? "an enclave") +
                        " from " +
                        loadedReputation +
                        " to " +
                        Reputation +
                        "."
                    );
                }

                Log.Message(
                    "[IEE] Loaded persistent reputation for " +
                    (Name ?? "an enclave") +
                    ": " +
                    Reputation +
                    " (" +
                    ReputationTierLabel +
                    ")."
                );
            }
        }

        public void InitializeReputation()
        {
            Reputation = EnclaveReputation.InitialValue;

            Log.Message(
                "[IEE] Initialized reputation for " +
                (Name ?? "an enclave") +
                " at " +
                Reputation +
                " (" +
                ReputationTierLabel +
                ")."
            );
        }

        public int ChangeReputation(
            int amount,
            string reason = null
        )
        {
            return ApplyReputation(
                (long)Reputation + amount,
                reason
            );
        }

        public int SetReputation(
            int value,
            string reason = null
        )
        {
            return ApplyReputation(value, reason);
        }

        private int ApplyReputation(long value, string reason)
        {
            int previous = EnclaveReputation.Clamp(Reputation);
            int updated = EnclaveReputation.Clamp(value);

            Reputation = updated;

            if (previous != updated)
            {
                Log.Message(
                    "[IEE] Reputation for " +
                    (Name ?? "an enclave") +
                    " changed from " +
                    previous +
                    " to " +
                    updated +
                    " (" +
                    ReputationTierLabel +
                    ")" +
                    (reason.NullOrEmpty()
                        ? "."
                        : ": " + reason + ".")
                );
            }

            return Reputation;
        }

        public bool EnsureLayoutAssignments(Random random)
        {
            if (HasValidLayoutAssignments())
            {
                return false;
            }

            EnclaveLayoutPosition[] positions =
            {
                EnclaveLayoutPosition.North,
                EnclaveLayoutPosition.South,
                EnclaveLayoutPosition.East,
                EnclaveLayoutPosition.West
            };

            for (int i = positions.Length - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                EnclaveLayoutPosition temporary = positions[i];

                positions[i] = positions[swapIndex];
                positions[swapIndex] = temporary;
            }

            GatheringPosition = positions[0];
            SleepingPosition = positions[1];
            StoragePosition = positions[2];
            RitualPosition = positions[3];

            return true;
        }

        public string DescribeLayoutAssignments()
        {
            return
                "Gathering=" + GatheringPosition + ", " +
                "Sleeping=" + SleepingPosition + ", " +
                "Storage=" + StoragePosition + ", " +
                "Ritual=" + RitualPosition;
        }

        private bool HasValidLayoutAssignments()
        {
            return
                IsCardinalPosition(GatheringPosition) &&
                IsCardinalPosition(SleepingPosition) &&
                IsCardinalPosition(StoragePosition) &&
                IsCardinalPosition(RitualPosition) &&
                GatheringPosition != SleepingPosition &&
                GatheringPosition != StoragePosition &&
                GatheringPosition != RitualPosition &&
                SleepingPosition != StoragePosition &&
                SleepingPosition != RitualPosition &&
                StoragePosition != RitualPosition;
        }

        private static bool IsCardinalPosition(
            EnclaveLayoutPosition position
        )
        {
            return
                position >= EnclaveLayoutPosition.North &&
                position <= EnclaveLayoutPosition.West;
        }

        internal List<EnclaveNeedRecord> MutableNeeds
        {
            get => needs;
            set => needs = value;
        }

        internal int AllocateQuestRequestId()
        {
            int allocated = nextQuestRequestId;
            nextQuestRequestId = Math.Max(
                nextQuestRequestId + 1,
                1
            );
            return allocated;
        }

        internal void SetActiveQuestRequest(
            EnclaveQuestRequest request
        )
        {
            activeQuestRequest = request;
        }

        internal void SetNextQuestRequestEligibleTick(int tick)
        {
            nextQuestRequestEligibleTick = Math.Max(0, tick);
        }
    }
}
