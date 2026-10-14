namespace RvmSharp.Tests.Tessellator;

using System;
using System.Numerics;
using Commons.Utils;
using NUnit.Framework;
using RvmSharp.Primitives;
using Tessellation;

[TestFixture]
public class TessellatorBridgeTests
{
    private static readonly RvmBoundingBox ArbitraryBoundingBox = new RvmBoundingBox(
        Min: Vector3.Zero,
        Max: Vector3.One
    );

    [TestFixture]
    public class TessellateFacetGroupTests
    {
        [Test]
        public void TessellateFacetGroup_WithHole_UsesOnlyTheSegmentVertices()
        {
            var origin = new Vector3(10000, 20000, 3);
            var vertices = new (Vector3 Vertex, Vector3 Normal)[]
            {
                (new Vector3(float.NaN), Vector3.Zero),
                (origin, Vector3.UnitZ),
                (origin + new Vector3(4, 0, 0), Vector3.UnitZ),
                (origin + new Vector3(4, 4, 0), Vector3.UnitZ),
                (origin + new Vector3(0, 4, 0), Vector3.UnitZ),
                (origin + new Vector3(1, 1, 0), Vector3.UnitZ),
                (origin + new Vector3(1, 3, 0), Vector3.UnitZ),
                (origin + new Vector3(3, 3, 0), Vector3.UnitZ),
                (origin + new Vector3(3, 1, 0), Vector3.UnitZ),
                (new Vector3(float.NaN), Vector3.Zero),
            };
            var contours = new RvmFacetGroup.RvmContour[4];
            contours[1] = new RvmFacetGroup.RvmContour(new ArraySegment<(Vector3, Vector3)>(vertices, 1, 4));
            contours[2] = new RvmFacetGroup.RvmContour(new ArraySegment<(Vector3, Vector3)>(vertices, 5, 4));
            var facetGroup = new RvmFacetGroup(
                1,
                Matrix4x4.Identity,
                new RvmBoundingBox(origin, origin + new Vector3(4, 4, 0)),
                [new RvmFacetGroup.RvmPolygon(new ArraySegment<RvmFacetGroup.RvmContour>(contours, 1, 2))]
            );

            var mesh = TessellatorBridge.TessellateWithoutApplyingMatrix(facetGroup, 1, 0.1f);

            Assert.That(mesh, Is.Not.Null);
            Assert.That(mesh.Triangles, Has.Length.EqualTo(24));
            var area = 0.0f;
            for (var triangleIndex = 0; triangleIndex < mesh.Triangles.Length; triangleIndex += 3)
            {
                var first = mesh.Vertices[mesh.Triangles[triangleIndex]];
                var second = mesh.Vertices[mesh.Triangles[triangleIndex + 1]];
                var third = mesh.Vertices[mesh.Triangles[triangleIndex + 2]];
                area += Vector3.Cross(second - first, third - first).Length() / 2;
            }
            Assert.That(area, Is.EqualTo(12).Within(0.00001f));
            Assert.That(mesh.Normals, Is.All.EqualTo(Vector3.UnitZ));
            Assert.That(vertices[1].Vertex, Is.EqualTo(origin));
            Assert.That(facetGroup.CalculateBoundingBoxFromVertexPositions(), Is.EqualTo(facetGroup.BoundingBoxLocal));
        }
    }

    [TestFixture]
    public class TessellateBoxTests
    {
        [Test]
        public void TessellateBox_WithUnitBox_ReturnsExpected1x1Mesh()
        {
            var unitBox = new RvmBox(
                1,
                Matrix4x4.Identity,
                new RvmBoundingBox(Min: new Vector3(-0.5f, -0.5f, -0.5f), Max: new Vector3(0.5f, 0.5f, 0.5f)),
                1,
                1,
                1
            );

            var randomToleranceValue = 0.1f;
            var box = TessellatorBridge.TessellateWithoutApplyingMatrix(unitBox, 1, randomToleranceValue);
            Assert.That(box, Is.Not.Null);
            Assert.That(box.Vertices, Has.Exactly(24).Items);
        }
    }

    [TestFixture]
    public class TessellatePyramidTests
    {
        [Test]
        public void TessellatePyramid_WithUnitPyramid_MatchesReferenceMethod()
        {
            var unitPyramid = new RvmPyramid(1, Matrix4x4.Identity, ArbitraryBoundingBox, 1, 1, 0, 1, 0, 0, 1);

            var randomToleranceValue = 0.1f;
            var pyramid = TessellatorBridge.TessellateWithoutApplyingMatrix(unitPyramid, 1, randomToleranceValue);
            Assert.That(pyramid, Is.Not.Null);
        }

        [Test]
        public void TessellatePyramid_WithHeightZero_HasNormalsThatAreNotNan()
        {
            var unitPyramid = new RvmPyramid(1, Matrix4x4.Identity, ArbitraryBoundingBox, 1, 1, 0, 1, 0, 0, 0);

            var unusedTolerance = 0.1f;
            var pyramid = TessellatorBridge.Tessellate(unitPyramid, unusedTolerance);
            Assert.That(pyramid, Is.Not.Null);

            // Normals should never be NaN or infinite
            Assert.That(
                pyramid.Normals,
                Has.All.Matches<Vector3>(x => x.X.IsFinite() && x.Y.IsFinite() && x.Z.IsFinite())
            );
        }
    }

    [TestFixture]
    public class TessellateCylinderTests
    {
        [Test]
        public void TessellatePyramid_WithUnitPyramid_MatchesReferenceMethod()
        {
            var unitCylinder = new RvmCylinder(1, Matrix4x4.Identity, ArbitraryBoundingBox, 1, 1);

            var randomToleranceValue = 0.1f;
            var cylinder = TessellatorBridge.TessellateWithoutApplyingMatrix(unitCylinder, 1, randomToleranceValue);

            Assert.That(cylinder, Is.Not.Null);
            Assert.That(cylinder.Triangles, Has.Exactly(156).Items);
        }
    }

    [TestFixture]
    public class TessellateLineTests
    {
        [Test]
        public void TessellateLine_IsNotPossible_ReturnsNull()
        {
            // If somehow Line is tessellated, improve this test.
            var rvmLine = new RvmLine(1, Matrix4x4.Identity, ArbitraryBoundingBox, 1, 3);

            var randomToleranceValue = 0.1f;
            var line = TessellatorBridge.TessellateWithoutApplyingMatrix(rvmLine, 1, randomToleranceValue);

            Assert.That(line, Is.Null);
        }
    }
}
