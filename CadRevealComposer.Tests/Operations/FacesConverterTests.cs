namespace CadRevealComposer.Tests.Operations
{
    using AlgebraExtensions;
    using CadRevealComposer.Operations;
    using CadRevealComposer.Utils;
    using Faces;
    using Newtonsoft.Json;
    using NUnit.Framework;
    using RvmSharp.BatchUtils;
    using RvmSharp.Containers;
    using RvmSharp.Exporters;
    using RvmSharp.Primitives;
    using RvmSharp.Tessellation;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using Writers;

    [TestFixture]
    public class FacesConverterTests
    {
        [Test]
        public void ConvertSimpleMesh()
        {
            // Collect RVM files
            var workload = Workload.CollectWorkload(new[] { @"d:\Models\hda\faces" });
            // Read RVM
            var store = Workload.ReadRvmData(workload);
            var rootNodes = store.RvmFiles.SelectMany(f => f.Model.Children);
            var meshes = rootNodes.SelectMany(GetMesh).ToArray()
                .Select(p => TessellatorBridge.Tessellate(p, 0.01f))
                .WhereNotNull().ToArray();

            var triangles = meshes.SelectMany(FacesConverter.CollectTriangles).ToArray();
            var center = new Vector3(15, 15, 13);
            //var mesh = TessellatorBridge.Tessellate(new RvmBox(1, matrix, rvmBounds, 1f, 1f, 1f), 0.1f);
            //var mesh = TessellatorBridge.Tessellate(new RvmCylinder(1, matrix, rvmBounds, 0.5f, 0.7f), 0.01f);
            //var mesh = TessellatorBridge.Tessellate(new RvmSphere(
//                1, matrix, new RvmBoundingBox(-Vector3.One * 0.5f, Vector3.One * 0.5f), 1f), 0.01f);

            var min = triangles.SelectMany(t => new []{ t.V1, t.V2, t.V3 }).Aggregate(Vector3.Min);
            var max = triangles.SelectMany(t => new []{ t.V1, t.V2, t.V3 }).Aggregate(Vector3.Max);
            var size = max - min;
            var bounds = new Raycasting.Bounds(min, max);
            var minDim = MathF.Min(size.X, MathF.Min(size.Y, size.Z));
            var increment = minDim / 50;
            Console.WriteLine(increment);
            var gridSizeX = (uint)(Math.Ceiling(size.X / increment) + 1);
            var gridSizeY = (uint)(Math.Ceiling(size.Y / increment) + 1);
            var gridSizeZ = (uint)(Math.Ceiling(size.Z / increment) + 1);
            var gridOrigin = bounds.Min + Vector3.One * increment / 2;

            using var e = new ObjExporter(@"E:\gush\projects\FacesFiles\Assets\m.obj");
            e.StartGroup("m");
            foreach (var mesh in meshes)
            {
                e.WriteMesh(mesh);
            }

            var grid = new GridParameters(gridSizeX, gridSizeY, gridSizeZ, gridOrigin, increment);
            var protoGrid = FacesConverter.Convert(triangles, grid);

            var sector = DumpTranslation(protoGrid, bounds);
            File.WriteAllText(@"D:\m.json",JsonConvert.SerializeObject(protoGrid, Formatting.Indented));
            using var output = File.OpenWrite(@"e:\gush\projects\cognite\reveal\examples\public\primitives\sector_0.f3d");
            F3dWriter.WriteSector(sector, output);
            Console.WriteLine($"Bounds: {bounds.Min.ToString("G4")}{bounds.Max.ToString("G4")}");
        }

        [Test]
        public void ConvertSimpleMesh2()
        {
            var center = new Vector3(15, 15, 13);
            var matrix = Matrix4x4.CreateTranslation(center);
            var size = new Vector3(1, 1, 1);
            var rvmBounds = new RvmBoundingBox(center - size / 2, center + size / 2);
            //var mesh = TessellatorBridge.Tessellate(new RvmBox(1, matrix, rvmBounds, 1f, 1f, 1f), 0.1f);
            //var mesh = TessellatorBridge.Tessellate(new RvmCylinder(1, matrix, rvmBounds, 0.5f, 0.7f), 0.01f);
            var mesh = TessellatorBridge.Tessellate(new RvmSphere(
                1, matrix, new RvmBoundingBox(-Vector3.One * 0.5f, Vector3.One * 0.5f), 1f), 0.01f);

            var min = mesh.Vertices.Aggregate(Vector3.Min);
            var max = mesh.Vertices.Aggregate(Vector3.Max);
            var bounds = new Raycasting.Bounds(min, max);
            var minDim = MathF.Min(size.X, MathF.Min(size.Y, size.Z));
            var increment = minDim / 10;
            var gridSizeX = (uint)(Math.Ceiling(size.X / increment) + 1);
            var gridSizeY = (uint)(Math.Ceiling(size.Y / increment) + 1);
            var gridSizeZ = (uint)(Math.Ceiling(size.Z / increment) + 1);
            var gridOrigin = bounds.Min + Vector3.One * increment / 2;

            using var e = new ObjExporter(@"E:\gush\projects\FacesFiles\Assets\m.obj");
            e.StartGroup("m");
            e.WriteMesh(mesh);

            var grid = new GridParameters(gridSizeX, gridSizeY, gridSizeZ, gridOrigin, increment);
           /* var protoGrid = FacesConverter.Convert(mesh, grid);

            var sector = DumpTranslation(protoGrid, bounds);
            File.WriteAllText(@"D:\m.json",JsonConvert.SerializeObject(protoGrid, Formatting.Indented));
            using var output = File.OpenWrite(@"e:\gush\projects\cognite\reveal\examples\public\primitives\sector_0.f3d");
            F3dWriter.WriteSector(sector, output);
            Console.WriteLine("Haa");*/
        }

        private FaceFlags ConvertFaceFlags(FacesConverter.FaceDirection direction)
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

        private ulong VectorToIndex(Vector3i v, GridParameters gridParameters)
        {
            return (ulong)(v.X + (gridParameters.GridSizeX - 1) * v.Y + (gridParameters.GridSizeX - 1) * (gridParameters.GridSizeY - 1) * v.Z);
        }

        private SectorFaces DumpTranslation(FacesConverter.ProtoGrid protoGrid, Raycasting.Bounds bounds)
        {
            return new SectorFaces(1, null, bounds.Min, bounds.Max, new FacesGrid(
                protoGrid.GridParameters, new[]
                {
                    new Node(CompressFlags.IndexIsLong, 1, 1, Color.Red, protoGrid.Faces.Select(f =>
                            new Face(ConvertFaceFlags(f.Value), 0, VectorToIndex(f.Key, protoGrid.GridParameters),
                                null)).ToArray()
                    )
                }));
        }

        private IEnumerable<RvmPrimitive> GetMesh(RvmGroup group)
        {
            switch (group)
            {
                case RvmNode node:
                    foreach (var primitive in node.Children.SelectMany(GetMesh))
                        yield return primitive;
                    break;
                case RvmPrimitive primitive:
                    yield return primitive;
                    break;
                default:
                    throw new ArgumentException();

            }
        }
    }
}