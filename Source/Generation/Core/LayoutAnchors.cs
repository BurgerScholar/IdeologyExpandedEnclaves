using System;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public class LayoutAnchors
    {
        public IntVec3 Center;
        public IntVec3 Gathering;
        public IntVec3 Sleeping;
        public IntVec3 Storage;
        public IntVec3 Ritual;

        public LayoutAnchors(
            IntVec3 campCenter,
            Random random
        )
        {
            Center = campCenter;

            IntVec3[] positionOffsets =
            {
                new IntVec3(0, 0, 16),
                new IntVec3(0, 0, -16),
                new IntVec3(16, 0, 0),
                new IntVec3(-16, 0, 0)
            };

            string[] positionNames =
            {
                "North",
                "South",
                "East",
                "West"
            };

            int[] assignments =
            {
                0,
                1,
                2,
                3
            };

            for (int i = assignments.Length - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                int temporary = assignments[i];

                assignments[i] = assignments[swapIndex];
                assignments[swapIndex] = temporary;
            }

            Gathering = campCenter + positionOffsets[assignments[0]];
            Sleeping = campCenter + positionOffsets[assignments[1]];
            Storage = campCenter + positionOffsets[assignments[2]];
            Ritual = campCenter + positionOffsets[assignments[3]];

            Log.Message(
                "[IEE] Layout arrangement: " +
                "Gathering=" + positionNames[assignments[0]] + ", " +
                "Sleeping=" + positionNames[assignments[1]] + ", " +
                "Storage=" + positionNames[assignments[2]] + ", " +
                "Ritual=" + positionNames[assignments[3]] + "."
            );
        }
    }
}
