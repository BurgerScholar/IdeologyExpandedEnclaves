using Verse;

namespace IdeologyExpandedEnclaves
{
    public class LayoutZone
    {
        public CellRect Area;

        public LayoutZone(
            IntVec3 center,
            int width,
            int height
        )
        {
            Area = CellRect.CenteredOn(
                center,
                width,
                height
            );
        }

        public bool Contains(IntVec3 cell)
        {
            return Area.Contains(cell);
        }
    }
}
