using System;
using RimWorld.Planet;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class InterEnclaveRelationshipUtility
    {
        public const int MinimumScore = -100;
        public const int MaximumScore = 100;

        public const int HostileMaximum = -51;
        public const int RivalMaximum = -11;
        public const int NeutralMaximum = 10;
        public const int FriendlyMaximum = 50;

        public const int CompatibilityScorePerProximityWeight = 15;

        public static InterEnclaveRelationshipRecord GetRelationship(
            PilgrimCamp first,
            PilgrimCamp second
        )
        {
            return GetComponent()?.GetOrCreate(first, second);
        }

        public static int GetRelationshipScore(
            PilgrimCamp first,
            PilgrimCamp second
        )
        {
            InterEnclaveRelationshipRecord relationship =
                GetRelationship(first, second);

            return relationship?.Score ?? 0;
        }

        public static InterEnclaveRelationshipState GetRelationshipState(
            PilgrimCamp first,
            PilgrimCamp second
        )
        {
            return GetState(GetRelationshipScore(first, second));
        }

        public static int AdjustRelationship(
            PilgrimCamp first,
            PilgrimCamp second,
            int amount,
            string reason = null
        )
        {
            InterEnclaveRelationshipRecord relationship =
                GetRelationship(first, second);

            if (relationship == null)
            {
                return 0;
            }

            int previous = relationship.Score;
            relationship.Score = ClampScore(
                (long)relationship.Score + amount
            );

            if (previous != relationship.Score)
            {
                Log.Message(
                    "[IEE] Inter-enclave relationship between " +
                    (first.Data?.Name ?? first.LabelCap) +
                    " and " +
                    (second.Data?.Name ?? second.LabelCap) +
                    " changed from " +
                    previous +
                    " to " +
                    relationship.Score +
                    " (" +
                    GetState(relationship.Score) +
                    ")" +
                    (reason.NullOrEmpty()
                        ? "."
                        : ": " + reason + ".")
                );
            }

            return relationship.Score;
        }

        public static InterEnclaveRelationshipRecord
            ResetRelationshipToBaseline(
            PilgrimCamp first,
            PilgrimCamp second
        )
        {
            EnclaveRelationshipWorldComponent component =
                GetComponent();

            if (component == null)
            {
                return null;
            }

            component.Remove(first, second);
            InterEnclaveRelationshipRecord relationship =
                component.GetOrCreate(first, second);

            if (relationship != null)
            {
                Log.Message(
                    "[IEE] Reset inter-enclave relationship between " +
                    (first.Data?.Name ?? first.LabelCap) +
                    " and " +
                    (second.Data?.Name ?? second.LabelCap) +
                    " to baseline " +
                    relationship.Score +
                    " (" +
                    GetState(relationship.Score) +
                    ")."
                );
            }

            return relationship;
        }

        public static int SetRelationshipScore(
            PilgrimCamp first,
            PilgrimCamp second,
            int score,
            string reason = null
        )
        {
            InterEnclaveRelationshipRecord relationship =
                GetRelationship(first, second);

            if (relationship == null)
            {
                return 0;
            }

            int previous = relationship.Score;
            relationship.Score = ClampScore(score);

            if (previous != relationship.Score)
            {
                Log.Message(
                    "[IEE] Set inter-enclave relationship between " +
                    (first.Data?.Name ?? first.LabelCap) +
                    " and " +
                    (second.Data?.Name ?? second.LabelCap) +
                    " from " +
                    previous +
                    " to " +
                    relationship.Score +
                    " (" +
                    GetState(relationship.Score) +
                    ")" +
                    (reason.NullOrEmpty()
                        ? "."
                        : ": " + reason + ".")
                );
            }

            return relationship.Score;
        }

        public static int SetRelationshipState(
            PilgrimCamp first,
            PilgrimCamp second,
            InterEnclaveRelationshipState state,
            string reason = null
        )
        {
            if (!Enum.IsDefined(
                typeof(InterEnclaveRelationshipState),
                state
            ))
            {
                return GetRelationshipScore(first, second);
            }

            return SetRelationshipScore(
                first,
                second,
                GetRepresentativeScore(state),
                reason
            );
        }

        public static bool AreAllied(
            PilgrimCamp first,
            PilgrimCamp second
        )
        {
            return GetRelationshipState(first, second) ==
                InterEnclaveRelationshipState.Allied;
        }

        public static bool AreRivals(
            PilgrimCamp first,
            PilgrimCamp second
        )
        {
            return GetRelationshipState(first, second) ==
                InterEnclaveRelationshipState.Rival;
        }

        public static bool AreHostile(
            PilgrimCamp first,
            PilgrimCamp second
        )
        {
            return GetRelationshipState(first, second) ==
                InterEnclaveRelationshipState.Hostile;
        }

        public static int CalculateInitialScore(
            PilgrimCamp first,
            PilgrimCamp second
        )
        {
            if (first?.Data == null || second?.Data == null)
            {
                return 0;
            }

            EnclaveIdeologyCompatibility compatibility =
                EnclaveIdeologyCompatibilityUtility.GetCompatibility(
                    first.Data,
                    second.Data
                );
            float distance =
                EnclaveProximityUtility.GetDistanceInTiles(
                    first,
                    second
                );
            EnclaveDistanceBand distanceBand =
                EnclaveProximityUtility.GetDistanceBand(distance);
            int proximityWeight =
                EnclaveInfluenceUtility.GetDistanceWeight(
                    distanceBand
                );

            return ClampScore(
                (int)compatibility *
                proximityWeight *
                CompatibilityScorePerProximityWeight
            );
        }

        public static InterEnclaveRelationshipState GetState(int score)
        {
            int clampedScore = ClampScore(score);

            if (clampedScore <= HostileMaximum)
            {
                return InterEnclaveRelationshipState.Hostile;
            }

            if (clampedScore <= RivalMaximum)
            {
                return InterEnclaveRelationshipState.Rival;
            }

            if (clampedScore <= NeutralMaximum)
            {
                return InterEnclaveRelationshipState.Neutral;
            }

            if (clampedScore <= FriendlyMaximum)
            {
                return InterEnclaveRelationshipState.Friendly;
            }

            return InterEnclaveRelationshipState.Allied;
        }

        public static int ClampScore(long score)
        {
            if (score < MinimumScore)
            {
                return MinimumScore;
            }

            if (score > MaximumScore)
            {
                return MaximumScore;
            }

            return (int)score;
        }

        public static int GetRepresentativeScore(
            InterEnclaveRelationshipState state
        )
        {
            switch (state)
            {
                case InterEnclaveRelationshipState.Hostile:
                    return -75;
                case InterEnclaveRelationshipState.Rival:
                    return -25;
                case InterEnclaveRelationshipState.Friendly:
                    return 25;
                case InterEnclaveRelationshipState.Allied:
                    return 75;
                default:
                    return 0;
            }
        }

        private static EnclaveRelationshipWorldComponent GetComponent()
        {
            World world = Find.World;

            return world?.GetComponent<
                EnclaveRelationshipWorldComponent
            >();
        }
    }
}
