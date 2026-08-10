using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public class EnclaveVisitingGroup : IExposable
    {
        private List<Pawn> members = new List<Pawn>();

        public IReadOnlyList<Pawn> Members => members;
        public bool HasStoredMembers => members != null && members.Count > 0;

        public void Capture(Caravan caravan)
        {
            members.Clear();

            if (caravan?.PawnsListForReading == null)
            {
                return;
            }

            foreach (Pawn pawn in caravan.PawnsListForReading)
            {
                AddCapturedMember(pawn);
            }
        }

        public void RecoverFromMap(Map map)
        {
            members.Clear();

            if (map?.mapPawns?.AllPawnsSpawned == null)
            {
                return;
            }

            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                AddCapturedMember(pawn);
            }
        }

        public IEnumerable<Pawn> ActiveMembers(PilgrimCamp camp)
        {
            if (members == null || camp?.Map == null)
            {
                yield break;
            }

            foreach (Pawn pawn in members)
            {
                if (IsActiveMember(camp, pawn))
                {
                    yield return pawn;
                }
            }
        }

        public List<Pawn> ActiveMembersList(PilgrimCamp camp)
        {
            return new List<Pawn>(ActiveMembers(camp));
        }

        public IEnumerable<Thing> InventoryThings(PilgrimCamp camp)
        {
            foreach (Pawn pawn in ActiveMembers(camp))
            {
                if (pawn.inventory == null)
                {
                    continue;
                }

                foreach (Thing thing in pawn.inventory.innerContainer)
                {
                    if (thing != null && !thing.Destroyed)
                    {
                        yield return thing;
                    }
                }
            }
        }

        public bool ContainsInventoryThing(
            PilgrimCamp camp,
            Thing thing
        )
        {
            if (thing == null || thing.Destroyed)
            {
                return false;
            }

            foreach (Pawn pawn in ActiveMembers(camp))
            {
                if (
                    pawn.inventory != null &&
                    pawn.inventory.innerContainer.Contains(thing)
                )
                {
                    return true;
                }
            }

            return false;
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(
                ref members,
                "members",
                LookMode.Reference
            );

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (members == null)
                {
                    members = new List<Pawn>();
                }
                else
                {
                    members.RemoveAll(pawn => pawn == null);
                }
            }
        }

        private void AddCapturedMember(Pawn pawn)
        {
            if (
                pawn != null &&
                !pawn.Destroyed &&
                pawn.Faction == Faction.OfPlayer &&
                !members.Contains(pawn)
            )
            {
                members.Add(pawn);
            }
        }

        private static bool IsActiveMember(
            PilgrimCamp camp,
            Pawn pawn
        )
        {
            return
                pawn != null &&
                !pawn.Destroyed &&
                !pawn.Dead &&
                pawn.Faction == Faction.OfPlayer &&
                pawn.Spawned &&
                pawn.Map == camp.Map;
        }
    }
}
