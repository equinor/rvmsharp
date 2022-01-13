namespace CadRevealComposer.Tests.Operations
{
    using AlgebraExtensions;
    using CadRevealComposer.Operations;
    using CadRevealComposer.Utils;
    using Faces;
    using NUnit.Framework;
    using RvmSharp.BatchUtils;
    using RvmSharp.Containers;
    using RvmSharp.Primitives;
    using RvmSharp.Tessellation;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using Utils;

    [TestFixture]
    public class SectorToFacesConverterTests
    {
        [Test]
        public void ConvertSimpleMesh()
        {
            // Collect RVM files

            var workload = Workload.CollectWorkload(new[] { $"{TestSampleLoader.GlobalTestSamplesDirectory}/Huldra" });
            // Read RVM
            var store = Workload.ReadRvmData(workload);
            var rootNodes = store.RvmFiles.SelectMany(f => f.Model.Children);
            var meshes = rootNodes.SelectMany(GetMesh).ToArray()
                .Select(p => TessellatorBridge.Tessellate(p, 0.01f))
                .WhereNotNull().ToArray();

            var triangles = meshes.SelectMany(SectorToFacesConverter.CollectTrianglesForMesh).ToArray();

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

            var grid = new GridParameters(gridSizeX, gridSizeY, gridSizeZ, gridOrigin, increment);
            var protoGrid = SectorToFacesConverter.Convert(triangles, grid);
            // NOTE: Once compression is implemented, this test should be changed to a more sophisticated one
            Assert.That(protoGrid.Faces.Count, Is.EqualTo(19661));
        }

        [Test]
        public void ConvertRvmBox()
        {
            var center = new Vector3(15, 15, 13);
            var matrix = Matrix4x4.CreateTranslation(center);
            var size = new Vector3(1, 1, 1);
            var rvmBounds = new RvmBoundingBox(center - size / 2, center + size / 2);
            var mesh = TessellatorBridge.Tessellate(new RvmBox(1, matrix, rvmBounds, 1f, 1f, 1f), 0.1f);

            var triangles = SectorToFacesConverter.CollectTrianglesForMesh(mesh!);

            var min = triangles.SelectMany(t => new []{ t.V1, t.V2, t.V3 }).Aggregate(Vector3.Min);
            var max = triangles.SelectMany(t => new []{ t.V1, t.V2, t.V3 }).Aggregate(Vector3.Max);

            var bounds = new Bounds(min, max);
            var minDim = MathF.Min(size.X, MathF.Min(size.Y, size.Z));
            var increment = minDim / 5;
            var gridSizeX = (uint)(Math.Ceiling(size.X / increment) + 1);
            var gridSizeY = (uint)(Math.Ceiling(size.Y / increment) + 1);
            var gridSizeZ = (uint)(Math.Ceiling(size.Z / increment) + 1);
            var gridOrigin = bounds.Min - Vector3.One * increment / 2;

            var grid = new GridParameters(gridSizeX, gridSizeY, gridSizeZ, gridOrigin, increment);
            var protoGrid = SectorToFacesConverter.Convert(triangles, grid);
            // NOTE: Once compression is implemented, this test should be changed to a more sophisticated one
            Assert.That(protoGrid.Faces.Count, Is.EqualTo(98));
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