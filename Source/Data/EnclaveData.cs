using System;
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

    public class EnclaveData : IExposable
    {
        public string Name;
        public string Leader;
        public string Ideology;
        public int Population;
        public bool Friendly;
        public EnclaveLayoutPosition GatheringPosition;
        public EnclaveLayoutPosition SleepingPosition;
        public EnclaveLayoutPosition StoragePosition;
        public EnclaveLayoutPosition RitualPosition;

        public void ExposeData()
        {
            Scribe_Values.Look(ref Name, "name", "Unnamed Enclave");
            Scribe_Values.Look(ref Leader, "leader", "Unknown");
            Scribe_Values.Look(ref Ideology, "ideology", "Unknown");
            Scribe_Values.Look(ref Population, "population", 0);
            Scribe_Values.Look(ref Friendly, "friendly", true);
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
    }
}
