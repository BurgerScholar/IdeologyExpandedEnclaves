using System.Collections.Generic;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveWorldTestTools
    {
        private static readonly HashSet<int> createdWorldObjectIds =
            new HashSet<int>();
        private static World trackedWorld;

        public static void RegisterCreatedWorldObject(
            WorldObject worldObject
        )
        {
            EnsureTrackingWorld();

            if (
                Prefs.DevMode &&
                worldObject != null &&
                worldObject.ID >= 0
            )
            {
                createdWorldObjectIds.Add(worldObject.ID);
            }
        }

        public static void ShowTestingMenu(PilgrimCamp camp)
        {
            if (!CanUse(camp))
            {
                return;
            }

            List<FloatMenuOption> options =
                new List<FloatMenuOption>
                {
                    new FloatMenuOption(
                        "Show Test State",
                        delegate { EnclaveDevTools.ShowTestState(camp); }
                    ),
                    new FloatMenuOption(
                        "Show Needs",
                        delegate { ShowNeeds(camp); }
                    ),
                    new FloatMenuOption(
                        "Evaluate Needs Now",
                        delegate { EvaluateNeedsNow(camp); }
                    ),
                    new FloatMenuOption(
                        "Set Need Severity",
                        delegate { ShowNeedTypeMenu(camp); }
                    ),
                    new FloatMenuOption(
                        "Create Critical Shortage",
                        delegate { ShowCriticalShortageMenu(camp); }
                    ),
                    new FloatMenuOption(
                        "Show Nearby Influence",
                        delegate { ShowNearbyInfluence(camp); }
                    ),
                    new FloatMenuOption(
                        "Preview Proximity Pulse",
                        delegate { PreviewProximityPulse(camp); }
                    ),
                    new FloatMenuOption(
                        "Apply Proximity Pulse Now",
                        delegate { ApplyProximityPulse(camp); }
                    ),
                    new FloatMenuOption(
                        "Give 2,000 Test Silver",
                        delegate { EnclaveDevTools.GiveTestSilver(camp); }
                    ),
                    new FloatMenuOption(
                        "Reputation",
                        delegate { ShowReputationMenu(camp); }
                    ),
                    new FloatMenuOption(
                        "Ideology Type",
                        delegate { ShowIdeologyTypeMenu(camp); }
                    ),
                    new FloatMenuOption(
                        "Development Tier",
                        delegate { ShowDevelopmentTierMenu(camp); }
                    ),
                    new FloatMenuOption(
                        "Camp Development Testing",
                        delegate
                        {
                            ShowCampDevelopmentTestingMenu();
                        }
                    ),
                    new FloatMenuOption(
                        "Overview Test Presets",
                        delegate { ShowPresetMenu(camp); }
                    ),
                    new FloatMenuOption(
                        "Spawn Nearby Enclave",
                        delegate { ShowSpawnEnclaveMenu(camp); }
                    ),
                    new FloatMenuOption(
                        "Relationship Tools",
                        delegate { ShowRelationshipNeighborMenu(camp); }
                    ),
                    new FloatMenuOption(
                        "Create Regional Test Scenario",
                        delegate { CreateRegionalTestScenario(camp); }
                    ),
                    new FloatMenuOption(
                        "Clean Tracked Test Neighbors",
                        delegate { CleanTrackedTestNeighbors(camp); }
                    )
                };

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void ShowNeeds(PilgrimCamp camp)
        {
            if (!CanUse(camp))
            {
                return;
            }

            StringBuilder report = new StringBuilder();

            report.AppendLine("Needs for " + camp.Data.Name);

            foreach (
                EnclaveNeedRecord need in
                EnclaveNeedsUtility.GetNeeds(camp)
            )
            {
                report.AppendLine(
                    EnclaveNeedsUtility.GetNeedLabel(need.Type) +
                    ": " +
                    need.Severity +
                    "; supply " +
                    need.EstimatedSupply +
                    "/" +
                    need.TargetAmount +
                    "; shortage " +
                    need.ShortageLevel +
                    "; last evaluation tick " +
                    need.LastEvaluationTick
                );
            }

            report.AppendLine();
            EnclaveQuestRequest request =
                EnclaveQuestService.GetActiveSupplyRequest(camp);
            report.AppendLine(
                "Supply Request: " +
                (request == null
                    ? "None active"
                    : EnclaveQuestService.DescribeRequest(request))
            );
            report.AppendLine(
                "Next request eligible tick: " +
                camp.Data.NextQuestRequestEligibleTick
            );

            ShowReport("DEV enclave needs", report.ToString().TrimEnd());
        }

        private static void EvaluateNeedsNow(PilgrimCamp camp)
        {
            if (!CanUse(camp))
            {
                return;
            }

            bool changed =
                EnclaveNeedsService.EvaluateCampAndGenerateRequest(camp);

            Messages.Message(
                "Evaluated production needs for " +
                camp.Data.Name +
                ". State changed: " +
                changed +
                ".",
                MessageTypeDefOf.NeutralEvent
            );
            ShowNeeds(camp);
        }

        private static void ShowNeedTypeMenu(PilgrimCamp camp)
        {
            List<FloatMenuOption> options =
                new List<FloatMenuOption>();

            foreach (
                EnclaveNeedType needType in
                (EnclaveNeedType[])System.Enum.GetValues(
                    typeof(EnclaveNeedType)
                )
            )
            {
                EnclaveNeedType selectedType = needType;

                options.Add(
                    new FloatMenuOption(
                        EnclaveNeedsUtility.GetNeedLabel(selectedType),
                        delegate
                        {
                            ShowNeedSeverityMenu(camp, selectedType);
                        }
                    )
                );
            }

            ShowValueMenu(options);
        }

        private static void ShowNeedSeverityMenu(
            PilgrimCamp camp,
            EnclaveNeedType needType
        )
        {
            List<FloatMenuOption> options =
                new List<FloatMenuOption>();

            foreach (
                EnclaveNeedSeverity severity in
                (EnclaveNeedSeverity[])System.Enum.GetValues(
                    typeof(EnclaveNeedSeverity)
                )
            )
            {
                EnclaveNeedSeverity selectedSeverity = severity;

                options.Add(
                    new FloatMenuOption(
                        selectedSeverity.ToString(),
                        delegate
                        {
                            SetNeedSeverity(
                                camp,
                                needType,
                                selectedSeverity
                            );
                        }
                    )
                );
            }

            ShowValueMenu(options);
        }

        private static void SetNeedSeverity(
            PilgrimCamp camp,
            EnclaveNeedType needType,
            EnclaveNeedSeverity severity
        )
        {
            bool changed = EnclaveNeedsUtility.SetNeedSeverity(
                camp.Data,
                needType,
                severity,
                "developer need test control"
            );

            Messages.Message(
                changed
                    ? EnclaveNeedsUtility.GetNeedLabel(needType) +
                        " need set to " +
                        severity +
                        "."
                    : "The need severity could not be changed.",
                changed
                    ? MessageTypeDefOf.NeutralEvent
                    : MessageTypeDefOf.RejectInput
            );
        }

        private static void ShowCriticalShortageMenu(PilgrimCamp camp)
        {
            List<FloatMenuOption> options =
                new List<FloatMenuOption>();

            foreach (
                EnclaveNeedType needType in
                (EnclaveNeedType[])System.Enum.GetValues(
                    typeof(EnclaveNeedType)
                )
            )
            {
                EnclaveNeedType selectedType = needType;

                options.Add(
                    new FloatMenuOption(
                        EnclaveNeedsUtility.GetNeedLabel(selectedType),
                        delegate
                        {
                            CreateCriticalShortage(
                                camp,
                                selectedType
                            );
                        }
                    )
                );
            }

            ShowValueMenu(options);
        }

        private static void CreateCriticalShortage(
            PilgrimCamp camp,
            EnclaveNeedType needType
        )
        {
            if (
                !EnclaveNeedsUtility.SetNeedSeverity(
                    camp.Data,
                    needType,
                    EnclaveNeedSeverity.Critical,
                    "developer critical-shortage preset"
                )
            )
            {
                Messages.Message(
                    "The critical shortage could not be created.",
                    MessageTypeDefOf.RejectInput
                );
                return;
            }

            EnclaveQuestRequest generatedRequest;
            bool generated =
                EnclaveQuestService.TryGenerateSupplyRequest(
                    camp,
                    out generatedRequest
                );

            Messages.Message(
                "Created a Critical " +
                EnclaveNeedsUtility.GetNeedLabel(needType) +
                " shortage for " +
                camp.Data.Name +
                "." +
                (generated
                    ? " A production Supply Request was generated."
                    : " No new request was generated because an active " +
                        "request, cooldown, hostility, or quest eligibility " +
                        "condition prevented it."),
                generated
                    ? MessageTypeDefOf.PositiveEvent
                    : MessageTypeDefOf.NeutralEvent
            );
        }

        public static void ShowNearbyInfluence(PilgrimCamp camp)
        {
            if (!CanUse(camp))
            {
                return;
            }

            List<EnclaveNeighborInfo> neighbors =
                EnclaveProximityUtility.GetNearbyNeighbors(camp);
            EnclaveRegionalInfluenceSummary regionalInfluence =
                EnclaveInfluenceUtility.CalculateRegionalSummary(
                    camp,
                    neighbors
                );
            StringBuilder report = new StringBuilder();

            report.AppendLine(
                "Nearby Influence for " + camp.Data.Name
            );
            report.AppendLine("Tile: " + camp.Tile);
            report.AppendLine(
                "Regional Status: " +
                regionalInfluence.StatusLabel +
                " (pressure " +
                FormatSignedScore(
                    regionalInfluence.TotalPressure
                ) +
                ")"
            );

            if (neighbors.Count == 0)
            {
                report.AppendLine("No qualifying neighbors within 30 tiles.");
            }
            else
            {
                foreach (EnclaveNeighborInfo neighbor in neighbors)
                {
                    report.AppendLine();
                    report.AppendLine(
                        neighbor.Label +
                        " [" +
                        GetNeighborTypeDisplayName(
                            neighbor.NeighborType
                        ) +
                        "]"
                    );
                    report.AppendLine(
                        "  Distance: " +
                        neighbor.DistanceInTiles.ToString("0.#") +
                        " tiles (" +
                        EnclaveProximityUtility
                            .GetDistanceBandDisplayName(
                                neighbor.DistanceBand
                            ) +
                        ")"
                    );
                    report.AppendLine(
                        "  Influence: distance " +
                        FormatSignedScore(
                            neighbor.Influence.DistanceWeight
                        ) +
                        ", development " +
                        FormatSignedScore(
                            neighbor.Influence.DevelopmentStrength
                        ) +
                        ", reputation " +
                        FormatSignedScore(
                            neighbor.Influence.ReputationWeight
                        ) +
                        ", ideology " +
                        FormatSignedScore(
                            neighbor.Influence
                                .IdeologyCompatibilityWeight
                        ) +
                        ", type " +
                        FormatSignedScore(
                            neighbor.Influence.NeighborTypeWeight
                        ) +
                        ", total " +
                        FormatSignedScore(neighbor.Influence.Total) +
                        "; regional pressure " +
                        FormatSignedScore(neighbor.RegionalPressure)
                    );

                    if (
                        neighbor.NeighborType ==
                            EnclaveNeighborType.Enclave
                    )
                    {
                        report.AppendLine(
                            "  Ideology: " +
                            neighbor.IdeologyType +
                            "; compatibility: " +
                            EnclaveIdeologyCompatibilityUtility
                                .GetDisplayName(
                                    neighbor.IdeologyCompatibility
                                )
                        );
                        report.AppendLine(
                            "  Relationship: " +
                            (neighbor.RelationshipState?.ToString() ??
                                "Unavailable") +
                            (neighbor.RelationshipScore.HasValue
                                ? " (" +
                                    FormatSignedScore(
                                        neighbor.RelationshipScore.Value
                                    ) +
                                    ")"
                                : string.Empty)
                        );
                    }
                }
            }

            ShowReport(
                "DEV nearby influence",
                report.ToString().TrimEnd()
            );
        }

        private static void PreviewProximityPulse(PilgrimCamp camp)
        {
            if (!CanUse(camp))
            {
                return;
            }

            EnclaveProximityPulseResult preview =
                EnclaveProximityEffectsService.PreviewPulse();

            ShowProximityPulseReport(
                camp,
                preview,
                "Proximity Pulse Preview",
                applied: false
            );
        }

        private static void ApplyProximityPulse(PilgrimCamp camp)
        {
            if (!CanUse(camp))
            {
                return;
            }

            EnclaveProximityPulseResult result =
                EnclaveProximityEffectsService.ApplyPulse();

            ShowProximityPulseReport(
                camp,
                result,
                "Proximity Pulse Applied",
                applied: true
            );
        }

        private static void ShowProximityPulseReport(
            PilgrimCamp camp,
            EnclaveProximityPulseResult result,
            string title,
            bool applied
        )
        {
            EnclaveCampProximityEffect campEffect =
                result?.GetCampEffect(camp);
            StringBuilder report = new StringBuilder();

            report.AppendLine(title + " - " + camp.Data.Name);

            if (campEffect == null)
            {
                report.AppendLine(
                    "This camp was not eligible for the production pulse."
                );
                ShowReport(
                    "DEV proximity pulse",
                    report.ToString().TrimEnd()
                );
                return;
            }

            report.AppendLine(
                "Regional Status: " +
                campEffect.RegionalInfluence.StatusLabel +
                " (pressure " +
                FormatSignedScore(
                    campEffect.RegionalInfluence.TotalPressure
                ) +
                ")"
            );

            if (campEffect.NearestPlayerSettlement == null)
            {
                report.AppendLine(
                    "Player reputation: no player settlement within " +
                    "30 tiles; delta +0."
                );
            }
            else
            {
                EnclaveNeighborInfo settlement =
                    campEffect.NearestPlayerSettlement;

                report.AppendLine(
                    "Nearest player settlement: " +
                    settlement.Label +
                    " at " +
                    settlement.DistanceInTiles.ToString("0.#") +
                    " tiles (" +
                    EnclaveProximityUtility.GetDistanceBandDisplayName(
                        settlement.DistanceBand
                    ) +
                    ")."
                );
                report.AppendLine(
                    "Player reputation: " +
                    campEffect.StartingReputation +
                    " -> " +
                    campEffect.ProjectedReputation +
                    " (delta " +
                    FormatSignedScore(
                        campEffect.PlayerReputationDelta
                    ) +
                    ", " +
                    EnclaveIdeologyUtility.GetTypeLabel(camp.Data) +
                    " tendency)."
                );
            }

            report.AppendLine("Nearby enclave relationships:");
            int relationshipCount = 0;

            foreach (
                EnclaveRelationshipProximityEffect relationship in
                result.RelationshipEffects
            )
            {
                if (!relationship.Includes(camp))
                {
                    continue;
                }

                PilgrimCamp otherCamp = relationship.GetOtherCamp(camp);
                relationshipCount++;
                report.AppendLine(
                    "  " +
                    (otherCamp?.Data?.Name ?? "Unknown enclave") +
                    ": " +
                    relationship.StartingRelationship +
                    " -> " +
                    relationship.ProjectedRelationship +
                    " (delta " +
                    FormatSignedScore(
                        relationship.RelationshipDelta
                    ) +
                    ", " +
                    EnclaveIdeologyCompatibilityUtility
                        .GetDisplayName(relationship.Compatibility) +
                    ", " +
                    EnclaveProximityUtility.GetDistanceBandDisplayName(
                        relationship.DistanceBand
                    ) +
                    ")."
                );
            }

            if (relationshipCount == 0)
            {
                report.AppendLine("  None within 30 tiles.");
            }

            report.AppendLine();
            report.AppendLine(
                applied
                    ? "The exact production pulse was applied globally " +
                        "to all initialized Pilgrim Camps."
                    : "Preview only; no reputation or relationship " +
                        "drift was applied."
            );

            ShowReport(
                "DEV proximity pulse",
                report.ToString().TrimEnd()
            );
        }

        private static void ShowReputationMenu(PilgrimCamp camp)
        {
            ShowValueMenu(
                new List<FloatMenuOption>
                {
                    CreateReputationOption(camp, "Hostile", -50),
                    CreateReputationOption(camp, "Wary", -10),
                    CreateReputationOption(camp, "Neutral", 0),
                    CreateReputationOption(camp, "Friendly", 30),
                    CreateReputationOption(camp, "Trusted", 60),
                    CreateReputationOption(camp, "Revered", 90)
                }
            );
        }

        private static FloatMenuOption CreateReputationOption(
            PilgrimCamp camp,
            string label,
            int value
        )
        {
            return new FloatMenuOption(
                label + ": " + value,
                delegate
                {
                    EnclaveReputationTier previousTier =
                        camp.Data.ReputationTier;

                    camp.Data.SetReputation(
                        value,
                        "developer reputation test control"
                    );
                    EnclaveLocalHostilityService
                        .NotifyReputationChanged(camp, previousTier);
                    Messages.Message(
                        "Enclave reputation set to " +
                        camp.Data.Reputation +
                        " \u2014 " +
                        camp.Data.ReputationTierLabel +
                        ".",
                        MessageTypeDefOf.NeutralEvent
                    );
                }
            );
        }

        private static void ShowIdeologyTypeMenu(PilgrimCamp camp)
        {
            List<FloatMenuOption> options =
                new List<FloatMenuOption>();
            EnclaveIdeologyType[] types =
            {
                EnclaveIdeologyType.Communal,
                EnclaveIdeologyType.Isolationist,
                EnclaveIdeologyType.Martial,
                EnclaveIdeologyType.Mercantile,
                EnclaveIdeologyType.Nature,
                EnclaveIdeologyType.Spiritual,
                EnclaveIdeologyType.Transhumanist
            };

            foreach (EnclaveIdeologyType type in types)
            {
                EnclaveIdeologyType selectedType = type;

                options.Add(
                    new FloatMenuOption(
                        selectedType.ToString(),
                        delegate
                        {
                            SetIdeologyType(camp, selectedType);
                        }
                    )
                );
            }

            ShowValueMenu(options);
        }

        private static void SetIdeologyType(
            PilgrimCamp camp,
            EnclaveIdeologyType type
        )
        {
            EnclaveIdeologyType previous =
                EnclaveIdeologyUtility.GetIdeologyType(camp.Data);

            if (
                !EnclaveIdeologyUtility.SetIdeologyType(
                    camp.Data,
                    type,
                    "developer compatibility test control"
                )
            )
            {
                Messages.Message(
                    "The ideology type could not be changed.",
                    MessageTypeDefOf.RejectInput
                );
                return;
            }

            Messages.Message(
                "Enclave ideology type changed: " +
                previous +
                " \u2192 " +
                type +
                ". Actual Ideo unchanged.",
                MessageTypeDefOf.NeutralEvent
            );
        }

        private static void ShowDevelopmentTierMenu(PilgrimCamp camp)
        {
            List<FloatMenuOption> options =
                new List<FloatMenuOption>();
            EnclaveDevelopmentTier[] tiers =
            {
                EnclaveDevelopmentTier.TierI,
                EnclaveDevelopmentTier.TierII,
                EnclaveDevelopmentTier.TierIII,
                EnclaveDevelopmentTier.TierIV
            };

            foreach (EnclaveDevelopmentTier tier in tiers)
            {
                EnclaveDevelopmentTier selectedTier = tier;

                options.Add(
                    new FloatMenuOption(
                        EnclaveDevelopmentUtility.GetDisplayName(
                            selectedTier
                        ),
                        delegate
                        {
                            SetDevelopmentTier(camp, selectedTier);
                        }
                    )
                );
            }

            ShowValueMenu(options);
        }

        private static void ShowCampDevelopmentTestingMenu()
        {
            List<FloatMenuOption> options =
                new List<FloatMenuOption>();
            EnclaveDevelopmentTier[] tiers =
            {
                EnclaveDevelopmentTier.TierI,
                EnclaveDevelopmentTier.TierII,
                EnclaveDevelopmentTier.TierIII,
                EnclaveDevelopmentTier.TierIV
            };

            foreach (EnclaveDevelopmentTier tier in tiers)
            {
                EnclaveDevelopmentTier selectedTier = tier;

                options.Add(
                    new FloatMenuOption(
                        "Spawn " +
                        EnclaveDevelopmentUtility.GetDisplayName(
                            selectedTier
                        ) +
                        " Test Enclave",
                        delegate
                        {
                            EnclaveDevTools
                                .CreateDevelopmentTestEnclave(
                                    selectedTier
                                );
                        }
                    )
                );
            }

            ShowValueMenu(options);
        }

        private static void SetDevelopmentTier(
            PilgrimCamp camp,
            EnclaveDevelopmentTier tier
        )
        {
            EnclaveDevelopmentTier previous =
                EnclaveDevelopmentUtility.GetTier(camp.Data);

            if (
                !EnclaveDevelopmentUtility.SetTier(
                    camp.Data,
                    tier,
                    "developer development test control"
                )
            )
            {
                Messages.Message(
                    "The development tier could not be changed.",
                    MessageTypeDefOf.RejectInput
                );
                return;
            }

            Messages.Message(
                "Enclave development changed: " +
                EnclaveDevelopmentUtility.GetDisplayName(previous) +
                " \u2192 " +
                EnclaveDevelopmentUtility.GetDisplayName(tier) +
                ". Population unchanged at " +
                camp.Data.Population +
                ". Existing generated maps are not remodeled.",
                MessageTypeDefOf.NeutralEvent
            );
        }

        private static void ShowPresetMenu(PilgrimCamp camp)
        {
            ShowValueMenu(
                new List<FloatMenuOption>
                {
                    new FloatMenuOption(
                        "Friendly Test State",
                        delegate
                        {
                            ApplyPreset(
                                camp,
                                30,
                                EnclaveDevelopmentTier.TierII,
                                "Friendly"
                            );
                        }
                    ),
                    new FloatMenuOption(
                        "Revered Test State",
                        delegate
                        {
                            ApplyPreset(
                                camp,
                                90,
                                EnclaveDevelopmentTier.TierIV,
                                "Revered"
                            );
                        }
                    ),
                    new FloatMenuOption(
                        "Hostile Test State",
                        delegate
                        {
                            EnclaveReputationTier previousTier =
                                camp.Data.ReputationTier;

                            camp.Data.SetReputation(
                                -50,
                                "developer Hostile test preset"
                            );
                            EnclaveLocalHostilityService
                                .NotifyReputationChanged(
                                    camp,
                                    previousTier
                                );
                            Messages.Message(
                                "Applied Hostile test state. " +
                                "Development and ideology were unchanged.",
                                MessageTypeDefOf.NeutralEvent
                            );
                        }
                    )
                }
            );
        }

        private static void ApplyPreset(
            PilgrimCamp camp,
            int reputation,
            EnclaveDevelopmentTier tier,
            string label
        )
        {
            EnclaveReputationTier previousTier =
                camp.Data.ReputationTier;

            camp.Data.SetReputation(
                reputation,
                "developer " + label + " test preset"
            );
            EnclaveLocalHostilityService.NotifyReputationChanged(
                camp,
                previousTier
            );
            EnclaveDevelopmentUtility.SetTier(
                camp.Data,
                tier,
                "developer " + label + " test preset"
            );

            Messages.Message(
                "Applied " +
                label +
                " test state: reputation " +
                camp.Data.Reputation +
                " (" +
                camp.Data.ReputationTierLabel +
                "), " +
                EnclaveDevelopmentUtility.GetDisplayName(camp.Data) +
                ". Ideology unchanged.",
                MessageTypeDefOf.NeutralEvent
            );
        }

        private static void ShowSpawnEnclaveMenu(PilgrimCamp source)
        {
            ShowValueMenu(
                new List<FloatMenuOption>
                {
                    CreateSpawnOption(
                        source,
                        EnclaveDistanceBand.Strong
                    ),
                    CreateSpawnOption(
                        source,
                        EnclaveDistanceBand.Moderate
                    ),
                    CreateSpawnOption(
                        source,
                        EnclaveDistanceBand.Weak
                    )
                }
            );
        }

        private static FloatMenuOption CreateSpawnOption(
            PilgrimCamp source,
            EnclaveDistanceBand distanceBand
        )
        {
            return new FloatMenuOption(
                EnclaveProximityUtility.GetDistanceBandDisplayName(
                    distanceBand
                ) +
                " proximity",
                delegate
                {
                    PilgrimCamp spawned;
                    TrySpawnNearbyEnclave(
                        source,
                        distanceBand,
                        out spawned,
                        showMessage: true
                    );
                }
            );
        }

        private static bool TrySpawnNearbyEnclave(
            PilgrimCamp source,
            EnclaveDistanceBand distanceBand,
            out PilgrimCamp spawnedCamp,
            bool showMessage
        )
        {
            spawnedCamp = null;
            PlanetTile tile;

            if (!TryFindOpenTileInBand(source, distanceBand, out tile))
            {
                if (showMessage)
                {
                    Messages.Message(
                        "No safe unoccupied tile was found in " +
                        EnclaveProximityUtility
                            .GetDistanceBandDisplayName(distanceBand) +
                        " proximity.",
                        MessageTypeDefOf.RejectInput
                    );
                }

                return false;
            }

            WorldObjectDef def =
                DefDatabase<WorldObjectDef>.GetNamedSilentFail(
                    "IEE_PilgrimCamp"
                );

            if (def == null)
            {
                return false;
            }

            spawnedCamp =
                (PilgrimCamp)WorldObjectMaker.MakeWorldObject(def);
            spawnedCamp.Data = EnclaveGenerator.Generate();
            spawnedCamp.Tile = tile;

            EnclaveFactionUtility.GetOrCreateFaction();
            Find.WorldObjects.Add(spawnedCamp);
            RegisterCreatedWorldObject(spawnedCamp);

            float actualDistance =
                EnclaveProximityUtility.GetDistanceInTiles(
                    source,
                    spawnedCamp
                );
            string result =
                "Spawned " +
                spawnedCamp.Data.Name +
                " at tile " +
                spawnedCamp.Tile +
                ", " +
                actualDistance.ToString("0.#") +
                " tiles away (" +
                EnclaveProximityUtility.GetDistanceBandDisplayName(
                    EnclaveProximityUtility.GetDistanceBand(
                        actualDistance
                    )
                ) +
                ").";

            Log.Message("[IEE] DEV " + result);

            if (showMessage)
            {
                Messages.Message(
                    result,
                    MessageTypeDefOf.PositiveEvent
                );
            }

            return true;
        }

        private static void ShowRelationshipNeighborMenu(
            PilgrimCamp source
        )
        {
            List<EnclaveNeighborInfo> neighbors =
                EnclaveProximityUtility.GetNearbyEnclaves(source);

            if (neighbors.Count == 0)
            {
                Messages.Message(
                    "No nearby enclave is available within 30 tiles.",
                    MessageTypeDefOf.RejectInput
                );
                return;
            }

            List<FloatMenuOption> options =
                new List<FloatMenuOption>();

            foreach (EnclaveNeighborInfo neighbor in neighbors)
            {
                PilgrimCamp other =
                    neighbor.WorldObject as PilgrimCamp;

                if (other == null)
                {
                    continue;
                }

                PilgrimCamp selectedOther = other;

                options.Add(
                    new FloatMenuOption(
                        neighbor.Label +
                        " \u2014 " +
                        neighbor.DistanceInTiles.ToString("0.#") +
                        " tiles",
                        delegate
                        {
                            ShowRelationshipActions(
                                source,
                                selectedOther
                            );
                        }
                    )
                );
            }

            ShowValueMenu(options);
        }

        private static void ShowRelationshipActions(
            PilgrimCamp first,
            PilgrimCamp second
        )
        {
            ShowValueMenu(
                new List<FloatMenuOption>
                {
                    new FloatMenuOption(
                        "Show Relationship Details",
                        delegate
                        {
                            ShowRelationshipDetails(first, second);
                        }
                    ),
                    new FloatMenuOption(
                        "Reset Relationship to Baseline",
                        delegate
                        {
                            ResetRelationship(first, second);
                        }
                    ),
                    new FloatMenuOption(
                        "Set Relationship",
                        delegate
                        {
                            ShowSetRelationshipMenu(first, second);
                        }
                    )
                }
            );
        }

        private static void ShowRelationshipDetails(
            PilgrimCamp first,
            PilgrimCamp second
        )
        {
            EnclaveIdeologyCompatibility compatibility =
                EnclaveIdeologyCompatibilityUtility.GetCompatibility(
                    first.Data,
                    second.Data
                );
            float distance =
                EnclaveProximityUtility.GetDistanceInTiles(
                    first,
                    second
                );
            EnclaveDistanceBand distanceBand =
                EnclaveProximityUtility.GetDistanceBand(distance);
            int baseline =
                InterEnclaveRelationshipUtility.CalculateInitialScore(
                    first,
                    second
                );
            InterEnclaveRelationshipRecord relationship =
                InterEnclaveRelationshipUtility.GetRelationship(
                    first,
                    second
                );
            StringBuilder report = new StringBuilder();

            report.AppendLine(
                first.Data.Name +
                " (ID " +
                first.ID +
                ") \u2194 " +
                second.Data.Name +
                " (ID " +
                second.ID +
                ")"
            );
            report.AppendLine(
                "Ideology types: " +
                EnclaveIdeologyUtility.GetTypeLabel(first.Data) +
                " / " +
                EnclaveIdeologyUtility.GetTypeLabel(second.Data)
            );
            report.AppendLine(
                "Compatibility: " +
                EnclaveIdeologyCompatibilityUtility.GetDisplayName(
                    compatibility
                ) +
                " (" +
                FormatSignedScore((int)compatibility) +
                ")"
            );
            report.AppendLine(
                "Distance: " +
                distance.ToString("0.#") +
                " tiles \u2014 " +
                EnclaveProximityUtility.GetDistanceBandDisplayName(
                    distanceBand
                )
            );
            report.AppendLine(
                "Current baseline: " +
                FormatSignedScore(baseline) +
                " (" +
                InterEnclaveRelationshipUtility.GetState(baseline) +
                ")"
            );
            report.AppendLine(
                "Persisted relationship: " +
                (relationship == null
                    ? "Unavailable"
                    : FormatSignedScore(relationship.Score) +
                        " (" +
                        InterEnclaveRelationshipUtility.GetState(
                            relationship.Score
                        ) +
                        ")")
            );

            ShowReport(
                "DEV relationship details",
                report.ToString().TrimEnd()
            );
        }

        private static void ResetRelationship(
            PilgrimCamp first,
            PilgrimCamp second
        )
        {
            InterEnclaveRelationshipRecord relationship =
                InterEnclaveRelationshipUtility
                    .ResetRelationshipToBaseline(first, second);

            Messages.Message(
                relationship == null
                    ? "The relationship could not be reset."
                    : "Relationship reset to baseline " +
                        FormatSignedScore(relationship.Score) +
                        " (" +
                        InterEnclaveRelationshipUtility.GetState(
                            relationship.Score
                        ) +
                        ").",
                relationship == null
                    ? MessageTypeDefOf.RejectInput
                    : MessageTypeDefOf.PositiveEvent
            );
        }

        private static void ShowSetRelationshipMenu(
            PilgrimCamp first,
            PilgrimCamp second
        )
        {
            List<FloatMenuOption> options =
                new List<FloatMenuOption>();
            InterEnclaveRelationshipState[] states =
            {
                InterEnclaveRelationshipState.Hostile,
                InterEnclaveRelationshipState.Rival,
                InterEnclaveRelationshipState.Neutral,
                InterEnclaveRelationshipState.Friendly,
                InterEnclaveRelationshipState.Allied
            };

            foreach (InterEnclaveRelationshipState state in states)
            {
                InterEnclaveRelationshipState selectedState = state;

                options.Add(
                    new FloatMenuOption(
                        selectedState.ToString(),
                        delegate
                        {
                            int score = InterEnclaveRelationshipUtility
                                .SetRelationshipState(
                                    first,
                                    second,
                                    selectedState,
                                    "developer relationship test control"
                                );

                            Messages.Message(
                                "Relationship set to " +
                                selectedState +
                                " (" +
                                FormatSignedScore(score) +
                                ").",
                                MessageTypeDefOf.NeutralEvent
                            );
                        }
                    )
                );
            }

            ShowValueMenu(options);
        }

        private static void CreateRegionalTestScenario(
            PilgrimCamp source
        )
        {
            StringBuilder report = new StringBuilder();
            List<EnclaveNeighborInfo> playerSettlements =
                EnclaveProximityUtility.GetNearbyPlayerSettlements(
                    source
                );
            EnclaveNeighborInfo strongPlayerSettlement =
                playerSettlements.Find(
                    neighbor =>
                        neighbor.DistanceBand ==
                            EnclaveDistanceBand.Strong
                );

            report.AppendLine("Regional test scenario:");

            if (strongPlayerSettlement == null)
            {
                report.AppendLine(
                    "- Player settlement: skipped; no existing player " +
                    "settlement is within Strong proximity, and the " +
                    "harness does not fabricate player colonies."
                );
            }
            else
            {
                report.AppendLine(
                    "- Player settlement: " +
                    strongPlayerSettlement.Label +
                    " at " +
                    strongPlayerSettlement.DistanceInTiles
                        .ToString("0.#") +
                    " tiles (existing)."
                );
            }

            PilgrimCamp nearbyEnclave;

            if (
                TrySpawnNearbyEnclave(
                    source,
                    EnclaveDistanceBand.Moderate,
                    out nearbyEnclave,
                    showMessage: false
                )
            )
            {
                report.AppendLine(
                    "- Enclave: " +
                    nearbyEnclave.Data.Name +
                    " at " +
                    EnclaveProximityUtility.GetDistanceInTiles(
                        source,
                        nearbyEnclave
                    ).ToString("0.#") +
                    " tiles."
                );
            }
            else
            {
                report.AppendLine(
                    "- Enclave: skipped; no safe Moderate tile found."
                );
            }

            Settlement factionSettlement;

            if (
                TrySpawnFactionSettlement(
                    source,
                    EnclaveDistanceBand.Weak,
                    out factionSettlement
                )
            )
            {
                report.AppendLine(
                    "- Faction settlement: " +
                    factionSettlement.LabelCap +
                    " at " +
                    EnclaveProximityUtility.GetDistanceInTiles(
                        source,
                        factionSettlement
                    ).ToString("0.#") +
                    " tiles."
                );
            }
            else
            {
                report.AppendLine(
                    "- Faction settlement: skipped; no safe faction " +
                    "or Weak-proximity tile was available."
                );
            }

            report.AppendLine(
                "Use Show Nearby Influence or Enclave Overview to " +
                "inspect the production calculations."
            );

            ShowReport(
                "DEV regional test scenario",
                report.ToString().TrimEnd()
            );
        }

        private static bool TrySpawnFactionSettlement(
            PilgrimCamp source,
            EnclaveDistanceBand distanceBand,
            out Settlement settlement
        )
        {
            settlement = null;
            Faction faction = FindTestSettlementFaction();
            PlanetTile tile;

            if (
                faction == null ||
                !TryFindOpenTileInBand(source, distanceBand, out tile)
            )
            {
                return false;
            }

            settlement = (Settlement)WorldObjectMaker.MakeWorldObject(
                WorldObjectDefOf.Settlement
            );
            settlement.Tile = tile;
            settlement.SetFaction(faction);
            settlement.Name =
                "DEV " +
                (faction.Name ?? "Faction") +
                " Test Settlement";

            Find.WorldObjects.Add(settlement);
            RegisterCreatedWorldObject(settlement);

            Log.Message(
                "[IEE] DEV spawned faction settlement " +
                settlement.LabelCap +
                " for " +
                faction.Name +
                " at tile " +
                tile +
                "."
            );

            return true;
        }

        private static Faction FindTestSettlementFaction()
        {
            Faction player = Faction.OfPlayerSilentFail;
            Faction fallback = null;

            if (Find.FactionManager?.AllFactionsListForReading == null)
            {
                return null;
            }

            foreach (
                Faction faction in
                Find.FactionManager.AllFactionsListForReading
            )
            {
                if (
                    faction == null ||
                    faction == player ||
                    faction.Hidden ||
                    faction.defeated ||
                    faction.def == null ||
                    !faction.def.humanlikeFaction ||
                    EnclaveFactionUtility.IsEnclaveFaction(faction)
                )
                {
                    continue;
                }

                if (
                    player != null &&
                    faction.RelationKindWith(player) ==
                        FactionRelationKind.Neutral
                )
                {
                    return faction;
                }

                if (fallback == null)
                {
                    fallback = faction;
                }
            }

            return fallback;
        }

        private static bool TryFindOpenTileInBand(
            PilgrimCamp source,
            EnclaveDistanceBand distanceBand,
            out PlanetTile tile
        )
        {
            tile = PlanetTile.Invalid;

            if (
                source == null ||
                !source.Tile.Valid ||
                Find.WorldGrid == null ||
                Find.WorldObjects == null
            )
            {
                return false;
            }

            int preferredMinimum;
            int preferredMaximum;
            int bandMinimum;
            int bandMaximum;

            GetDistanceRanges(
                distanceBand,
                out preferredMinimum,
                out preferredMaximum,
                out bandMinimum,
                out bandMaximum
            );

            return
                TryFindOpenTile(
                    source,
                    distanceBand,
                    preferredMinimum,
                    preferredMaximum,
                    out tile
                ) ||
                TryFindOpenTile(
                    source,
                    distanceBand,
                    bandMinimum,
                    bandMaximum,
                    out tile
                );
        }

        private static bool TryFindOpenTile(
            PilgrimCamp source,
            EnclaveDistanceBand distanceBand,
            int minimum,
            int maximum,
            out PlanetTile tile
        )
        {
            return TileFinder.TryFindTileWithDistance(
                source.Tile,
                minimum,
                maximum,
                out tile,
                candidate =>
                    candidate.Valid &&
                    candidate.Layer == source.Tile.Layer &&
                    !Find.WorldObjects.AnyWorldObjectAt(candidate) &&
                    TileFinder.IsValidTileForNewSettlement(candidate) &&
                    EnclaveProximityUtility.GetDistanceBand(
                        Find.WorldGrid.ApproxDistanceInTiles(
                            source.Tile,
                            candidate
                        )
                    ) == distanceBand
            );
        }

        private static void GetDistanceRanges(
            EnclaveDistanceBand distanceBand,
            out int preferredMinimum,
            out int preferredMaximum,
            out int bandMinimum,
            out int bandMaximum
        )
        {
            switch (distanceBand)
            {
                case EnclaveDistanceBand.Strong:
                    preferredMinimum = 7;
                    preferredMaximum = 9;
                    bandMinimum = 1;
                    bandMaximum = 10;
                    break;
                case EnclaveDistanceBand.Moderate:
                    preferredMinimum = 14;
                    preferredMaximum = 16;
                    bandMinimum = 11;
                    bandMaximum = 20;
                    break;
                default:
                    preferredMinimum = 24;
                    preferredMaximum = 26;
                    bandMinimum = 21;
                    bandMaximum = 30;
                    break;
            }
        }

        private static void CleanTrackedTestNeighbors(
            PilgrimCamp selectedCamp
        )
        {
            EnsureTrackingWorld();

            List<WorldObject> toRemove = new List<WorldObject>();
            int skippedMaps = 0;

            foreach (
                WorldObject worldObject in
                Find.WorldObjects.AllWorldObjects
            )
            {
                if (
                    worldObject == null ||
                    worldObject == selectedCamp ||
                    !createdWorldObjectIds.Contains(worldObject.ID)
                )
                {
                    continue;
                }

                MapParent mapParent = worldObject as MapParent;

                if (mapParent?.HasMap == true)
                {
                    skippedMaps++;
                    continue;
                }

                toRemove.Add(worldObject);
            }

            foreach (WorldObject worldObject in toRemove)
            {
                createdWorldObjectIds.Remove(worldObject.ID);
                Find.WorldObjects.Remove(worldObject);
            }

            Messages.Message(
                "Removed " +
                toRemove.Count +
                " test neighbor(s) created by this runtime session." +
                (skippedMaps > 0
                    ? " Skipped " +
                        skippedMaps +
                        " object(s) with generated maps."
                    : string.Empty),
                MessageTypeDefOf.NeutralEvent
            );
        }

        private static void EnsureTrackingWorld()
        {
            if (object.ReferenceEquals(trackedWorld, Find.World))
            {
                return;
            }

            trackedWorld = Find.World;
            createdWorldObjectIds.Clear();
        }

        private static void ShowValueMenu(
            List<FloatMenuOption> options
        )
        {
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void ShowReport(
            string logLabel,
            string report
        )
        {
            Log.Message("[IEE] " + logLabel + "\n" + report);
            Find.WindowStack.Add(new Dialog_MessageBox(report));
        }

        private static bool CanUse(PilgrimCamp camp)
        {
            if (!Prefs.DevMode)
            {
                return false;
            }

            if (camp?.Data != null)
            {
                return true;
            }

            Messages.Message(
                "The selected Pilgrim Camp has no enclave data.",
                MessageTypeDefOf.RejectInput
            );
            return false;
        }

        private static string GetNeighborTypeDisplayName(
            EnclaveNeighborType neighborType
        )
        {
            switch (neighborType)
            {
                case EnclaveNeighborType.PlayerSettlement:
                    return "Player Colony";
                case EnclaveNeighborType.Enclave:
                    return "Enclave";
                case EnclaveNeighborType.FriendlyFactionSettlement:
                    return "Friendly Settlement";
                case EnclaveNeighborType.HostileFactionSettlement:
                    return "Hostile Settlement";
                default:
                    return "Neutral Settlement";
            }
        }

        private static string FormatSignedScore(int score)
        {
            return score >= 0
                ? "+" + score
                : score.ToString();
        }
    }
}
