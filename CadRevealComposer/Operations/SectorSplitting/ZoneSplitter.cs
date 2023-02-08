namespace CadRevealComposer.Operations.SectorSplitting;

using Primitives;
using Utils;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Versioning;

/// <summary>
/// Divides a model into zones for a better starting point for sector splitting.
/// This is needed for models spread over a larger area like Melk�ya and Tjeldbergodden.
/// </summary>
public static class ZoneSplitter
{
    public record Zone(APrimitive[] Primitives);

    private record RootZone(APrimitive[] Primitives) : Zone(Primitives);

    private record ZoneInternal(Cell[] Cells, int MinNodeCount);

    private record Cell(int X, int Y)
    {
        public List<Node> Nodes { get; } = new();

        // mutable flags for processing
        public bool IsQueued { get; set; }
        public bool IsProcessed { get; set; }
        public bool IsPlacedInZone { get; set; }
    }

    private record Node(
        ulong NodeId,
        APrimitive[] Primitives,
        BoundingBox BoundingBox)
    {
        public Vector3 Extents => BoundingBox.Extents;

        public List<Cell> Cells { get; } = new();

        /// <summary>
        /// Checks if the node is enclosed by the given zone.
        /// </summary>
        public bool IsContainedWithin(IReadOnlySet<Cell> zoneCellSet)
        {
            if (Cells.Count > zoneCellSet.Count)
            {
                return false;
            }

            foreach (var cell in Cells)
            {
                if (zoneCellSet.Contains(cell) is false)
                {
                    return false;
                }
            }

            return true;
        }
    }

    private class Grid
    {
        private readonly Cell?[,] _matrix;

        private Grid(Cell?[,] matrix, ImmutableArray<Cell> cells)
        {
            _matrix = matrix;
            Cells = cells;
            GridSizeX = (uint)matrix.GetLength(0);
            GridSizeY = (uint)matrix.GetLength(1);
        }

        public ImmutableArray<Cell> Cells { get; }
        public uint GridSizeX { get; }
        public uint GridSizeY { get; }

        public static Grid Create(APrimitive[] primitives)
        {
            // 10x10m cell size chosen by experiments
            const float tenMeterCellSize = 10f; // assumption: data is in meters

            // group primitives into nodes
            var nodes = primitives
                .GroupBy(p => p.TreeIndex)
                .Select(g =>
                {
                    var geometries = g.ToArray();
                    var geometryBounds = geometries.CalculateBoundingBox();
                    return new Node(
                        g.Key,
                        geometries,
                        geometryBounds);
                })
                .ToArray();

            // calculate grid size
            var boundingBox = primitives.CalculateBoundingBox();
            var extents = boundingBox.Extents;
            var gridSizeX = (uint)MathF.Ceiling(extents.X / tenMeterCellSize);
            var gridSizeY = (uint)MathF.Ceiling(extents.Y / tenMeterCellSize);

            // place all nodes in matrix
            // NOTE: a node will be placed in all the cells it spans using its bounding box
            var matrix = new Cell?[gridSizeX, gridSizeY];
            foreach (var node in nodes)
            {
                var pBbMin = node.BoundingBox.Min - boundingBox.Min;
                var cellStartX = (int)MathF.Floor(pBbMin.X / tenMeterCellSize);
                var cellStartY = (int)MathF.Floor(pBbMin.Y / tenMeterCellSize);

                var cellCountX = (int)MathF.Ceiling(node.Extents.X / tenMeterCellSize);
                var cellCountY = (int)MathF.Ceiling(node.Extents.Y / tenMeterCellSize);

                var isLargeNode = cellCountX * cellCountY > 100;
                if (!isLargeNode)
                {
                    for (var x = cellStartX; x < cellStartX + cellCountX; x++)
                    for (var y = cellStartY; y < cellStartY + cellCountY; y++)
                    {
                        matrix[x, y] ??= new Cell(x, y);
                        var cell = matrix[x, y]!;
                        node.Cells.Add(cell);
                        cell.Nodes.Add(node);
                    }
                }
            }

            // build cell list to enumerate cells later on
            var cellsList = new List<Cell>();
            for (uint x = 0; x < gridSizeX; x++)
            for (uint y = 0; y < gridSizeY; y++)
            {
                if (matrix[x, y] is { } cell)
                {
                    cellsList.Add(cell);
                }
            }

            return new Grid(matrix, cellsList.ToImmutableArray());
        }

        public Cell? this[int x, int y]
        {
            get => _matrix[x, y];
        }

        public void ResetCellFlags()
        {
            foreach (var cell in Cells.Where(c => !c.IsPlacedInZone))
            {
                cell.IsProcessed = false;
                cell.IsQueued = false;
            }
        }
    }

