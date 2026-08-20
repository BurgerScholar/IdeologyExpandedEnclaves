using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveLayoutPlacementUtility
    {
        public static bool TryPlaceBuilding(
            LayoutContext context,
            LayoutZone zone,
            ThingDef buildingDef,
            ThingDef stuffDef,
            Rot4 rotation,
            IEnumerable<IntVec3> preferredCells,
            out Thing placedThing
        )
        {
            placedThing = null;

            if (
                context?.Map == null ||
                zone == null ||
                buildingDef == null
            )
            {
                return false;
            }

            HashSet<IntVec3> checkedCells = new HashSet<IntVec3>();

            if (preferredCells != null)
            {
                foreach (IntVec3 cell in preferredCells)
                {
                    if (
                        checkedCells.Add(cell) &&
                        TryPlaceBuildingAt(
                            context,
                            zone,
                            buildingDef,
                            stuffDef,
                            rotation,
                            cell,
                            out placedThing
                        )
                    )
                    {
                        return true;
                    }
                }
            }

            foreach (
                IntVec3 cell in
                GetRadialCells(zone.Area.CenterCell, zone)
            )
            {
                if (
                    checkedCells.Add(cell) &&
                    TryPlaceBuildingAt(
                        context,
                        zone,
                        buildingDef,
                        stuffDef,
                        rotation,
                        cell,
                        out placedThing
                    )
                )
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryPlaceItem(
            LayoutContext context,
            LayoutZone zone,
            ThingDef itemDef,
            int stackCount,
            IEnumerable<IntVec3> preferredCells,
            out Thing placedThing
        )
        {
            placedThing = null;

            if (
                context?.Map == null ||
                zone == null ||
                itemDef == null ||
                stackCount <= 0
            )
            {
                return false;
            }

            HashSet<IntVec3> checkedCells = new HashSet<IntVec3>();

            if (preferredCells != null)
            {
                foreach (IntVec3 cell in preferredCells)
                {
                    if (
                        checkedCells.Add(cell) &&
                        TryPlaceItemAt(
                            context,
                            zone,
                            itemDef,
                            stackCount,
                            cell,
                            out placedThing
                        )
                    )
                    {
                        return true;
                    }
                }
            }

            foreach (
                IntVec3 cell in
                GetRadialCells(zone.Area.CenterCell, zone)
            )
            {
                if (
                    checkedCells.Add(cell) &&
                    TryPlaceItemAt(
                        context,
                        zone,
                        itemDef,
                        stackCount,
                        cell,
                        out placedThing
                    )
                )
                {
                    return true;
                }
            }

            return false;
        }

        public static List<IntVec3> GetCenteredGridCells(
            IntVec3 center,
            int count,
            int columns,
            int spacing,
            bool staggerRows = false
        )
        {
            List<IntVec3> cells = new List<IntVec3>();

            if (count <= 0)
            {
                return cells;
            }

            columns = Math.Max(1, columns);
            spacing = Math.Max(1, spacing);
            int rows = (count + columns - 1) / columns;
            int xStart = -((columns - 1) * spacing) / 2;
            int zStart = -((rows - 1) * spacing) / 2;

            for (int index = 0; index < count; index++)
            {
                int row = index / columns;
                int column = index % columns;
                int x = xStart + column * spacing;

                if (staggerRows && row % 2 == 1)
                {
                    x++;
                }

                cells.Add(
                    center +
                    new IntVec3(
                        x,
                        0,
                        zStart + row * spacing
                    )
                );
            }

            return cells;
        }

        public static List<IntVec3> GetRingCells(
            IntVec3 center,
            int distance
        )
        {
            distance = Math.Max(1, distance);

            return new List<IntVec3>
            {
                center + new IntVec3(-distance, 0, 0),
                center + new IntVec3(distance, 0, 0),
                center + new IntVec3(0, 0, -distance),
                center + new IntVec3(0, 0, distance),
                center + new IntVec3(-distance, 0, -distance),
                center + new IntVec3(distance, 0, -distance),
                center + new IntVec3(-distance, 0, distance),
                center + new IntVec3(distance, 0, distance),
                center + new IntVec3(-distance / 2, 0, -distance),
                center + new IntVec3(distance / 2, 0, -distance),
                center + new IntVec3(-distance / 2, 0, distance),
                center + new IntVec3(distance / 2, 0, distance)
            };
        }

        public static List<IntVec3> GetCornerCells(
            LayoutZone zone,
            int inset = 1
        )
        {
            inset = Math.Max(0, inset);
            CellRect area = zone.Area;

            return new List<IntVec3>
            {
                new IntVec3(
                    area.minX + inset,
                    0,
                    area.minZ + inset
                ),
                new IntVec3(
                    area.maxX - inset,
                    0,
                    area.minZ + inset
                ),
                new IntVec3(
                    area.minX + inset,
                    0,
                    area.maxZ - inset
                ),
                new IntVec3(
                    area.maxX - inset,
                    0,
                    area.maxZ - inset
                )
            };
        }

        private static bool TryPlaceBuildingAt(
            LayoutContext context,
            LayoutZone zone,
            ThingDef buildingDef,
            ThingDef stuffDef,
            Rot4 rotation,
            IntVec3 cell,
            out Thing placedThing
        )
        {
            placedThing = null;

            try
            {
                if (
                    !CanPlaceBuildingAt(
                        cell,
                        rotation,
                        buildingDef,
                        stuffDef,
                        context.Map,
                        zone
                    )
                )
                {
                    return false;
                }

                Thing building = ThingMaker.MakeThing(
                    buildingDef,
                    buildingDef.MadeFromStuff ? stuffDef : null
                );

                placedThing = GenSpawn.Spawn(
                    building,
                    cell,
                    context.Map,
                    rotation
                );
                return placedThing != null;
            }
            catch (Exception exception)
            {
                Log.Warning(
                    "[IEE] Skipped " +
                    buildingDef.defName +
                    " during enclave layout generation after a safe " +
                    "placement attempt failed: " +
                    exception.Message
                );
                return false;
            }
        }

        private static bool TryPlaceItemAt(
            LayoutContext context,
            LayoutZone zone,
            ThingDef itemDef,
            int stackCount,
            IntVec3 cell,
            out Thing placedThing
        )
        {
            placedThing = null;

            if (!CanPlaceSingleCellAt(cell, context.Map, zone))
            {
                return false;
            }

            try
            {
                Thing item = ThingMaker.MakeThing(itemDef);
                item.stackCount = Math.Min(
                    stackCount,
                    itemDef.stackLimit
                );
                placedThing = GenSpawn.Spawn(
                    item,
                    cell,
                    context.Map
                );
                return placedThing != null;
            }
            catch (Exception exception)
            {
                Log.Warning(
                    "[IEE] Skipped " +
                    itemDef.defName +
                    " during enclave layout generation after a safe " +
                    "placement attempt failed: " +
                    exception.Message
                );
                return false;
            }
        }

        private static bool CanPlaceBuildingAt(
            IntVec3 center,
            Rot4 rotation,
            ThingDef buildingDef,
            ThingDef stuffDef,
            Map map,
            LayoutZone zone
        )
        {
            CellRect occupied = GenAdj.OccupiedRect(
                center,
                rotation,
                buildingDef.Size
            );

            foreach (IntVec3 cell in occupied.Cells)
            {
                if (!CanPlaceSingleCellAt(cell, map, zone))
                {
                    return false;
                }
            }

            return GenConstruct.CanPlaceBlueprintAt(
                buildingDef,
                center,
                rotation,
                map,
                godMode: false,
                thingToIgnore: null,
                thing: null,
                stuffDef: stuffDef
            ).Accepted;
        }

        private static bool CanPlaceSingleCellAt(
            IntVec3 cell,
            Map map,
            LayoutZone zone
        )
        {
            return
                zone.Contains(cell) &&
                cell.InBounds(map) &&
                cell.Standable(map) &&
                cell.GetThingList(map).Count == 0;
        }

        private static IEnumerable<IntVec3> GetRadialCells(
            IntVec3 center,
            LayoutZone zone
        )
        {
            int maximumRadius = Math.Max(
                zone.Area.Width,
                zone.Area.Height
            );

            for (int radius = 0; radius <= maximumRadius; radius++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (
                            radius > 0 &&
                            Math.Abs(x) != radius &&
                            Math.Abs(z) != radius
                        )
                        {
                            continue;
                        }

                        IntVec3 cell =
                            center + new IntVec3(x, 0, z);

                        if (zone.Contains(cell))
                        {
                            yield return cell;
                        }
                    }
                }
            }
        }
    }
}
