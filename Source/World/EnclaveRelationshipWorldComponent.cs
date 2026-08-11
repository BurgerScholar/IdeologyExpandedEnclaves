using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public sealed class EnclaveRelationshipWorldComponent
        : WorldComponent
    {
        private List<InterEnclaveRelationshipRecord> relationships =
            new List<InterEnclaveRelationshipRecord>();
        private Dictionary<long, InterEnclaveRelationshipRecord>
            relationshipByPair;

        public EnclaveRelationshipWorldComponent(World world)
            : base(world)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(
                ref relationships,
                "interEnclaveRelationships",
                LookMode.Deep
            );

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                RebuildIndex();
            }
        }

        public InterEnclaveRelationshipRecord GetOrCreate(
            PilgrimCamp first,
            PilgrimCamp second
        )
        {
            if (!CanRepresent(first, second))
            {
                return null;
            }

            EnsureCollections();

            long pairKey = CreatePairKey(first.ID, second.ID);
            InterEnclaveRelationshipRecord relationship;

            if (relationshipByPair.TryGetValue(
                pairKey,
                out relationship
            ))
            {
                return relationship;
            }

            int firstId = first.ID < second.ID
                ? first.ID
                : second.ID;
            int secondId = first.ID < second.ID
                ? second.ID
                : first.ID;

            relationship = new InterEnclaveRelationshipRecord
            {
                FirstEnclaveId = firstId,
                SecondEnclaveId = secondId,
                Score = InterEnclaveRelationshipUtility
                    .CalculateInitialScore(first, second)
            };

            relationships.Add(relationship);
            relationshipByPair.Add(pairKey, relationship);

            Log.Message(
                "[IEE] Initialized inter-enclave relationship " +
                "between " +
                (first.Data?.Name ?? first.LabelCap) +
                " and " +
                (second.Data?.Name ?? second.LabelCap) +
                " at " +
                relationship.Score +
                " (" +
                InterEnclaveRelationshipUtility.GetState(
                    relationship.Score
                ) +
                ")."
            );

            return relationship;
        }

        public bool Remove(
            PilgrimCamp first,
            PilgrimCamp second
        )
        {
            if (!CanRepresent(first, second))
            {
                return false;
            }

            EnsureCollections();

            long pairKey = CreatePairKey(first.ID, second.ID);
            InterEnclaveRelationshipRecord relationship;

            if (!relationshipByPair.TryGetValue(
                pairKey,
                out relationship
            ))
            {
                return false;
            }

            relationshipByPair.Remove(pairKey);
            relationships.Remove(relationship);
            return true;
        }

        private void RebuildIndex()
        {
            if (relationships == null)
            {
                relationships =
                    new List<InterEnclaveRelationshipRecord>();
            }

            relationshipByPair =
                new Dictionary<long, InterEnclaveRelationshipRecord>();
            List<InterEnclaveRelationshipRecord> validRelationships =
                new List<InterEnclaveRelationshipRecord>();

            foreach (
                InterEnclaveRelationshipRecord relationship in
                relationships
            )
            {
                if (relationship == null)
                {
                    continue;
                }

                relationship.Normalize();

                if (
                    relationship.FirstEnclaveId < 0 ||
                    relationship.SecondEnclaveId < 0 ||
                    relationship.FirstEnclaveId ==
                        relationship.SecondEnclaveId
                )
                {
                    Log.Warning(
                        "[IEE] Ignored an invalid saved inter-enclave " +
                        "relationship record."
                    );
                    continue;
                }

                long key = CreatePairKey(
                    relationship.FirstEnclaveId,
                    relationship.SecondEnclaveId
                );

                if (relationshipByPair.ContainsKey(key))
                {
                    Log.Warning(
                        "[IEE] Ignored a duplicate saved " +
                        "inter-enclave relationship record for IDs " +
                        relationship.FirstEnclaveId +
                        " and " +
                        relationship.SecondEnclaveId +
                        "."
                    );
                    continue;
                }

                relationshipByPair.Add(key, relationship);
                validRelationships.Add(relationship);
            }

            relationships = validRelationships;
        }

        private void EnsureCollections()
        {
            if (relationships == null)
            {
                relationships =
                    new List<InterEnclaveRelationshipRecord>();
            }

            if (relationshipByPair == null)
            {
                RebuildIndex();
            }
        }

        private static bool CanRepresent(
            PilgrimCamp first,
            PilgrimCamp second
        )
        {
            return
                first != null &&
                second != null &&
                first != second &&
                first.Data != null &&
                second.Data != null &&
                !first.Destroyed &&
                !second.Destroyed &&
                first.ID >= 0 &&
                second.ID >= 0 &&
                first.ID != second.ID;
        }

        private static long CreatePairKey(int firstId, int secondId)
        {
            uint lower = (uint)(firstId < secondId
                ? firstId
                : secondId);
            uint higher = (uint)(firstId < secondId
                ? secondId
                : firstId);

            return ((long)lower << 32) | higher;
        }
    }
}
