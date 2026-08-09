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

        public LayoutAnchors(IntVec3 campCenter)
        {
            Center = campCenter;

            Gathering =
                campCenter + new IntVec3(0, 0, 6);

            Sleeping =
                campCenter + new IntVec3(-8, 0, 0);

            Storage =
                campCenter + new IntVec3(8, 0, 0);

            Ritual =
                campCenter + new IntVec3(0, 0, -8);
        }
    }
}