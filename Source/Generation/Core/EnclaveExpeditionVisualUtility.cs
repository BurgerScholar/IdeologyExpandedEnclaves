using System;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveExpeditionVisualUtility
    {
        public static EnclaveDevelopmentVisualProfile GetProfile(
            EnclaveData data,
            EnclaveExpeditionPurpose purpose
        )
        {
            EnclaveDevelopmentVisualProfile profile =
                EnclaveDevelopmentVisualUtility.GetProfile(data);

            profile.AreaScalePercent = Math.Max(
                60,
                profile.AreaScalePercent * 3 / 4
            );
            profile.GatheringWidth = Scale(profile.GatheringWidth, 7);
            profile.GatheringHeight = Scale(profile.GatheringHeight, 7);
            profile.SleepingWidth = Scale(profile.SleepingWidth, 8);
            profile.SleepingHeight = Scale(profile.SleepingHeight, 8);
            profile.StorageWidth = Scale(profile.StorageWidth, 6);
            profile.StorageHeight = Scale(profile.StorageHeight, 6);
            profile.GatheringSeatCount = Math.Max(
                2,
                Math.Min(5, profile.GatheringSeatCount)
            );
            profile.GatheringLightCount = Math.Min(
                2,
                profile.GatheringLightCount
            );
            profile.SleepingLightCount = Math.Min(
                1,
                profile.SleepingLightCount
            );

            switch (purpose)
            {
                case EnclaveExpeditionPurpose.Trade:
                    profile.StorageStackCount = Math.Min(
                        4,
                        Math.Max(2, profile.StorageStackCount)
                    );
                    profile.OrganizationLabel +=
                        ", temporary trade outpost";
                    break;
                case EnclaveExpeditionPurpose.Patrol:
                    profile.StorageStackCount = Math.Min(
                        3,
                        Math.Max(2, profile.StorageStackCount)
                    );
                    profile.SleepingSpacing = Math.Max(
                        2,
                        profile.SleepingSpacing - 1
                    );
                    profile.OrganizationLabel +=
                        ", temporary patrol staging";
                    break;
                default:
                    profile.StorageStackCount = Math.Min(
                        3,
                        Math.Max(2, profile.StorageStackCount)
                    );
                    profile.GatheringSeatCount = Math.Min(
                        5,
                        profile.GatheringSeatCount + 1
                    );
                    profile.OrganizationLabel +=
                        ", temporary relief camp";
                    break;
            }

            return profile;
        }

        private static int Scale(int value, int minimum)
        {
            return Math.Max(minimum, value * 3 / 4);
        }
    }
}