    public static Zone[] SplitIntoZones(APrimitive[] primitives, DirectoryInfo outputDirectory)
    {
        var grid = Grid.Create(primitives);

        // group cells into zones
        // NOTE: start grouping cells with a higher node count, and go lower
        var zonesInternal = new List<ZoneInternal>();
        var minNodeCountInCellList = new[] { 10, 7, 3, 1 };
        foreach (var minNodeCountInCell in minNodeCountInCellList)
        {
            while (grid.Cells.Any(cell => cell.IsProcessed is false))
            {
                // select seed cell which has the highest node count
                var seedCell = grid.Cells
                    .Where(cell => cell.IsProcessed is false)
                    .MaxBy(cell => cell.Nodes.Count);

                var zone = GetZoneUsingSeedCell(seedCell!, grid, minNodeCountInCell);
                if (zone.Cells.Any())
                {
                    zonesInternal.Add(zone);
                }
            }

            // reset cell flags, ready for next iteration
            grid.ResetCellFlags();
        }

        // write zone visualization (PNG grid image)
        if (OperatingSystem.IsWindows())
        {
            var path = Path.Combine(outputDirectory.FullName, "Zones.png");
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            using var stream = File.OpenWrite(path);
            WriteZoneBitmap(stream, zonesInternal, grid.GridSizeX, grid.GridSizeY);
        }

        static Zone ConvertToZone(ZoneInternal zone)
        {
            // A node may be placed into multiple cells, hence the use of Distinct().
            // Let's filter out the nodes which extends beyond the zone.
            var zoneCellSet = zone.Cells.ToHashSet();
            var nodesContainedWithinZone = zone.Cells
                .SelectMany(c => c.Nodes)
                .Where(n => n.IsContainedWithin(zoneCellSet))
                .Distinct();
            var primitives = nodesContainedWithinZone
                .SelectMany(n => n.Primitives)
                .ToArray();
            return new Zone(primitives);
        }

        var zones = zonesInternal
            .Select(ConvertToZone)
            .ToList();

        // root sector
        var primitivesWithoutZone = primitives
            .Except(zones.SelectMany(z => z.Primitives))
            .ToArray();
        var nodesWithoutZone = primitivesWithoutZone
            .GroupBy(p => p.TreeIndex)
            .ToArray();

        Console.WriteLine($"Root sector has {nodesWithoutZone.Length:N0} nodes and {primitivesWithoutZone.Length:N0} geometries.");
        var rootZone = new RootZone(primitivesWithoutZone);
        zones.Add(rootZone);

        return zones.ToArray();
    }

    /// <summary>
    /// Grows a zone using a seed cell.
    /// </summary>
    private static ZoneInternal GetZoneUsingSeedCell(Cell seedCell, Grid grid, int minNodeCountInCell)
    {
        var queue = new Queue<Cell>();
        queue.Enqueue(seedCell);

        var result = new List<Cell>();
        while (queue.Count > 0)
        {
            var currentCell = queue.Dequeue();

            // check if all cells 5x5 matches the rules
            // NOTE: the current cell is in the middle
            var isPartOfZone = true;
            for (var x = currentCell.X - 2; x <= currentCell.X + 2; x++)
            {
                for (var y = currentCell.Y - 2; y <= currentCell.Y + 2; y++)
                {
                    var isWithinGridBounds = x >= 0 &&
                                             y >= 0 &&
                                             x < grid.GridSizeX &&
                                             y < grid.GridSizeY;
                    var cellOk = isWithinGridBounds &&
                               grid[x, y] is { } cell &&
                               cell.Nodes.Count >= minNodeCountInCell;
                    if (!cellOk)
                    {
                        isPartOfZone = false;
                        break;
                    }
                }

                if (!isPartOfZone)
                {
                    break;
                }
            }

            currentCell.IsProcessed = true;

            if (isPartOfZone)
            {
                // place in zone (if not already)
                for (var x = currentCell.X - 2; x <= currentCell.X + 2; x++)
                {
                    for (var y = currentCell.Y - 2; y <= currentCell.Y + 2; y++)
                    {
                        if (grid[x, y] is { IsPlacedInZone: false } cell)
                        {
                            cell.IsPlacedInZone = true;
                            result.Add(cell);
                        }
                    }
                }

                // queue neighbors for processing
                for (var x = currentCell.X - 1; x <= currentCell.X + 1; x++)
                {
                    for (var y = currentCell.Y - 1; y <= currentCell.Y + 1; y++)
                    {
                        if (grid[x, y] is { IsProcessed: false, IsQueued: false } cell)
                        {
                            cell.IsQueued = true;
                            queue.Enqueue(cell);
                        }
                    }
                }
            }
        }

        return new ZoneInternal(result.ToArray(), minNodeCountInCell);
    }

    /// <summary>
    /// System.Drawing.Common only available for Windows. Only used for debugging. Implement cross platform if needed.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static void WriteZoneBitmap(Stream stream, List<ZoneInternal> zones, uint gridSizeX, uint gridSizeY)
    {
        var zoneColorList = new[]
        {
            Color.Red,
            Color.Blue,
            Color.Green,
            Color.Orchid,
            Color.DeepSkyBlue,
            Color.YellowGreen,
            Color.SandyBrown,
            Color.DeepPink,
            Color.Khaki,
            Color.Magenta,
            Color.LightSalmon,
            Color.Chartreuse,
            Color.Gold,
            Color.SaddleBrown,
            Color.Orange,
            Color.Indigo,
            Color.MediumSlateBlue,
            Color.Olive,
            Color.PaleGreen,
            Color.HotPink
        };

        var bitmap = new Bitmap((int)gridSizeX, (int)gridSizeY);

        // fill with black background
        using (var gfx = Graphics.FromImage(bitmap))
        using (var brush = new SolidBrush(Color.Black))
        {
            gfx.FillRectangle(brush, 0, 0, gridSizeX, gridSizeY);
        }

        // write zones
        var i = 0;
        foreach (var zone in zones)
        {
            var zoneColor = zoneColorList[i % zoneColorList.Length];
            foreach (var (x, y) in zone.Cells)
            {
                bitmap.SetPixel(x, y, zoneColor);
            }
            i++;
        }

        // save to disk
        bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
        bitmap.Save(stream, ImageFormat.Png);
    }
}