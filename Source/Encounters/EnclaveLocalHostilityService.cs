using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveLocalHostilityService
    {
        private const float PeacefulWanderRadius = 10f;
        private const float PeacefulDefendRadius = 24f;

        public static void NotifyReputationChanged(
            PilgrimCamp camp,
            EnclaveReputationTier previousTier
        )
        {
            bool wasHostile =
                previousTier == EnclaveReputationTier.Hostile;
            bool isHostile =
                EnclaveRelationshipUtility.IsLocallyHostile(camp);

            if (wasHostile == isHostile)
            {
                return;
            }

            UpdateCampCombatState(camp, notifyPlayer: true);
        }

        public static bool UpdateCampCombatState(
            PilgrimCamp camp,
            bool notifyPlayer = false
        )
        {
            if (camp?.Data == null || camp.Map == null)
            {
                return false;
            }

            bool shouldBeHostile =
                EnclaveRelationshipUtility.IsLocallyHostile(camp) &&
                camp.VisitingGroup != null &&
                camp.VisitingGroup.HasActiveMembers(camp);

            return shouldBeHostile
                ? ActivateLocalHostility(camp, notifyPlayer)
                : RestorePeacefulBehavior(camp, notifyPlayer);
        }

        public static string GetCombatStateLabel(PilgrimCamp camp)
        {
            if (camp?.Map == null)
            {
                return "Map not generated";
            }

            Lord lord = FindCampLord(camp, preferHostileLord: true);

            if (lord?.LordJob is LordJob_EnclaveLocalHostility)
            {
                return "Hostile local-defense Lord active";
            }

            if (
                EnclaveRelationshipUtility.IsLocallyHostile(camp) &&
                camp.VisitingGroup?.HasActiveMembers(camp) == true
            )
            {
                return "Hostile reputation; local-defense Lord inactive";
            }

            return lord == null
                ? "No active enclave Lord"
                : "Peaceful Lord active (" +
                    lord.LordJob.GetType().Name +
                    ")";
        }

        public static bool IsGenuineCampMember(
            PilgrimCamp camp,
            Pawn pawn
        )
        {
            return
                camp?.Map != null &&
                pawn != null &&
                !pawn.Destroyed &&
                !pawn.Dead &&
                pawn.Spawned &&
                pawn.Map == camp.Map &&
                pawn.Faction != null &&
                EnclaveFactionUtility.IsEnclaveFaction(pawn.Faction) &&
                camp.PawnMembers != null &&
                camp.PawnMembers.Contains(pawn);
        }

        public static bool IsValidVisitingTarget(
            PilgrimCamp camp,
            Pawn target
        )
        {
            if (
                camp?.VisitingGroup == null ||
                target == null ||
                target.Destroyed ||
                target.Dead ||
                target.Downed ||
                !target.Spawned ||
                target.Map != camp.Map ||
                target.Faction != Faction.OfPlayer
            )
            {
                return false;
            }

            foreach (Pawn member in camp.VisitingGroup.ActiveMembers(camp))
            {
                if (member == target)
                {
                    return true;
                }
            }

            return false;
        }

        public static Pawn FindClosestVisitingTarget(
            PilgrimCamp camp,
            Pawn attacker
        )
        {
            if (
                !EnclaveRelationshipUtility.IsLocallyHostile(camp) ||
                !IsGenuineCampMember(camp, attacker) ||
                attacker.Downed ||
                attacker.InMentalState ||
                attacker.Map == null
            )
            {
                return null;
            }

            Pawn closest = null;
            int closestDistance = int.MaxValue;

            foreach (Pawn target in camp.VisitingGroup.ActiveMembers(camp))
            {
                if (
                    !IsValidVisitingTarget(camp, target) ||
                    !attacker.CanReach(
                        target,
                        PathEndMode.Touch,
                        Danger.Deadly
                    )
                )
                {
                    continue;
                }

                int distance =
                    (attacker.Position - target.Position)
                        .LengthHorizontalSquared;

                if (distance < closestDistance)
                {
                    closest = target;
                    closestDistance = distance;
                }
            }

            return closest;
        }

        private static bool ActivateLocalHostility(
            PilgrimCamp camp,
            bool notifyPlayer
        )
        {
            List<Pawn> members = GetActiveCampMembers(camp);

            if (members.Count == 0)
            {
                return false;
            }

            Lord lord = FindCampLord(camp, preferHostileLord: true);
            LordJob_EnclaveLocalHostility localHostilityJob =
                lord?.LordJob as LordJob_EnclaveLocalHostility;
            bool changed = localHostilityJob == null;

            if (lord == null)
            {
                IntVec3 defendPoint = ResolveDefendPoint(camp, null);

                lord = LordMaker.MakeNewLord(
                    EnclaveFactionUtility.GetOrCreateFaction(),
                    new LordJob_EnclaveLocalHostility(
                        camp,
                        defendPoint
                    ),
                    camp.Map,
                    members
                );
            }
            else
            {
                if (localHostilityJob == null)
                {
                    IntVec3 defendPoint =
                        ResolveDefendPoint(camp, lord);

                    SetLordJob(
                        lord,
                        new LordJob_EnclaveLocalHostility(
                            camp,
                            defendPoint
                        )
                    );
                }

                EnsureLordMembers(lord, members);
            }

            if (changed)
            {
                InterruptForCombat(members);

                Log.Message(
                    "[IEE] Activated camp-local hostility for " +
                    (camp.Data?.Name ?? "an enclave") +
                    " against " +
                    camp.VisitingGroup.ActiveMembersList(camp).Count +
                    " registered visiting-group member(s)."
                );

                if (notifyPlayer)
                {
                    Messages.Message(
                        (camp.Data?.Name ?? "The enclave") +
                        " is hostile toward your colony.",
                        MessageTypeDefOf.ThreatBig
                    );
                }
            }

            return changed;
        }

        private static bool RestorePeacefulBehavior(
            PilgrimCamp camp,
            bool notifyPlayer
        )
        {
            Lord lord = FindCampLord(camp, preferHostileLord: true);
            LordJob_EnclaveLocalHostility localHostilityJob =
                lord?.LordJob as LordJob_EnclaveLocalHostility;

            if (localHostilityJob == null)
            {
                return false;
            }

            IntVec3 defendPoint = localHostilityJob.DefendPoint.IsValid
                ? localHostilityJob.DefendPoint
                : ResolveDefendPoint(camp, lord);

            SetLordJob(
                lord,
                new LordJob_DefendPoint(
                    defendPoint,
                    PeacefulWanderRadius,
                    PeacefulDefendRadius,
                    isCaravanSendable: false,
                    addFleeToil: true
                )
            );

            StopLocalAttackJobs(camp);

            Log.Message(
                "[IEE] Restored peaceful camp behavior for " +
                (camp.Data?.Name ?? "an enclave") +
                "."
            );

            if (
                notifyPlayer &&
                !EnclaveRelationshipUtility.IsLocallyHostile(camp)
            )
            {
                Messages.Message(
                    (camp.Data?.Name ?? "The enclave") +
                    " is no longer locally hostile.",
                    MessageTypeDefOf.NeutralEvent
                );
            }

            return true;
        }

        private static List<Pawn> GetActiveCampMembers(
            PilgrimCamp camp
        )
        {
            List<Pawn> members = new List<Pawn>();

            if (camp?.PawnMembers?.Members == null)
            {
                return members;
            }

            foreach (Pawn pawn in camp.PawnMembers.Members)
            {
                if (IsGenuineCampMember(camp, pawn))
                {
                    members.Add(pawn);
                }
            }

            return members;
        }

        private static Lord FindCampLord(
            PilgrimCamp camp,
            bool preferHostileLord
        )
        {
            if (camp?.PawnMembers?.Members == null)
            {
                return null;
            }

            Dictionary<Lord, int> memberCounts =
                new Dictionary<Lord, int>();
            Lord mostPopulatedLord = null;
            int mostMembers = 0;

            foreach (Pawn pawn in camp.PawnMembers.Members)
            {
                if (!IsGenuineCampMember(camp, pawn))
                {
                    continue;
                }

                Lord lord = pawn.GetLord();

                if (lord?.Map != camp.Map)
                {
                    continue;
                }

                if (
                    preferHostileLord &&
                    lord.LordJob is LordJob_EnclaveLocalHostility
                )
                {
                    return lord;
                }

                int count;
                memberCounts.TryGetValue(lord, out count);
                count++;
                memberCounts[lord] = count;

                if (count > mostMembers)
                {
                    mostPopulatedLord = lord;
                    mostMembers = count;
                }
            }

            return mostPopulatedLord;
        }

        private static void EnsureLordMembers(
            Lord targetLord,
            List<Pawn> members
        )
        {
            foreach (Pawn pawn in members)
            {
                Lord currentLord = pawn.GetLord();

                if (currentLord == targetLord)
                {
                    continue;
                }

                currentLord?.RemovePawn(pawn);
                targetLord.AddPawn(pawn);
            }
        }

        private static void SetLordJob(
            Lord lord,
            LordJob lordJob
        )
        {
            lord.SetJob(lordJob, false);

            if (
                lord.CurLordToil == null &&
                lord.Graph?.StartingToil != null
            )
            {
                lord.GotoToil(lord.Graph.StartingToil);
            }
        }

        private static IntVec3 ResolveDefendPoint(
            PilgrimCamp camp,
            Lord lord
        )
        {
            IntVec3 flagLoc = lord?.CurLordToil?.FlagLoc ??
                IntVec3.Invalid;

            if (flagLoc.IsValid && flagLoc.InBounds(camp.Map))
            {
                return flagLoc;
            }

            List<Thing> campfires =
                camp.Map.listerThings.ThingsOfDef(ThingDefOf.Campfire);

            if (campfires != null && campfires.Count > 0)
            {
                return campfires[0].Position;
            }

            return camp.Map.Center;
        }

        private static void InterruptForCombat(List<Pawn> members)
        {
            foreach (Pawn pawn in members)
            {
                if (
                    pawn.Downed ||
                    pawn.InMentalState ||
                    pawn.jobs?.curJob == null
                )
                {
                    continue;
                }

                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
        }

        private static void StopLocalAttackJobs(PilgrimCamp camp)
        {
            if (camp?.PawnMembers?.Members == null)
            {
                return;
            }

            foreach (Pawn pawn in camp.PawnMembers.Members)
            {
                if (
                    !IsGenuineCampMember(camp, pawn) ||
                    pawn.mindState == null
                )
                {
                    continue;
                }

                pawn.mindState.enemyTarget = null;

                if (
                    pawn.jobs?.curJob?.def == JobDefOf.AttackMelee ||
                    pawn.jobs?.curJob?.def == JobDefOf.AttackStatic
                )
                {
                    pawn.jobs.EndCurrentJob(
                        JobCondition.InterruptForced
                    );
                }
            }
        }
    }

    [DefOf]
    public static class EnclaveDutyDefOf
    {
        public static DutyDef IEE_EnclaveLocalHostility;

        static EnclaveDutyDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(
                typeof(EnclaveDutyDefOf)
            );
        }
    }

    public class LordJob_EnclaveLocalHostility : LordJob
    {
        private PilgrimCamp camp;
        private IntVec3 defendPoint = IntVec3.Invalid;

        public PilgrimCamp Camp => camp;
        public IntVec3 DefendPoint => defendPoint;

        public LordJob_EnclaveLocalHostility()
        {
        }

        public LordJob_EnclaveLocalHostility(
            PilgrimCamp camp,
            IntVec3 defendPoint
        )
        {
            this.camp = camp;
            this.defendPoint = defendPoint;
        }

        public override StateGraph CreateGraph()
        {
            StateGraph graph = new StateGraph();
            graph.AddToil(new LordToil_EnclaveLocalHostility());
            return graph;
        }

        public override bool ValidateAttackTarget(
            Pawn searcher,
            Thing target
        )
        {
            return
                EnclaveLocalHostilityService.IsGenuineCampMember(
                    camp,
                    searcher
                ) &&
                EnclaveLocalHostilityService.IsValidVisitingTarget(
                    camp,
                    target as Pawn
                );
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref camp, "pilgrimCamp");
            Scribe_Values.Look(
                ref defendPoint,
                "defendPoint",
                IntVec3.Invalid
            );
        }
    }

    public class LordToil_EnclaveLocalHostility : LordToil
    {
        public override bool AllowSatisfyLongNeeds => false;
        public override bool ForceHighStoryDanger => true;

        public override void UpdateAllDuties()
        {
            PilgrimCamp camp =
                (lord?.LordJob as LordJob_EnclaveLocalHostility)
                    ?.Camp;

            foreach (Pawn pawn in lord.ownedPawns)
            {
                if (
                    EnclaveLocalHostilityService.IsGenuineCampMember(
                        camp,
                        pawn
                    )
                )
                {
                    pawn.mindState.duty = new PawnDuty(
                        EnclaveDutyDefOf.IEE_EnclaveLocalHostility
                    );
                }
            }
        }
    }

    public class JobGiver_EnclaveAttackVisitingGroup
        : JobGiver_AIFightEnemies
    {
        public JobGiver_EnclaveAttackVisitingGroup()
        {
            targetAcquireRadius = 9999f;
            targetKeepRadius = 9999f;
            chaseTarget = true;
            allowTurrets = false;
            ignoreNonCombatants = false;
            humanlikesOnly = false;
        }

        protected override Thing FindAttackTarget(Pawn pawn)
        {
            PilgrimCamp camp = pawn?.Map?.Parent as PilgrimCamp;

            return EnclaveLocalHostilityService
                .FindClosestVisitingTarget(camp, pawn);
        }

        protected override bool ExtraTargetValidator(
            Pawn pawn,
            Thing target
        )
        {
            PilgrimCamp camp = pawn?.Map?.Parent as PilgrimCamp;

            return
                EnclaveLocalHostilityService.IsGenuineCampMember(
                    camp,
                    pawn
                ) &&
                EnclaveLocalHostilityService.IsValidVisitingTarget(
                    camp,
                    target as Pawn
                );
        }

        protected override bool ShouldLoseTarget(Pawn pawn)
        {
            PilgrimCamp camp = pawn?.Map?.Parent as PilgrimCamp;

            return
                !EnclaveRelationshipUtility.IsLocallyHostile(camp) ||
                !EnclaveLocalHostilityService.IsGenuineCampMember(
                    camp,
                    pawn
                ) ||
                !EnclaveLocalHostilityService.IsValidVisitingTarget(
                    camp,
                    pawn?.mindState?.enemyTarget as Pawn
                );
        }
    }
}
