using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveIdeologyUtility
    {
        private sealed class WeightedIdeologyType
        {
            public readonly EnclaveIdeologyType Type;
            public readonly int Weight;

            public WeightedIdeologyType(
                EnclaveIdeologyType type,
                int weight
            )
            {
                Type = type;
                Weight = weight;
            }
        }

        private static readonly WeightedIdeologyType[] weightedTypes =
        {
            new WeightedIdeologyType(
                EnclaveIdeologyType.Communal,
                18
            ),
            new WeightedIdeologyType(
                EnclaveIdeologyType.Isolationist,
                12
            ),
            new WeightedIdeologyType(
                EnclaveIdeologyType.Martial,
                12
            ),
            new WeightedIdeologyType(
                EnclaveIdeologyType.Mercantile,
                15
            ),
            new WeightedIdeologyType(
                EnclaveIdeologyType.Nature,
                15
            ),
            new WeightedIdeologyType(
                EnclaveIdeologyType.Spiritual,
                18
            ),
            new WeightedIdeologyType(
                EnclaveIdeologyType.Transhumanist,
                10
            )
        };

        public static EnclaveIdeologyType GetIdeologyType(
            EnclaveData data
        )
        {
            return data?.IdeologyProfile?.IsValid == true
                ? data.IdeologyProfile.Type
                : EnclaveIdeologyType.Unassigned;
        }

        public static bool HasType(
            EnclaveData data,
            EnclaveIdeologyType type
        )
        {
            return
                type != EnclaveIdeologyType.Unassigned &&
                GetIdeologyType(data) == type;
        }

        public static string GetTypeLabel(EnclaveData data)
        {
            EnclaveIdeologyType type = GetIdeologyType(data);

            return type == EnclaveIdeologyType.Unassigned
                ? "Unknown"
                : type.ToString();
        }

        public static bool SetIdeologyType(
            EnclaveData data,
            EnclaveIdeologyType type,
            string reason = null
        )
        {
            if (
                data == null ||
                type == EnclaveIdeologyType.Unassigned ||
                !Enum.IsDefined(typeof(EnclaveIdeologyType), type)
            )
            {
                return false;
            }

            EnsureProfile(data, reason: "ideology type assignment");

            EnclaveIdeologyType previous = data.IdeologyProfile.Type;

            if (previous == type)
            {
                return true;
            }

            data.IdeologyProfile.Type = type;

            Log.Message(
                "[IEE] Enclave ideology type for " +
                (data.Name ?? "an enclave") +
                " changed from " +
                previous +
                " to " +
                type +
                (reason.NullOrEmpty()
                    ? "."
                    : " (" + reason + ").")
            );

            return true;
        }

        public static Ideo GetActualIdeo(EnclaveData data)
        {
            return data?.IdeologyProfile?.ActualIdeo;
        }

        public static bool TryGetOrCreateActualIdeo(
            EnclaveData data,
            out Ideo ideo
        )
        {
            return TryEnsureActualIdeo(data, out ideo);
        }

        public static string GetActualIdeoLabel(EnclaveData data)
        {
            Ideo ideo = GetActualIdeo(data);

            return ideo?.name.NullOrEmpty() == false
                ? ideo.name
                : "Not yet established";
        }

        public static bool EnsureCampPawnAlignment(
            PilgrimCamp camp,
            string reason = null
        )
        {
            if (
                camp?.Data == null ||
                camp.Map == null ||
                camp.PawnMembers?.Members == null ||
                !camp.PawnMembers.IsInitialized
            )
            {
                return false;
            }

            Ideo ideo;

            if (!TryEnsureActualIdeo(camp.Data, out ideo))
            {
                return false;
            }

            int eligibleCount = 0;
            int alignedCount = 0;

            foreach (Pawn pawn in camp.PawnMembers.Members)
            {
                if (
                    pawn == null ||
                    pawn.Destroyed ||
                    pawn.Dead ||
                    pawn.Faction == Faction.OfPlayer ||
                    pawn.RaceProps == null ||
                    !pawn.RaceProps.Humanlike ||
                    pawn.Map != camp.Map
                )
                {
                    continue;
                }

                eligibleCount++;

                if (pawn.Ideo == ideo)
                {
                    continue;
                }

                try
                {
                    if (pawn.ideo == null)
                    {
                        PawnComponentsUtility
                            .AddAndRemoveDynamicComponents(pawn);
                    }

                    if (pawn.ideo == null)
                    {
                        Log.Error(
                            "[IEE] Could not align ideology for " +
                            pawn.GetUniqueLoadID() +
                            " because its ideology tracker is missing."
                        );
                        continue;
                    }

                    pawn.ideo.SetIdeo(ideo);
                    alignedCount++;
                }
                catch (Exception exception)
                {
                    Log.Error(
                        "[IEE] Could not align enclave member " +
                        pawn.GetUniqueLoadID() +
                        " with " +
                        GetActualIdeoLabel(camp.Data) +
                        ": " +
                        exception
                    );
                }
            }

            if (alignedCount > 0)
            {
                Log.Message(
                    "[IEE] Aligned " +
                    alignedCount +
                    " of " +
                    eligibleCount +
                    " registered member(s) at " +
                    (camp.Data.Name ?? "an enclave") +
                    " with Ideo " +
                    GetActualIdeoLabel(camp.Data) +
                    (reason.NullOrEmpty()
                        ? "."
                        : " (" + reason + ").")
                );
            }

            return true;
        }

        public static bool IsPersistentCampIdeo(Ideo ideo)
        {
            if (ideo == null || Find.World == null)
            {
                return false;
            }

            List<WorldObject> worldObjects =
                Find.WorldObjects?.AllWorldObjects;

            if (worldObjects == null)
            {
                return false;
            }

            foreach (WorldObject worldObject in worldObjects)
            {
                PilgrimCamp camp = worldObject as PilgrimCamp;

                if (
                    camp?.Data?.IdeologyProfile?.ActualIdeo == ideo
                )
                {
                    return true;
                }
            }

            return false;
        }

        public static bool EnsureProfile(
            EnclaveData data,
            Random random = null,
            string reason = null
        )
        {
            if (data == null)
            {
                return false;
            }

            if (data.IdeologyProfile?.IsValid == true)
            {
                return false;
            }

            Random generationRandom =
                random ?? new Random(CreateStableSeed(data));

            data.IdeologyProfile = new EnclaveIdeologyProfile
            {
                Type = SelectType(generationRandom)
            };

            Log.Message(
                "[IEE] Generated persistent ideology profile for " +
                (data.Name ?? "an enclave") +
                ": " +
                data.IdeologyProfile.Type +
                (reason.NullOrEmpty()
                    ? "."
                    : " (" + reason + ").")
            );

            return true;
        }

        private static bool TryEnsureActualIdeo(
            EnclaveData data,
            out Ideo ideo
        )
        {
            ideo = null;

            if (data == null || !ModsConfig.IdeologyActive)
            {
                return false;
            }

            EnsureProfile(data, reason: "actual Ideo generation");

            IdeoManager manager = Find.IdeoManager;
            Faction enclaveFaction =
                EnclaveFactionUtility.GetOrCreateFaction();

            if (manager == null || enclaveFaction == null)
            {
                Log.Error(
                    "[IEE] Cannot establish an enclave Ideo because " +
                    "the ideology manager or enclave faction is missing."
                );
                return false;
            }

            ideo = data.IdeologyProfile.ActualIdeo;

            if (ideo != null)
            {
                if (
                    !manager.IdeosListForReading.Contains(ideo) &&
                    !manager.Add(ideo)
                )
                {
                    Log.Error(
                        "[IEE] The saved Ideo for " +
                        (data.Name ?? "an enclave") +
                        " could not be registered with RimWorld."
                    );
                    ideo = null;
                    return false;
                }

                return true;
            }

            Ideo generatedIdeo;

            try
            {
                generatedIdeo = IdeoGenerator.GenerateIdeo(
                    new IdeoGenerationParms(enclaveFaction.def)
                );
            }
            catch (Exception exception)
            {
                Log.Error(
                    "[IEE] Failed to generate a persistent Ideo for " +
                    (data.Name ?? "an enclave") +
                    ": " +
                    exception
                );
                return false;
            }

            if (generatedIdeo == null || !manager.Add(generatedIdeo))
            {
                Log.Error(
                    "[IEE] RimWorld did not register the generated " +
                    "Ideo for " +
                    (data.Name ?? "an enclave") +
                    "."
                );
                return false;
            }

            data.IdeologyProfile.ActualIdeo = generatedIdeo;
            ideo = generatedIdeo;

            Log.Message(
                "[IEE] Generated persistent Ideo " +
                GetActualIdeoLabel(data) +
                " (" +
                generatedIdeo.GetUniqueLoadID() +
                ") for " +
                (data.Name ?? "an enclave") +
                "."
            );

            return true;
        }

        private static EnclaveIdeologyType SelectType(Random random)
        {
            int totalWeight = 0;

            foreach (WeightedIdeologyType weightedType in weightedTypes)
            {
                totalWeight += weightedType.Weight;
            }

            int selection = random.Next(totalWeight);

            foreach (WeightedIdeologyType weightedType in weightedTypes)
            {
                if (selection < weightedType.Weight)
                {
                    return weightedType.Type;
                }

                selection -= weightedType.Weight;
            }

            return EnclaveIdeologyType.Communal;
        }

        private static int CreateStableSeed(EnclaveData data)
        {
            unchecked
            {
                uint hash = 2166136261;

                AddStringToHash(ref hash, data.Name);
                AddStringToHash(ref hash, data.Leader);
                AddIntToHash(ref hash, data.Population);
                AddIntToHash(
                    ref hash,
                    (int)data.GatheringPosition
                );
                AddIntToHash(
                    ref hash,
                    (int)data.SleepingPosition
                );
                AddIntToHash(
                    ref hash,
                    (int)data.StoragePosition
                );
                AddIntToHash(
                    ref hash,
                    (int)data.RitualPosition
                );

                return (int)hash;
            }
        }

        private static void AddStringToHash(
            ref uint hash,
            string value
        )
        {
            if (value.NullOrEmpty())
            {
                AddIntToHash(ref hash, 0);
                return;
            }

            foreach (char character in value)
            {
                hash ^= character;
                hash *= 16777619;
            }
        }

        private static void AddIntToHash(ref uint hash, int value)
        {
            unchecked
            {
                hash ^= (uint)value;
                hash *= 16777619;
            }
        }
    }
}
