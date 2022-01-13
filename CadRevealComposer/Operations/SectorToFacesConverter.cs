namespace CadRevealComposer.Operations
{
    using AlgebraExtensions;
    using Faces;
    using Primitives;
    using RvmSharp.Tessellation;
    using System;
    using System.Buffers;
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

        private record ProtoFaceNode(ulong TreeIndex, ulong NodeId, Color Color, ProtoGrid Faces);

        private readonly record struct Hit(Vector3i Cell, VisibleSide Direction, FaceHitLocation HitLocation);

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
            Enum.GetValues<FaceHitLocation>()
                .Where(l => l != FaceHitLocation.None)
                .ToArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Ray GetAdjustedRay(in Ray rayIn, FaceHitLocation location, GridParameters gridParameters, Axis direction)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<Triangle> CollectTrianglesForMesh(Mesh mesh)
        {
            var triangleCount = mesh.Triangles.Count / 3;
            var vertices = mesh.Vertices;
            for (var i = 0; i < triangleCount; i++)
            {
                var v1 = vertices[mesh.Triangles[i * 3]];
                var v2 = vertices[mesh.Triangles[i * 3 + 1]];
                var v3 = vertices[mesh.Triangles[i * 3 + 2]];
                yield return new Triangle(v1, v2, v3);
            }
        }

        public static ProtoGrid Convert(IEnumerable<Triangle> triangles, GridParameters gridParameters)
        {
            var hits = new List<Hit>();

            foreach (var triangle in triangles)
            {
                var (start, end) = GetGridCellsForBounds(triangle.Bounds, gridParameters);

                // X cast
                for (var y = start.Y; y <= end.Y; y++)
                {
                    for (var z = start.Z; z <= end.Z; z++)
                    {
                        var xRay = GetRayForGridCellAndDirection(new Vector3(0, y, z), gridParameters, Axis.X);
                        hits.AddRange(CollectHits(gridParameters, xRay, Axis.X, triangle));
                    }
                }

                // Y cast
                for (var x = start.X; x <= end.X; x++)
                {
                    for (var z = start.Z; z <= end.Z; z++)
                    {
                        var yRay = GetRayForGridCellAndDirection(new Vector3(x, 0, z), gridParameters, Axis.Y);
                        hits.AddRange(CollectHits(gridParameters, yRay, Axis.Y, triangle));
                    }
                }

                // z cast
                for (var x = start.X; x <= end.X; x++)
                {
                    for (var y = start.Y; y <= end.Y; y++)
                    {
                        var zRay = GetRayForGridCellAndDirection(new Vector3(x, y, 0), gridParameters, Axis.Z);
                        hits.AddRange(CollectHits(gridParameters, zRay, Axis.Z, triangle));
                    }
                }
            }

            static (Vector3i Key, VisibleSide result) GetVisibleSide(KeyValuePair<Vector3i, Dictionary<VisibleSide, FaceHitLocation>> kvp)
            {
                var dictionary = kvp.Value;
                var result = dictionary.Select(kvp =>
                {
                    var g = kvp.Value;
                    var weight = BitOperations.PopCount((uint)g) + (g.HasFlag(FaceHitLocation.Center) ? 1 : 0);
                    return weight >= 4
                        ? kvp.Key
                        : VisibleSide.None;
                })
                    .Where(v => v != VisibleSide.None)
                    .Aggregate(VisibleSide.None, (a, b) => a | b);
                return (kvp.Key, result);
            }

            var faces = new Dictionary<Vector3i, Dictionary<VisibleSide, FaceHitLocation>>();
            foreach (var (voxelCoordinate, visibleSide, faceHitLocation) in hits)
            {
                if (!faces.TryGetValue(voxelCoordinate, out var face))
                {
                    face = new Dictionary<VisibleSide, FaceHitLocation>();
                    faces[voxelCoordinate] = face;
                }

                if (!face.TryGetValue(visibleSide, out var oldHitGrade))
                {
                    oldHitGrade = FaceHitLocation.None;
                }

                face[visibleSide] = oldHitGrade | faceHitLocation;
            }

            var resultFaces = faces
                .Select(GetVisibleSide)
                .Where(kvp => kvp.result != VisibleSide.None)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.result);

            return new ProtoGrid(gridParameters, resultFaces);
        }

        private static IEnumerable<Hit> CollectHits(GridParameters gridParameters, Ray ray, Axis axis, Triangle triangle)
        {
            foreach (var hitLocation in AllHitLocations)
            {
                var adjustedRay = GetAdjustedRay(ray, hitLocation, gridParameters, axis);
                var hitResult = adjustedRay.Trace(triangle, out var hitPosition, out var frontFace);
                if (!hitResult)
                {
                    continue;
                }

                var (cell, direction) = HitResultToFaceIn(hitPosition, frontFace, gridParameters, axis);

                yield return new Hit(cell, direction, hitLocation);
            }
        }

        private static (Vector3i cell, VisibleSide direction) HitResultToFaceIn(in Vector3 hitPosition, bool isFrontFace, GridParameters grid, Axis axis)
        {
            #pragma warning disable CS8524 // missing catch all case in switch

            var cell = PositionToGridCell(hitPosition, grid);
            var center = grid.GridOrigin + (cell + Vector3i.One) * grid.GridIncrement;
            var isHigh = axis switch
            {
                Axis.X => center.X < hitPosition.X,
                Axis.Y => center.Y < hitPosition.Y,
                Axis.Z => center.Z < hitPosition.Z,
            };
            var direction = isFrontFace
                ? axis switch
                {
                    Axis.X => VisibleSide.XNegative,
                    Axis.Y => VisibleSide.YNegative,
                    Axis.Z => VisibleSide.ZNegative
                }
                : axis switch
                {
                    Axis.X => VisibleSide.XPositive,
                    Axis.Y => VisibleSide.YPositive,
                    Axis.Z => VisibleSide.ZPositive
                };

            if (isFrontFace & isHigh)
            {
                switch (axis)
                {
                    case Axis.X:
                        cell = cell with { X = cell.X + 1 };
                        break;
                    case Axis.Y:
                        cell = cell with { Y = cell.Y + 1 };
                        break;
                    case Axis.Z:
                        cell = cell with { Z = cell.Z + 1 };
                        break;
                }
            }
            else if (!isFrontFace & !isHigh)
            {
                switch (axis)
                {
                    case Axis.X:
                        cell = cell with { X = cell.X - 1 };
                        break;
                    case Axis.Y:
                        cell = cell with { Y = cell.Y - 1 };
                        break;
                    case Axis.Z:
                        cell = cell with { Z = cell.Z - 1 };
                        break;
                }
            }

            return (cell, direction);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Ray GetRayForGridCellAndDirection(in Vector3 target, GridParameters grid, Axis axis)
        {
            var newTarget = grid.GridOrigin + (Vector3.One + target) * grid.GridIncrement;
            var origin = grid.GridOrigin;
            return axis switch
            {
                Axis.X => new Ray(new Vector3(origin.X, newTarget.Y, newTarget.Z), Vector3.UnitX),
                Axis.Y => new Ray(new Vector3(newTarget.X, origin.Y, newTarget.Z), Vector3.UnitY),
                Axis.Z => new Ray(new Vector3(newTarget.X, newTarget.Y, origin.Z), Vector3.UnitZ),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (Vector3i start, Vector3i end) GetGridCellsForBounds(in Bounds bounds, GridParameters gridParameters)
        {
            var start = PositionToGridCell(bounds.Min, gridParameters);
            var end = PositionToGridCell(bounds.Max, gridParameters);
            return (start, end);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3i PositionToGridCell(in Vector3 position, GridParameters grid)
        {
            var startF = (position - (grid.GridOrigin + Vector3.One * grid.GridIncrement / 2)) / grid.GridIncrement;
            return new Vector3i(
                (int)MathF.Floor(startF.X),
                (int)MathF.Floor(startF.Y),
                (int)MathF.Floor(startF.Z));
        }

        public static SectorFaces ConvertSector(SectorSplitter.ProtoSector protoSector, string outputDirectoryFullName)
        {
            var bounds = new Bounds(protoSector.BoundingBoxMin, protoSector.BoundingBoxMax);
            var size = bounds.Size;
            var minDim = MathF.Min(size.X, MathF.Min(size.Y, size.Z));
            var increment = MathF.Min(MathF.Max(minDim / 50, MinFaceSize), MaxFaceSize);
            var gridSizeX = (uint)(Math.Ceiling(size.X / increment) + 1);
            var gridSizeY = (uint)(Math.Ceiling(size.Y / increment) + 1);
            var gridSizeZ = (uint)(Math.Ceiling(size.Z / increment) + 1);
            var gridOrigin = bounds.Min - Vector3.One * increment / 2; // center of first voxel
            var grid = new GridParameters(gridSizeX, gridSizeY, gridSizeZ, gridOrigin, increment);

            var nodeGroupedGeometry = protoSector.Geometries
                .Where(g => g is InstancedMesh or TriangleMesh) // only process meshes, skip primitives
                .GroupBy(g => g.TreeIndex)
                .ToArray();

            var protoNodes = new ProtoFaceNode[nodeGroupedGeometry.Length];
            for (var i = 0; i < nodeGroupedGeometry.Length; i++)
            {
                var group = nodeGroupedGeometry[i];
                var singleColor = group
                    .DistinctBy(g => g.Color)
                    .Count() == 1;
                if (!singleColor)
                {
                    throw new NotImplementedException("Multi color support per node is not yet implemented");
                }

                var treeIndex = group.Key;
                var first = group.First();
                var nodeId = first.NodeId;
                var color = first.Color;

                var triangles = group
                    .Select(g => g is TriangleMesh triangleMesh
                        ? triangleMesh.TempTessellatedMesh
                        : TessellatorBridge.Tessellate(g.SourcePrimitive, 0.01f))
                    .WhereNotNull()
                    .SelectMany(CollectTrianglesForMesh);
                var protoGrid = Convert(triangles, grid);
                protoNodes[i] = new ProtoFaceNode(treeIndex, nodeId, color, protoGrid);
            }

            return ExportFaceSector(protoSector, protoNodes, grid, outputDirectoryFullName + $"/sector_{protoSector.SectorId}.f3d");
        }

        private static SectorFaces ExportFaceSector(SectorSplitter.ProtoSector protoSector, ProtoFaceNode[] protoNodes, GridParameters grid, string outputFilename)
        {
            // TODO: Calculate coverage factors from input
            // TODO: Implement compression

            var sectorContents = new FacesGrid(
                grid,
                protoNodes
                    .Select(pn =>
                        new Node(
                            CompressFlags.IndexIsLong,
                            pn.NodeId,
                            pn.TreeIndex,
                            pn.Color,
                            pn.Faces.Faces
                                .Select(f => new Face(ConvertVisibleSidesToFaceFlags(f.Value), 0, GridCellToGridIndex(f.Key, grid), null))
                                .ToArray())
                    ).ToArray());

            var sector = new SectorFaces(
                protoSector.SectorId,
                protoSector.ParentSectorId,
                protoSector.BoundingBoxMin,
                protoSector.BoundingBoxMax,
                sectorContents,
                new CoverageFactors { Xy = 0.1f, Yz = 0.1f, Xz = 0.1f });

            using var output = File.OpenWrite(outputFilename);
            F3dWriter.WriteSector(sector, output);
            return sector;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static FaceFlags ConvertVisibleSidesToFaceFlags(in VisibleSide direction)
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
        private static ulong GridCellToGridIndex(in Vector3i voxelCoordinate, GridParameters gridParameters)
        {
            return (ulong)(voxelCoordinate.X +
                           voxelCoordinate.Y * (gridParameters.GridSizeX - 1) +
                           voxelCoordinate.Z * (gridParameters.GridSizeX - 1) * (gridParameters.GridSizeY - 1));
        }
    }
}