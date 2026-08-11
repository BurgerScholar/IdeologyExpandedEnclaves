using System;
using System.Collections.Generic;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveIdeologyCompatibilityUtility
    {
        private static readonly Dictionary<int, EnclaveIdeologyCompatibility>
            pairCompatibility =
                new Dictionary<int, EnclaveIdeologyCompatibility>
                {
                    {
                        PairKey(
                            EnclaveIdeologyType.Communal,
                            EnclaveIdeologyType.Mercantile
                        ),
                        EnclaveIdeologyCompatibility.Compatible
                    },
                    {
                        PairKey(
                            EnclaveIdeologyType.Communal,
                            EnclaveIdeologyType.Isolationist
                        ),
                        EnclaveIdeologyCompatibility.Opposed
                    },
                    {
                        PairKey(
                            EnclaveIdeologyType.Martial,
                            EnclaveIdeologyType.Isolationist
                        ),
                        EnclaveIdeologyCompatibility.Compatible
                    },
                    {
                        PairKey(
                            EnclaveIdeologyType.Mercantile,
                            EnclaveIdeologyType.Isolationist
                        ),
                        EnclaveIdeologyCompatibility.Opposed
                    },
                    {
                        PairKey(
                            EnclaveIdeologyType.Nature,
                            EnclaveIdeologyType.Transhumanist
                        ),
                        EnclaveIdeologyCompatibility.StronglyOpposed
                    },
                    {
                        PairKey(
                            EnclaveIdeologyType.Spiritual,
                            EnclaveIdeologyType.Transhumanist
                        ),
                        EnclaveIdeologyCompatibility.Opposed
                    },
                    {
                        PairKey(
                            EnclaveIdeologyType.Nature,
                            EnclaveIdeologyType.Spiritual
                        ),
                        EnclaveIdeologyCompatibility.Compatible
                    },
                    {
                        PairKey(
                            EnclaveIdeologyType.Mercantile,
                            EnclaveIdeologyType.Transhumanist
                        ),
                        EnclaveIdeologyCompatibility.Compatible
                    }
                };

        public static EnclaveIdeologyCompatibility GetCompatibility(
            EnclaveData first,
            EnclaveData second
        )
        {
            return GetCompatibility(
                EnclaveIdeologyUtility.GetIdeologyType(first),
                EnclaveIdeologyUtility.GetIdeologyType(second)
            );
        }

        public static EnclaveIdeologyCompatibility GetCompatibility(
            EnclaveIdeologyType first,
            EnclaveIdeologyType second
        )
        {
            if (
                first == EnclaveIdeologyType.Unassigned ||
                second == EnclaveIdeologyType.Unassigned ||
                !Enum.IsDefined(typeof(EnclaveIdeologyType), first) ||
                !Enum.IsDefined(typeof(EnclaveIdeologyType), second)
            )
            {
                return EnclaveIdeologyCompatibility.Neutral;
            }

            if (first == second)
            {
                return EnclaveIdeologyCompatibility.StronglyCompatible;
            }

            EnclaveIdeologyCompatibility compatibility;

            return pairCompatibility.TryGetValue(
                PairKey(first, second),
                out compatibility
            )
                ? compatibility
                : EnclaveIdeologyCompatibility.Neutral;
        }

        public static string GetDisplayName(
            EnclaveIdeologyCompatibility compatibility
        )
        {
            switch (compatibility)
            {
                case EnclaveIdeologyCompatibility.StronglyOpposed:
                    return "Strongly Opposed";
                case EnclaveIdeologyCompatibility.Opposed:
                    return "Opposed";
                case EnclaveIdeologyCompatibility.Compatible:
                    return "Compatible";
                case EnclaveIdeologyCompatibility.StronglyCompatible:
                    return "Strongly Compatible";
                default:
                    return "Neutral";
            }
        }

        private static int PairKey(
            EnclaveIdeologyType first,
            EnclaveIdeologyType second
        )
        {
            int firstValue = (int)first;
            int secondValue = (int)second;
            int lower = firstValue < secondValue
                ? firstValue
                : secondValue;
            int higher = firstValue < secondValue
                ? secondValue
                : firstValue;

            return lower * 100 + higher;
        }
    }
}
