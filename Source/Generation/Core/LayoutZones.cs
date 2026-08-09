namespace IdeologyExpandedEnclaves
{
    public class LayoutZones
    {
        public LayoutZone Gathering;
        public LayoutZone Sleeping;
        public LayoutZone Storage;
        public LayoutZone Ritual;

        public LayoutZones(LayoutAnchors anchors)
        {
            Gathering = new LayoutZone(
                anchors.Gathering,
                10,
                8
            );

            Sleeping = new LayoutZone(
                anchors.Sleeping,
                12,
                10
            );

            Storage = new LayoutZone(
                anchors.Storage,
                8,
                8
            );

            Ritual = new LayoutZone(
                anchors.Ritual,
                10,
                8
            );
        }
    }
}
