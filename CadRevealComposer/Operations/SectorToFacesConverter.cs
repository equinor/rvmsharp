namespace CadRevealComposer.Operations
{
    using AlgebraExtensions;
    using Faces;
    using Primitives;
    using RvmSharp.Tessellation;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using Utils;
    using Writers;

    public static class SectorToFacesConverter
    {
        /// <summary>
        /// Maximum size for a face side in meters
        /// </summary>
        private const float MaxFaceSize = 2.5f;

        /// <summary>
        /// Minimum size for a face side in meters
        /// </summary>
        private const float MinFaceSize = 0.1f;

        public record ProtoGrid(GridParameters GridParameters, IReadOnlyDictionary<Vector3i, VisibleSide> Faces);

        private record struct ProtoFaceNode(ulong TreeIndex, ulong NodeId, Color Color, ProtoGrid Faces);

        private record struct Hit(Vector3i Cell, VisibleSide Direction, FaceHitLocation HitLocation);

        [Flags]
        public enum VisibleSide
        {
            None = 0,
            XPositive = 1 << 0,
            XNegative = 1 << 1,
            YPositive = 1 << 2,
            YNegative = 1 << 3,
            ZPositive = 1 << 4,
            ZNegative = 1 << 5
        }

        [Flags]
        private enum FaceHitLocation
        {
            None = 0,
            North = 1 << 0,
            NorthEast = 1 << 1,
            East = 1 << 2,
            SouthEast = 1 << 3,
            South = 1 << 4,
            SouthWest = 1 << 5,
            West = 1 << 6,
            NorthWest = 1 << 7,
            Center = 1 << 8,
        }

        private static readonly FaceHitLocation[] AllHitLocations =
            Enum.GetValues<FaceHitLocation>().Where(l => l != FaceHitLocation.None).ToArray();

        private static Ray GetAdjustedRay(Ray rayIn, FaceHitLocation location, GridParameters gridParameters, Axis direction)
        {
            var (north, east) = direction switch
            {
                Axis.X => (Vector3.UnitZ, -Vector3.UnitY),
                Axis.Y => (Vector3.UnitZ, Vector3.UnitX),
                Axis.Z => (-Vector3.UnitX, -Vector3.UnitY),
                _ => throw new ArgumentException()
            };
            var multiplier = gridParameters.GridIncrement / 3;
            return location switch
            {
                FaceHitLocation.North => rayIn with { Origin = rayIn.Origin + north * multiplier },
                FaceHitLocation.NorthEast => rayIn with { Origin = rayIn.Origin + (north + east) * multiplier },
                FaceHitLocation.East => rayIn with { Origin = rayIn.Origin + east * multiplier },
                FaceHitLocation.SouthEast => rayIn with { Origin = rayIn.Origin + (-north + east) * multiplier },
                FaceHitLocation.South => rayIn with { Origin = rayIn.Origin - north * multiplier },
                FaceHitLocation.SouthWest => rayIn with { Origin = rayIn.Origin - (north + east) * multiplier },
                FaceHitLocation.West => rayIn with { Origin = rayIn.Origin - east * multiplier },
                FaceHitLocation.NorthWest => rayIn with { Origin = rayIn.Origin + (north - east) * multiplier },
                FaceHitLocation.Center => rayIn,
                _ => throw new ArgumentOutOfRangeException(nameof(location), location, null)
            };
        }

        private static IEnumerable<Hit> EnumerateHits(IEnumerable<Mesh> meshes, GridParameters gridParameters)
        {
            foreach (var mesh in meshes)
            {
                for (var i = 0; i < mesh.Triangles.Count; i += 3)
                {
                    var v1 = mesh.Vertices[mesh.Triangles[i]];
                    var v2 = mesh.Vertices[mesh.Triangles[i + 1]];
                    var v3 = mesh.Vertices[mesh.Triangles[i + 2]];

                    var min = Vector3.Min(v1, Vector3.Min(v2, v3));
                    var max = Vector3.Max(v1, Vector3.Max(v2, v3));
                    var (start, end) = GetGridCellsForBounds(min, max, gridParameters);

                    // X cast
                    for (var y = start.Y; y <= end.Y; y++)
                    {
                        for (var z = start.Z; z <= end.Z; z++)
                        {
                            const Axis axis = Axis.X;
                            var xRay = GetRayForGridCellAndDirection(new Vector3i(0, y, z), gridParameters, axis);

                            foreach (var hitLocation in AllHitLocations)
                            {
                                var adjustedRay = GetAdjustedRay(xRay, hitLocation, gridParameters, axis);
                                var hitResult = adjustedRay.Trace(v1, v2, v3, out var hitPosition, out var frontFace);
                                if (!hitResult)
                                {
                                    continue;
                                }

                                (var cell, VisibleSide direction) = HitResultToFaceIn(hitPosition, frontFace, gridParameters, axis);

                                yield return new Hit(cell, direction, hitLocation);
                            }
                        }
                    }

                    // Y cast
                    for (var x = start.X; x <= end.X; x++)
                    {
                        for (var z = start.Z; z <= end.Z; z++)
                        {
                            const Axis axis = Axis.Y;
                            var yRay = GetRayForGridCellAndDirection(new Vector3i(x, 0, z), gridParameters, axis);
                            foreach (var hitLocation in AllHitLocations)
                            {
                                var adjustedRay = GetAdjustedRay(yRay, hitLocation, gridParameters, axis);
                                var hitResult = adjustedRay.Trace(v1, v2, v3, out var hitPosition, out var frontFace);
                                if (!hitResult)
                                {
                                    continue;
                                }

                                (var cell, VisibleSide direction) = HitResultToFaceIn(hitPosition, frontFace, gridParameters, axis);

                                yield return new Hit(cell, direction, hitLocation);
                            }
                        }
                    }

                    // z cast
                    for (var x = start.X; x <= end.X; x++)
                    {
                        for (var y = start.Y; y <= end.Y; y++)
                        {
                            const Axis axis = Axis.Z;
                            var zRay = GetRayForGridCellAndDirection(new Vector3i(x, y, 0), gridParameters, axis);
                            foreach (var hitLocation in AllHitLocations)
                            {
                                var adjustedRay = GetAdjustedRay(zRay, hitLocation, gridParameters, axis);
                                var hitResult = adjustedRay.Trace(v1, v2, v3, out var hitPosition, out var frontFace);
                                if (!hitResult)
                                {
                                    continue;
                                }

                                (var cell, VisibleSide direction) = HitResultToFaceIn(hitPosition, frontFace, gridParameters, axis);

                                yield return new Hit(cell, direction, hitLocation);
                            }
                        }
                    }
                }
            }
        }

        public static ProtoGrid Convert(IEnumerable<Mesh> meshes, GridParameters gridParameters)
        {
            var faces = new Dictionary<Vector3i, Dictionary<VisibleSide, FaceHitLocation>>();

            foreach (var hit in EnumerateHits(meshes, gridParameters))
            {
                if (!faces.TryGetValue(hit.Cell, out var face))
                {
                    face = new Dictionary<VisibleSide, FaceHitLocation>();
                    faces[hit.Cell] = face;
                }

                if (!face.TryGetValue(hit.Direction, out var oldHitGrade))
                {
                    oldHitGrade = FaceHitLocation.None;
                }

                face[hit.Direction] = oldHitGrade | hit.HitLocation;
            }

            var newFaces = faces.Select(kvp =>
                {
                    var currentFace = kvp.Value;
                    var result = currentFace.Select(kvp =>
                    {
                        var g = kvp.Value;
                        var weight = BitOperations.PopCount((uint)g) + (g.HasFlag(FaceHitLocation.Center) ? 1 : 0);
                        return weight >= 4 ? kvp.Key : VisibleSide.None;
                    }).Where(v => v != VisibleSide.None).Aggregate(VisibleSide.None, (a, b) => a | b);
                    return (kvp.Key, result);
                }).Where(kvp => kvp.result != VisibleSide.None)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.result);
            return new ProtoGrid(gridParameters, newFaces);
        }

        private static IEnumerable<Hit> CollectHits(GridParameters gridParameters, Ray ray, Axis axis, Vector3 v1, Vector3 v2, Vector3 v3)
        {
            foreach (var hitLocation in AllHitLocations)
            {
                var adjustedRay = GetAdjustedRay(ray, hitLocation, gridParameters, axis);
                var hitResult = adjustedRay.Trace(v1, v2, v3, out var hitPosition, out var frontFace);
                if (!hitResult)
                {
                    continue;
                }

                (var cell, VisibleSide direction) = HitResultToFaceIn(hitPosition, frontFace, gridParameters, axis);

                yield return new Hit(cell, direction, hitLocation);
            }
        }

        private static (Vector3i cell, VisibleSide direction) HitResultToFaceIn(Vector3 hitPosition, bool isFrontFace, GridParameters grid, Axis axis)
        {
            var cell = PositionToGridCell(hitPosition, grid);
            var center = grid.GridOrigin + (cell + Vector3i.One) * grid.GridIncrement;
            var isHigh = axis switch
            {
                Axis.X => center.X < hitPosition.X,
                Axis.Y => center.Y < hitPosition.Y,
                Axis.Z => center.Z < hitPosition.Z,
                _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, null)
            };
            var direction = (isFrontFace, axis) switch
            {
                (false, Axis.X) => VisibleSide.XPositive,
                (false, Axis.Y) => VisibleSide.YPositive,
                (false, Axis.Z) => VisibleSide.ZPositive,
                (true, Axis.X) => VisibleSide.XNegative,
                (true, Axis.Y) => VisibleSide.YNegative,
                (true, Axis.Z) => VisibleSide.ZNegative,
                _ => throw new ArgumentOutOfRangeException()
            };

            var axisIndex = (int)axis;
            if (isFrontFace & isHigh)
            {
                cell[axisIndex]++;
            }
            else if (!isFrontFace & !isHigh)
            {
                cell[axisIndex]--;
            }

            return (cell, direction);
        }

        private static Ray GetRayForGridCellAndDirection(Vector3i target, GridParameters grid, Axis axis)
        {
            var newTarget = grid.GridOrigin + (Vector3.One + new Vector3(target.X, target.Y, target.Z)) * grid.GridIncrement;
            var newOrigin = grid.GridOrigin;
            return axis switch
            {
                Axis.X => new Ray(new Vector3(newOrigin.X, newTarget.Y, newTarget.Z), Vector3.UnitX),
                Axis.Y => new Ray(new Vector3(newTarget.X, newOrigin.Y, newTarget.Z), Vector3.UnitY),
                Axis.Z => new Ray(new Vector3(newTarget.X, newTarget.Y, newOrigin.Z), Vector3.UnitZ),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static (Vector3i start, Vector3i end) GetGridCellsForBounds(Vector3 min, Vector3 max, GridParameters gridParameters)
        {
            var start = PositionToGridCell(min, gridParameters);
            var end = PositionToGridCell(max, gridParameters);
            return (start, end);
        }

        private static Vector3i PositionToGridCell(Vector3 position, GridParameters grid)
        {
            var startF = (position - (grid.GridOrigin + Vector3.One * grid.GridIncrement / 2)) /
                         grid.GridIncrement;
            return new Vector3i((int)MathF.Floor(startF.X), (int)MathF.Floor(startF.Y), (int)MathF.Floor(startF.Z));
        }

        public static SectorFaces ConvertSector(SectorSplitter.ProtoSector protoSector, string outputDirectoryFullName)
        {
            var grid = CalculateGridParameters(protoSector);
            var protoNodes = protoSector.Geometries
                .GroupBy(g => g.TreeIndex)
                .Chunk(100)
                .AsParallel()
                .SelectMany(groups => Test(groups, grid))
                .ToArray();
            return ExportFaceSector(protoSector, protoNodes, grid, Path.Combine(outputDirectoryFullName, $"sector_{protoSector.SectorId}.f3d"));
        }

        private static IEnumerable<ProtoFaceNode> Test(IGrouping<ulong, APrimitive>[] groups, GridParameters grid)
        {
            foreach (var group in groups)
            {
                yield return ConvertNode(group, grid);
            }
        }

        private static ProtoFaceNode ConvertNode(IGrouping<ulong, APrimitive> group, GridParameters grid)
        {
            var geometries = group.AsEnumerable();
            var treeIndex = group.Key;
            var nodeId = geometries.First().NodeId;
            var meshesWithColors = geometries
                .Select(g => (Mesh: TessellatorBridge.Tessellate(g.SourcePrimitive, 0.01f), g.Color))
                .ToArray();
            var singleColor = meshesWithColors.Select(mc => mc.Color).Distinct().Count() == 1;
            if (!singleColor)
            {
                throw new NotImplementedException("Multi color support per node is not yet implemented");
            }

            var color = meshesWithColors
                .Select(mc => mc.Color)
                .First();
            var meshes = meshesWithColors
                .Select(mc => mc.Mesh)
                .WhereNotNull();
            var protoGrid = Convert(meshes, grid);
            return new ProtoFaceNode(treeIndex, nodeId, color, protoGrid);
        }

        private static GridParameters CalculateGridParameters(SectorSplitter.ProtoSector protoSector)
        {
            var bounds = new Bounds(protoSector.BoundingBoxMin, protoSector.BoundingBoxMax);
            var size = bounds.Size;
            var minDim = MathF.Min(size.X, MathF.Min(size.Y, size.Z));
            var increment = MathF.Min(MathF.Max(minDim / 50, MinFaceSize), MaxFaceSize);
            var gridSizeX = (uint)(Math.Ceiling(size.X / increment) + 1);
            var gridSizeY = (uint)(Math.Ceiling(size.Y / increment) + 1);
            var gridSizeZ = (uint)(Math.Ceiling(size.Z / increment) + 1);
            var gridOrigin = bounds.Min - Vector3.One * increment / 2;
            return new GridParameters(gridSizeX, gridSizeY, gridSizeZ, gridOrigin, increment);
        }

        private static SectorFaces ExportFaceSector(SectorSplitter.ProtoSector protoSector, ProtoFaceNode[] protoNodes, GridParameters grid, string outputFilename)
        {
            // TODO: Calculate coverage factors from input
            // TODO: Implement compression

            var nodes = protoNodes.Select(pn =>
                new Node(CompressFlags.IndexIsLong, pn.NodeId, pn.TreeIndex, pn.Color,
                    pn.Faces.Faces.Select(f =>
                        new Face(ConvertVisibleSidesToFaceFlags(f.Value), 0, GridCellToGridIndex(f.Key, grid),
                            null)).ToArray())
            ).ToArray();
            var sector = new SectorFaces(
                protoSector.SectorId,
                protoSector.ParentSectorId,
                protoSector.BoundingBoxMin,
                protoSector.BoundingBoxMax,
                new FacesGrid(grid, nodes),
                new CoverageFactors { Xy = 0.1f, Yz = 0.1f, Xz = 0.1f });

            using var output = File.OpenWrite(outputFilename);
            F3dWriter.WriteSector(sector, output);
            return sector;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static FaceFlags ConvertVisibleSidesToFaceFlags(VisibleSide direction)
        {
            if (direction == VisibleSide.None)
                throw new ArgumentException("Must contain at least one face");
            var result = FaceFlags.None;
            if (direction.HasFlag(VisibleSide.XPositive)) result |= FaceFlags.PositiveXVisible;
            if (direction.HasFlag(VisibleSide.YPositive)) result |= FaceFlags.PositiveYVisible;
            if (direction.HasFlag(VisibleSide.ZPositive)) result |= FaceFlags.PositiveZVisible;
            if (direction.HasFlag(VisibleSide.XNegative)) result |= FaceFlags.NegativeXVisible;
            if (direction.HasFlag(VisibleSide.YNegative)) result |= FaceFlags.NegativeYVisible;
            if (direction.HasFlag(VisibleSide.ZNegative)) result |= FaceFlags.NegativeZVisible;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong GridCellToGridIndex(Vector3i v, GridParameters gridParameters)
        {
            return (ulong)(v.X + (gridParameters.GridSizeX - 1) * v.Y + (gridParameters.GridSizeX - 1) * (gridParameters.GridSizeY - 1) * v.Z);
        }
    }
}