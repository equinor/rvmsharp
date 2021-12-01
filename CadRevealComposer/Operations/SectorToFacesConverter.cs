namespace CadRevealComposer.Operations
{
    using AlgebraExtensions;
    using Faces;
    using RvmSharp.Tessellation;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Numerics;
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

        public record ProtoFaceNode(ulong TreeIndex, ulong NodeId, Color Color, ProtoGrid Faces);

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
        public enum FaceHitLocation
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

        public static Triangle[] CollectTriangles(Mesh mesh)
        {
            var triangleCount = mesh.Triangles.Count / 3;
            var result = new Triangle[triangleCount];
            for (var i = 0; i < triangleCount; i++)
            {
                var v1 = mesh.Vertices[mesh.Triangles[i * 3]];
                var v2 = mesh.Vertices[mesh.Triangles[i * 3 + 1]];
                var v3 = mesh.Vertices[mesh.Triangles[i * 3 + 2]];
                result[i] = new Triangle(v1, v2, v3);
            }

            return result;
        }

        public static ProtoGrid Convert(Triangle[] triangles, GridParameters gridParameters)
        {
            var triangleCount = triangles.Length;
            var faces = new Dictionary<Vector3i, Dictionary<VisibleSide, FaceHitLocation>>();
            for (var i = 0; i < triangleCount; i++)
            {

                var triangle = triangles[i];
                var bounds = triangle.Bounds;
                var (start, end) = GetGridCellsForBounds(bounds, gridParameters);

                // X cast
                for (var y = start.Y; y <= end.Y; y++)
                {
                    for (var z = start.Z; z <= end.Z; z++)
                    {
                        const Axis axis = Axis.X;
                        var xRay = GetRayForGridCellAndDirection(new Vector3i(0, y, z), gridParameters, axis);
                        CollectHits(gridParameters, xRay, axis, triangle, faces);
                    }
                }

                // Y cast
                for (var x = start.X; x <= end.X; x++)
                {
                    for (var z = start.Z; z <= end.Z; z++)
                    {
                        const Axis axis = Axis.Y;
                        var yRay = GetRayForGridCellAndDirection(new Vector3i(x, 0, z), gridParameters, axis);
                        CollectHits(gridParameters, yRay, axis, triangle, faces);
                    }
                }

                // z cast
                for (var x = start.X; x <= end.X; x++)
                {
                    for (var y = start.Y; y <= end.Y; y++)
                    {
                        const Axis axis = Axis.Z;
                        var zRay = GetRayForGridCellAndDirection(new Vector3i(x, y, 0), gridParameters, axis);
                        CollectHits(gridParameters, zRay, axis, triangle, faces);
                    }
                }
            }

            var newFaces = faces.Select(kvp =>
                {
                    var currentFace = kvp.Value;
                    var result = currentFace.Select(kvp =>
                    {
                        var g = kvp.Value;
                        if (((g.HasFlag(FaceHitLocation.Center) ? 2 : 0)
                             + (g.HasFlag(FaceHitLocation.North) ? 1 : 0)
                             + (g.HasFlag(FaceHitLocation.NorthEast) ? 1 : 0)
                             + (g.HasFlag(FaceHitLocation.East) ? 1 : 0)
                             + (g.HasFlag(FaceHitLocation.SouthEast) ? 1 : 0)
                             + (g.HasFlag(FaceHitLocation.South) ? 1 : 0)
                             + (g.HasFlag(FaceHitLocation.SouthWest) ? 1 : 0)
                             + (g.HasFlag(FaceHitLocation.West) ? 1 : 0)
                             + (g.HasFlag(FaceHitLocation.NorthWest) ? 1 : 0)) >= 4)
                            return kvp.Key;
                        return VisibleSide.None;
                    }).Where(v => v != VisibleSide.None).Aggregate(VisibleSide.None, (a, b) => a | b);
                    return (kvp.Key, result);
                }).Where(kvp => kvp.result != VisibleSide.None)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.result);
            return new ProtoGrid(gridParameters, newFaces);
        }

        private static void CollectHits(GridParameters gridParameters, Ray ray, Axis axis, Triangle triangle,
            IDictionary<Vector3i, Dictionary<VisibleSide, FaceHitLocation>> faces)
        {
            for (var k = 0; k < 9; k++)
            {
                var hitLocation = (FaceHitLocation)(1 << k);
                var adjustedRay = GetAdjustedRay(ray, hitLocation, gridParameters, axis);
                var hitResult = adjustedRay.Trace(triangle, out var hitPosition,
                    out var frontFace);
                if (!hitResult)
                {
                    continue;
                }

                (var cell, VisibleSide direction) = HitResultToFaceIn(hitPosition, frontFace,
                    gridParameters, axis);
                if (!faces.TryGetValue(cell, out var face))
                {
                    face = new Dictionary<VisibleSide, FaceHitLocation>();
                    faces[cell] = face;
                }

                if (!face.TryGetValue(direction, out var oldHitGrade))
                {
                    oldHitGrade = FaceHitLocation.None;
                }

                face[direction] = oldHitGrade | hitLocation;
            }
        }

        private static (Vector3i cell, VisibleSide direction) HitResultToFaceIn(Vector3 hitPosition, bool isFrontFace, GridParameters grid, Axis axis)
        {
            var cell = PositionToCell(hitPosition, grid);
            var center = grid.GridOrigin + (cell + Vector3i.One) * grid.GridIncrement;
            var isHigh = new[]{center.X < hitPosition.X,center.Y < hitPosition.Y,center.Z < hitPosition.Z};
            var lowFaces = new[] { VisibleSide.XNegative, VisibleSide.YNegative, VisibleSide.ZNegative };
            var highFaces = new[] { VisibleSide.XPositive, VisibleSide.YPositive, VisibleSide.ZPositive };
            var axisIndex = (int)axis;
            var direction = isFrontFace ? lowFaces[axisIndex] : highFaces[axisIndex];


            if (isFrontFace & isHigh[axisIndex])
            {
                cell[axisIndex]++;
            } else if (!isFrontFace & !isHigh[axisIndex])
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

        private static (Vector3i start, Vector3i end) GetGridCellsForBounds(Bounds bounds, GridParameters gridParameters)
        {
            var start = PositionToCell(bounds.Min, gridParameters);
            var end = PositionToCell(bounds.Max, gridParameters);
            return (start, end);
        }

        private static Vector3i PositionToCell(Vector3 position, GridParameters grid)
        {
            var startF = (position - (grid.GridOrigin + Vector3.One * grid.GridIncrement / 2)) /
                         grid.GridIncrement;
            return new Vector3i((int)MathF.Floor(startF.X), (int)MathF.Floor(startF.Y), (int)MathF.Floor(startF.Z));
        }

        public static SectorFaces ConvertSector(SectorSplitter.ProtoSector protoSector, string outputDirectoryFullName)
        {
            var groupedGeometry = protoSector.Geometries.GroupBy(g => g.TreeIndex);
            var protoNodes = new List<ProtoFaceNode>();
            var bounds = new Bounds(protoSector.BoundingBoxMin, protoSector.BoundingBoxMax);
            var size = bounds.Size;
            var minDim = MathF.Min(size.X, MathF.Min(size.Y, size.Z));
            var increment = MathF.Min(MathF.Max(minDim / 50, MinFaceSize), MaxFaceSize);
            var gridSizeX = (uint)(Math.Ceiling(size.X / increment) + 1);
            var gridSizeY = (uint)(Math.Ceiling(size.Y / increment) + 1);
            var gridSizeZ = (uint)(Math.Ceiling(size.Z / increment) + 1);
            var gridOrigin = bounds.Min - Vector3.One * increment / 2;
            var grid = new GridParameters(gridSizeX, gridSizeY, gridSizeZ, gridOrigin, increment);
            foreach (var group in groupedGeometry)
            {
                var geometries = group.ToArray();
                var treeIndex = group.Key;
                var nodeId = geometries.First().NodeId;
                var meshesWithColors = geometries.Select(g => (g.SourcePrimitive, g.Color)).Select(mc => (TessellatorBridge.Tessellate(mc.SourcePrimitive, 0.01f), mc.Color)).ToArray();
                var singleColor = meshesWithColors.Select(mc => mc.Color).Distinct().Count() == 1;
                // TODO: support for multiple colors
                if (!singleColor)
                {
                    throw new NotImplementedException("Multi color support per node is not yet implemented");
                }
                var color = meshesWithColors.Select(mc => mc.Color).First();
                var triangles = meshesWithColors.Select(mc => mc.Item1).WhereNotNull().SelectMany(SectorToFacesConverter.CollectTriangles).ToArray();
                var protoGrid = SectorToFacesConverter.Convert(triangles, grid);
                protoNodes.Add(new ProtoFaceNode(treeIndex, nodeId, color, protoGrid));
            }

            return ExportFaceSector(protoSector, protoNodes, grid, outputDirectoryFullName + $"/sector_{protoSector.SectorId}.f3d");
        }

        private static SectorFaces ExportFaceSector(SectorSplitter.ProtoSector protoSector, List<ProtoFaceNode> protoNodes, GridParameters grid, string outputFilename)
        {
            var sector = new SectorFaces(protoSector.SectorId, protoSector.ParentSectorId, protoSector.BoundingBoxMin,
                protoSector.BoundingBoxMax,

                new FacesGrid(
                    grid,
                    protoNodes.Select(pn =>
                        new Node(CompressFlags.IndexIsLong, pn.NodeId, pn.TreeIndex, pn.Color,
                            pn.Faces.Faces.Select(f =>
                                new Face(ConvertFaceFlags(f.Value), 0, CellPositionToGridIndex(f.Key, grid),
                                    null)).ToArray())
                    ).ToArray()));
            using var output = File.OpenWrite(outputFilename);
            F3dWriter.WriteSector(sector, output);
            return sector;
        }

        public static FaceFlags ConvertFaceFlags(VisibleSide direction)
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

        public static ulong CellPositionToGridIndex(Vector3i v, GridParameters gridParameters)
        {
            return (ulong)(v.X + (gridParameters.GridSizeX - 1) * v.Y + (gridParameters.GridSizeX - 1) * (gridParameters.GridSizeY - 1) * v.Z);
        }
    }
}