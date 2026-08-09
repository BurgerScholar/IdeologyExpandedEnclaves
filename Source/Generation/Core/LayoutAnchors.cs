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
            EnclaveData enclave
        )
        {
            Center = campCenter;

            Gathering = campCenter + GetOffset(enclave.GatheringPosition);
            Sleeping = campCenter + GetOffset(enclave.SleepingPosition);
            Storage = campCenter + GetOffset(enclave.StoragePosition);
            Ritual = campCenter + GetOffset(enclave.RitualPosition);
        }

        private static IntVec3 GetOffset(
            EnclaveLayoutPosition position
        )
        {
            switch (position)
            {
                case EnclaveLayoutPosition.North:
                    return new IntVec3(0, 0, 16);
                case EnclaveLayoutPosition.South:
                    return new IntVec3(0, 0, -16);
                case EnclaveLayoutPosition.East:
                    return new IntVec3(16, 0, 0);
                case EnclaveLayoutPosition.West:
                    return new IntVec3(-16, 0, 0);
                default:
                    return IntVec3.Zero;
            }
        }
    }
}
