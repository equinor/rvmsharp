namespace CadRevealComposer.Operations
{
    using AlgebraExtensions;
    using Faces;
    using Newtonsoft.Json;
    using RvmSharp.Tessellation;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using Utils;
    using Writers;

    public static class FacesConverter
    {

        public record ProtoGrid(GridParameters GridParameters, IReadOnlyDictionary<Vector3i, FaceDirection> Faces);

        public record ProtoFaceNode(ulong TreeIndex, ulong NodeId, Color Color, ProtoGrid Faces);

        [Flags]
        public enum FaceDirection
        {
            None = 0,
            Xp = 0b000001,
            Xm = 0b000010,
            Yp = 0b000100,
            Ym = 0b001000,
            Zp = 0b010000,
            Zm = 0b100000,
        }

        [Flags]
        public enum FaceHitGrade
        {
            None = 0,
            N = 0b00000000_00000001,
            NE = 0b00000000_00000010,
            E = 0b00000000_00000100,
            SE = 0b00000000_00001000,
            S = 0b00000000_00010000,
            SW = 0b00000000_00100000,
            W = 0b00000000_01000000,
            NW = 0b00000000_10000000,
            C = 0b00000001_00000000,
        }

        private static Raycasting.Ray GetAdjustedRay(Raycasting.Ray rayIn, FaceHitGrade grade, GridParameters gridParameters, Axis direction)
        {
            var (north, east) = direction switch
            {
                Axis.X => (Vector3.UnitZ, -Vector3.UnitY),
                Axis.Y => (Vector3.UnitZ, Vector3.UnitX),
                Axis.Z => (-Vector3.UnitX, -Vector3.UnitY),
                _ => throw new ArgumentException()
            };
            var multiplier = gridParameters.GridIncrement / 3;
            switch (grade)
            {
                case FaceHitGrade.N:
                    return rayIn with { Origin = rayIn.Origin + north * multiplier};
                case FaceHitGrade.NE:
                    return rayIn with { Origin = rayIn.Origin + (north + east) * multiplier};
                case FaceHitGrade.E:
                    return rayIn with { Origin = rayIn.Origin + east * multiplier};
                case FaceHitGrade.SE:
                    return rayIn with { Origin = rayIn.Origin + (-north + east) * multiplier};
                case FaceHitGrade.S:
                    return rayIn with { Origin = rayIn.Origin - north * multiplier};
                case FaceHitGrade.SW:
                    return rayIn with { Origin = rayIn.Origin - (north + east) * multiplier};
                case FaceHitGrade.W:
                    return rayIn with { Origin = rayIn.Origin - east * multiplier};
                case FaceHitGrade.NW:
                    return rayIn with { Origin = rayIn.Origin + (north - east) * multiplier};
                case FaceHitGrade.C:
                    return rayIn;
                default:
                    throw new ArgumentOutOfRangeException(nameof(grade), grade, null);
            }
        }

        public record HitSave(int triangle, Axis Axis, Vector3i Cell, FaceDirection Direction, FaceHitGrade Area, Vector3 HitPosition, bool Front);

        public static Raycasting.Triangle[] CollectTriangles(Mesh mesh)
        {
            var triangleCount = mesh.Triangles.Count / 3;
            var result = new Raycasting.Triangle[triangleCount];
            for (var i = 0; i < triangleCount; i++)
            {
                var v1 = mesh.Vertices[mesh.Triangles[i * 3]];
                var v2 = mesh.Vertices[mesh.Triangles[i * 3 + 1]];
                var v3 = mesh.Vertices[mesh.Triangles[i * 3 + 2]];
                result[i] = new Raycasting.Triangle(v1, v2, v3);
            }

            return result;
        }

#if DUMP_FACES
        private static List<HitSave> Hits = new();
        private static List<LabeledRay> Rays = new();
        private record LabeledRay(Axis Axis, Vector3i Cell, FaceHitGrade Grade, Raycasting.Ray Ray);
#endif


        public static ProtoGrid Convert(Raycasting.Triangle[] triangles, GridParameters gridParameters)
        {
            var triangleCount = triangles.Length;
            var faces = new Dictionary<Vector3i, Dictionary<FaceDirection, FaceHitGrade>>();
            var hitCount = 0;
            var triangleMap = new Dictionary<Vector3i, List<Raycasting.Triangle>>();
            for (var i = 0; i < triangleCount; i++)
            {

                var triangle = triangles[i];
                var bounds = GetBounds(triangle);
                var (start, end) = GetPotentialGridPositions(bounds, gridParameters);

                // X cast
                for (var y = start.Y; y <= end.Y; y++)
                {
                    for (var z = start.Z; z <= end.Z; z++)
                    {
                        var (xRay, _, _) = GetRay(new Vector3i(0, y, z), gridParameters);
                        var axis = Axis.X;
                        hitCount = LolPleaseRenameMe(gridParameters, xRay, axis,  0, y, z, triangle, hitCount, faces, i);
                    }
                }

                // Y cast
                for (var x = start.X; x <= end.X; x++)
                {
                    for (var z = start.Z; z <= end.Z; z++)
                    {
                        var (_, yRay, _) = GetRay(new Vector3i(x, 0, z), gridParameters);
                        var axis = Axis.Y;
                        hitCount = LolPleaseRenameMe(gridParameters, yRay, axis,  x, 0, z, triangle, hitCount, faces, i);
                    }
                }

                // z cast
                for (var x = start.X; x <= end.X; x++)
                {
                    for (var y = start.Y; y <= end.Y; y++)
                    {
                        var (_, _, zRay) = GetRay(new Vector3i(x, y, 0), gridParameters);
                        var axis = Axis.Z;
                        hitCount = LolPleaseRenameMe(gridParameters, zRay, axis,  x, y, 0, triangle, hitCount, faces, i);
                    }
                }
            }

            var newFaces = faces.Select(kvp =>
                {
                    var faces = kvp.Value;
                    var result = faces.Select(kvp =>
                    {
                        var g = kvp.Value;
                        if (((g.HasFlag(FaceHitGrade.C) ? 2 : 0)
                             + (g.HasFlag(FaceHitGrade.N) ? 1 : 0)
                             + (g.HasFlag(FaceHitGrade.NE) ? 1 : 0)
                             + (g.HasFlag(FaceHitGrade.E) ? 1 : 0)
                             + (g.HasFlag(FaceHitGrade.SE) ? 1 : 0)
                             + (g.HasFlag(FaceHitGrade.S) ? 1 : 0)
                             + (g.HasFlag(FaceHitGrade.SW) ? 1 : 0)
                             + (g.HasFlag(FaceHitGrade.W) ? 1 : 0)
                             + (g.HasFlag(FaceHitGrade.NW) ? 1 : 0)) >= 4)
                            return kvp.Key;
                        return FaceDirection.None;
                    }).Where(v => v != FaceDirection.None).Aggregate(FaceDirection.None, (a, b) => a | b);
                    return (kvp.Key, result);
                }).Where(kvp => kvp.result != FaceDirection.None)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.result);
#if DUMP_FACES
            File.WriteAllText("E://gush//projects//FacesFiles//Assets//StreamingAssets//protogrid.json", JsonConvert.SerializeObject(newFaces));
            File.WriteAllText("E://gush//projects//FacesFiles//Assets//StreamingAssets//hits.json", JsonConvert.SerializeObject(Hits));
            File.WriteAllText(@"E:\gush\projects\FacesFiles\Assets\StreamingAssets\gridParameters.json", JsonConvert.SerializeObject(gridParameters, Formatting.Indented));
            JsonUtils.JsonSerializeToFile(Rays, "E://gush//projects//FacesFiles//Assets//StreamingAssets//rays.json");
#endif
            return new ProtoGrid(gridParameters, newFaces);
        }

        private static int LolPleaseRenameMe(GridParameters gridParameters, Raycasting.Ray? ray, Axis axis, int x, int y, int z, Raycasting.Triangle? triangle,
            int hitCount, Dictionary<Vector3i, Dictionary<FaceDirection, FaceHitGrade>>? faces, int i)
        {
            for (var k = 0; k < 9; k++)
            {
                var hitGrade = (FaceHitGrade)(1 << k);
                var adjustedRay = GetAdjustedRay(ray, hitGrade, gridParameters, axis);
                var hitResult = Raycasting.Raycast(adjustedRay, triangle, out var hitPosition,
                    out var frontFace);
                if (hitResult)
                {
                    hitCount++;
                    (var cell, FaceDirection direction) = HitResultToFaceIn(hitPosition, frontFace,
                        gridParameters, axis);
                    if (!faces.TryGetValue(cell, out var face))
                    {
                        face = new Dictionary<FaceDirection, FaceHitGrade>();
                        faces[cell] = face;
                    }

                    if (!face.TryGetValue(direction, out var oldHitGrade))
                    {
                        oldHitGrade = FaceHitGrade.None;
                    }

                    face[direction] = oldHitGrade | hitGrade;
#if DUMP_FACES
                    Rays.Add(new LabeledRay(axis, new Vector3i(x, y, z), hitGrade, adjustedRay));
                    Hits.Add(new HitSave(i, axis, cell, direction, hitGrade, hitPosition, frontFace));
#endif
                }
            }

            return hitCount;
        }

        public enum Axis
        {
            X = 0,
            Y = 1,
            Z = 2
        }

        private static (Vector3i cell, FaceDirection direction) HitResultToFaceIn(Vector3 hitPosition, bool isFrontFace, GridParameters grid, Axis axis)
        {
            var cell = PositionToCell(hitPosition, grid);
            var center = grid.GridOrigin + (cell + Vector3i.One) * grid.GridIncrement;
            var isHigh = new[]{center.X < hitPosition.X,center.Y < hitPosition.Y,center.Z < hitPosition.Z};
            var lowFaces = new[] { FaceDirection.Xm, FaceDirection.Ym, FaceDirection.Zm };
            var highFaces = new[] { FaceDirection.Xp, FaceDirection.Yp, FaceDirection.Zp };
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


        private static (Raycasting.Ray xRay, Raycasting.Ray yRay, Raycasting.Ray zRay) GetRay(Vector3i target, GridParameters grid)
        {
            var newTarget = grid.GridOrigin + (Vector3.One + new Vector3(target.X, target.Y, target.Z)) * grid.GridIncrement;
            var newOrigin = grid.GridOrigin;
            var xRay = new Raycasting.Ray(new Vector3(newOrigin.X, newTarget.Y, newTarget.Z), Vector3.UnitX);
            var yRay = new Raycasting.Ray(new Vector3(newTarget.X, newOrigin.Y, newTarget.Z), Vector3.UnitY);
            var zRay = new Raycasting.Ray(new Vector3(newTarget.X, newTarget.Y, newOrigin.Z), Vector3.UnitZ);
            return (xRay, yRay, zRay);
        }

        private static (Vector3i start, Vector3i end) GetPotentialGridPositions(Raycasting.Bounds bounds, GridParameters gridParameters)
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

        private static Raycasting.Bounds GetBounds(Raycasting.Triangle triangle)
        {
            var min = Vector3.Min(triangle.V1, Vector3.Min(triangle.V2, triangle.V3));
            var max = Vector3.Max(triangle.V1, Vector3.Max(triangle.V2, triangle.V3));
            return new Raycasting.Bounds(min, max);
        }

        public static SectorFaces ConvertSector(SectorSplitter.ProtoSector protoSector, string outputDirectoryFullName)
        {
            var groupedGeometry = protoSector.Geometries.GroupBy(g => g.TreeIndex);
            var protoNodes = new List<ProtoFaceNode>();
            var bounds = new Raycasting.Bounds(protoSector.BoundingBox.Min, protoSector.BoundingBox.Max);
            var size = bounds.Size;
            var minDim = MathF.Min(size.X, MathF.Min(size.Y, size.Z));
            var increment = minDim / 50;
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
                var color = meshesWithColors.Select(mc => mc.Color).First();
                var triangles = meshesWithColors.Select(mc => mc.Item1).WhereNotNull().SelectMany(FacesConverter.CollectTriangles).ToArray();
                var protoGrid = FacesConverter.Convert(triangles, grid);
                protoNodes.Add(new ProtoFaceNode(treeIndex, nodeId, color, protoGrid));
            }

            return ExportFaceSector(protoSector, protoNodes, grid, outputDirectoryFullName + $"/sector_{protoSector.SectorId}.f3d");
        }

        private static SectorFaces ExportFaceSector(SectorSplitter.ProtoSector protoSector, List<ProtoFaceNode> protoNodes, GridParameters grid, string outputFilename)
        {
            var sector = new SectorFaces(protoSector.SectorId, protoSector.ParentSectorId, protoSector.BoundingBox.Min,
                protoSector.BoundingBox.Max,

                new FacesGrid(
                    grid,
                    protoNodes.Select(pn =>
                        new Node(CompressFlags.IndexIsLong, pn.NodeId, pn.TreeIndex, pn.Color,
                            pn.Faces.Faces.Select(f =>
                                new Face(ConvertFaceFlags(f.Value), 0, VectorToIndex(f.Key, grid),
                                    null)).ToArray())
                    ).ToArray()));
            using var output = File.OpenWrite(outputFilename);
            F3dWriter.WriteSector(sector, output);
            return sector;
        }

        public static FaceFlags ConvertFaceFlags(FacesConverter.FaceDirection direction)
        {
            if (direction == FacesConverter.FaceDirection.None)
                throw new ArgumentException("Must contain at least one face");
            var result = FaceFlags.None;
            if (direction.HasFlag(FacesConverter.FaceDirection.Xp)) result |= FaceFlags.PositiveXVisible;
            if (direction.HasFlag(FacesConverter.FaceDirection.Yp)) result |= FaceFlags.PositiveYVisible;
            if (direction.HasFlag(FacesConverter.FaceDirection.Zp)) result |= FaceFlags.PositiveZVisible;
            if (direction.HasFlag(FacesConverter.FaceDirection.Xm)) result |= FaceFlags.NegativeXVisible;
            if (direction.HasFlag(FacesConverter.FaceDirection.Ym)) result |= FaceFlags.NegativeYVisible;
            if (direction.HasFlag(FacesConverter.FaceDirection.Zm)) result |= FaceFlags.NegativeZVisible;
            return result;
        }

        public static ulong VectorToIndex(Vector3i v, GridParameters gridParameters)
        {
            return (ulong)(v.X + (gridParameters.GridSizeX - 1) * v.Y + (gridParameters.GridSizeX - 1) * (gridParameters.GridSizeY - 1) * v.Z);
        }
    }
}