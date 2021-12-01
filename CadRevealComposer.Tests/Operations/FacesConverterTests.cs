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

            var triangles = meshes.SelectMany(SectorToFacesConverter.CollectTriangles).ToArray();

            var min = triangles.SelectMany(t => new []{ t.V1, t.V2, t.V3 }).Aggregate(Vector3.Min);
            var max = triangles.SelectMany(t => new []{ t.V1, t.V2, t.V3 }).Aggregate(Vector3.Max);
            var size = max - min;
            var bounds = new Bounds(min, max);
            var minDim = MathF.Min(size.X, MathF.Min(size.Y, size.Z));
            var increment = minDim / 50;
            Console.WriteLine(increment);
            var gridSizeX = (uint)(Math.Ceiling(size.X / increment) + 1);
            var gridSizeY = (uint)(Math.Ceiling(size.Y / increment) + 1);
            var gridSizeZ = (uint)(Math.Ceiling(size.Z / increment) + 1);
            var gridOrigin = bounds.Min - Vector3.One * increment / 2;

            using var e = new ObjExporter(@"E:\gush\projects\FacesFiles\Assets\m.obj");
            e.StartGroup("m");
            foreach (var mesh in meshes)
            {
                e.WriteMesh(mesh);
            }

            var grid = new GridParameters(gridSizeX, gridSizeY, gridSizeZ, gridOrigin, increment);
            var protoGrid = SectorToFacesConverter.Convert(triangles, grid);

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
            var mesh = TessellatorBridge.Tessellate(new RvmCylinder(1, matrix, rvmBounds, 0.5f, 0.7f), 0.01f);
            //var mesh = TessellatorBridge.Tessellate(new RvmSphere(
//                1, matrix, new RvmBoundingBox(-Vector3.One * 0.5f, Vector3.One * 0.5f), 1f), 0.01f);

            var triangles = SectorToFacesConverter.CollectTriangles(mesh);

            var min = triangles.SelectMany(t => new []{ t.V1, t.V2, t.V3 }).Aggregate(Vector3.Min);
            var max = triangles.SelectMany(t => new []{ t.V1, t.V2, t.V3 }).Aggregate(Vector3.Max);

            var bounds = new Bounds(min, max);
            var minDim = MathF.Min(size.X, MathF.Min(size.Y, size.Z));
            var increment = minDim / 5;
            var gridSizeX = (uint)(Math.Ceiling(size.X / increment) + 1);
            var gridSizeY = (uint)(Math.Ceiling(size.Y / increment) + 1);
            var gridSizeZ = (uint)(Math.Ceiling(size.Z / increment) + 1);
            var gridOrigin = bounds.Min - Vector3.One * increment / 2;

            using var e = new ObjExporter(@"E:\gush\projects\FacesFiles\Assets\m.obj");
            e.StartGroup("m");
            e.WriteMesh(mesh);

            var grid = new GridParameters(gridSizeX, gridSizeY, gridSizeZ, gridOrigin, increment);
            var protoGrid = SectorToFacesConverter.Convert(triangles, grid);

            var sector = DumpTranslation(protoGrid, bounds);
            File.WriteAllText(@"D:\m.json",JsonConvert.SerializeObject(protoGrid, Formatting.Indented));
            //using var output = File.OpenWrite(@"e:\gush\projects\cognite\reveal\examples\public\primitives\sector_0.f3d");
            //F3dWriter.WriteSector(sector, output);
        }

        private struct MeshHolder
        {
            public int[] Indices;
            public Vector3[] Vertices;
            public Vector3[] Normals;
        }

        [Test]
        public void ConvertRandomObj()
        {
            var mh = JsonConvert.DeserializeObject<MeshHolder>(File.ReadAllText("D:/gush.json"));
            var tCount = mh.Indices.Length / 3;
            var triangles = new Triangle[tCount];
            for (var i = 0; i < tCount; i++)
            {
                triangles[i] = new Triangle(mh.Vertices[mh.Indices[i * 3]],
                    mh.Vertices[mh.Indices[i * 3 + 1]], mh.Vertices[mh.Indices[i * 3 + 2]]);
            }

            var min = triangles.SelectMany(t => new []{ t.V1, t.V2, t.V3 }).Aggregate(Vector3.Min);
            var max = triangles.SelectMany(t => new []{ t.V1, t.V2, t.V3 }).Aggregate(Vector3.Max);
            var size = max - min;
            var bounds = new Bounds(min, max);
            var minDim = MathF.Min(size.X, MathF.Min(size.Y, size.Z));
            var increment = minDim / 100;
            Console.WriteLine(increment);
            var gridSizeX = (uint)(Math.Ceiling(size.X / increment) + 1);
            var gridSizeY = (uint)(Math.Ceiling(size.Y / increment) + 1);
            var gridSizeZ = (uint)(Math.Ceiling(size.Z / increment) + 1);
            var gridOrigin = bounds.Min - Vector3.One * increment / 2;

            var grid = new GridParameters(gridSizeX, gridSizeY, gridSizeZ, gridOrigin, increment);
            var protoGrid = SectorToFacesConverter.Convert(triangles, grid);

            var sector = DumpTranslation(protoGrid, bounds);
            File.WriteAllText(@"D:\m.json",JsonConvert.SerializeObject(protoGrid, Formatting.Indented));
            using var output = File.OpenWrite(@"e:\gush\projects\cognite\reveal\examples\public\primitives\sector_0.f3d");
            F3dWriter.WriteSector(sector, output);
            Console.WriteLine($"Bounds: {bounds.Min.ToString("G4")}{bounds.Max.ToString("G4")}");
        }





        private SectorFaces DumpTranslation(SectorToFacesConverter.ProtoGrid protoGrid, Bounds bounds)
        {
            return new SectorFaces(1, null, bounds.Min, bounds.Max, new FacesGrid(
                protoGrid.GridParameters, new[]
                {
                    new Node(CompressFlags.IndexIsLong, 1, 1, Color.Red, protoGrid.Faces.Select(f =>
                            new Face(SectorToFacesConverter.ConvertVisibleSidesToFaceFlags(f.Value), 0, SectorToFacesConverter.GridCellToGridIndex(f.Key, protoGrid.GridParameters),
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