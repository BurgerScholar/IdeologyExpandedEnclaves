using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveExpeditionCampLayout
    {
        public static void Generate(
            Map map,
            IntVec3 center,
            int partySize,
            EnclaveData sourceData,
            EnclaveExpeditionPurpose purpose
        )
        {
            EnclaveDevelopmentVisualProfile visualProfile =
                EnclaveExpeditionVisualUtility.GetProfile(
                    sourceData,
                    purpose
                );
            LayoutContext context = new LayoutContext(
                map,
                center,
                partySize,
                sourceData,
                visualProfile
            );

            if (purpose == EnclaveExpeditionPurpose.Patrol)
            {
                SleepingAreaPiece.Generate(context);
                GatheringAreaPiece.Generate(context);
            }
            else
            {
                GatheringAreaPiece.Generate(context);
                SleepingAreaPiece.Generate(context);
            }

            ExpeditionStorageAreaPiece.Generate(context, purpose);

            Log.Message(
                "[IEE] Generated scaled " +
                EnclaveExpeditionUtility.GetSiteTypeLabel(purpose) +
                " layout."
            );
        }
    }
}
