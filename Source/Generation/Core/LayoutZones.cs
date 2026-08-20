namespace IdeologyExpandedEnclaves
{
    public class LayoutZones
    {
        public LayoutZone Gathering;
        public LayoutZone Sleeping;
        public LayoutZone Storage;
        public LayoutZone Ritual;

        public LayoutZones(
            LayoutAnchors anchors,
            EnclaveDevelopmentVisualProfile profile
        )
        {
            Gathering = new LayoutZone(
                anchors.Gathering,
                profile.GatheringWidth,
                profile.GatheringHeight
            );

            Sleeping = new LayoutZone(
                anchors.Sleeping,
                profile.SleepingWidth,
                profile.SleepingHeight
            );

            Storage = new LayoutZone(
                anchors.Storage,
                profile.StorageWidth,
                profile.StorageHeight
            );

            Ritual = new LayoutZone(
                anchors.Ritual,
                profile.RitualWidth,
                profile.RitualHeight
            );
        }
    }
}
