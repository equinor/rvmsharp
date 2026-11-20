namespace RvmSharp.Exe.Tests;

using System.Numerics;
using RvmSharp.Primitives;

[TestFixture]
public class NativeFacetMatcherTests
{
    private const double Tolerance = 0.0001;

    private static readonly Matrix4x4 Millimetres = Matrix4x4.CreateScale(0.001f);

    private static readonly Matrix4x4 Placement =
        Matrix4x4.CreateFromYawPitchRoll(0.7f, -1.1f, 2.3f) * Matrix4x4.CreateTranslation(120.5f, -14.25f, 33.125f);

    /// <summary>An irregular wedge with no symmetry, in millimetres.</summary>
    private static RvmFacetGroup Wedge(Matrix4x4 matrix, float size = 100f, Vector3 nudge = default)
    {
        Vector3[] corners =
        [
            new(0, 0, 0),
            new(1.0f, 0, 0),
            new(0.9f, 0.6f, 0),
            new(0, 0.4f, 0),
            new(0.1f, 0.1f, 0.5f),
            new(0.7f, 0.2f, 0.3f),
        ];
        corners = corners.Select(corner => corner * size).ToArray();
        corners[5] += nudge;
        int[][] faces =
        [
            [0, 4, 5, 1],
            [1, 5, 2],
            [2, 5, 4, 3],
            [3, 4, 0],
            [0, 1, 2, 3],
        ];
        var polygons = faces
            .Select(face => new RvmFacetGroup.RvmPolygon([
                new RvmFacetGroup.RvmContour(face.Select(corner => (corners[corner], Vector3.UnitZ)).ToArray()),
            ]))
            .ToArray();
        return new RvmFacetGroup(1, matrix, new RvmBoundingBox(Vector3.Zero, Vector3.One * size), polygons);
    }

    private static FacetShape Shape(RvmFacetGroup facetGroup, int ordinal = 0) =>
        FacetShape.Create(facetGroup, 0, ordinal)!;

    private static Vector3 Point(FacetShape shape, int vertex) =>
        new(shape.Points[vertex * 3], shape.Points[vertex * 3 + 1], shape.Points[vertex * 3 + 2]);

    private static Vector3 Apply(FacetFit fit, Vector3 point)
    {
        var linear = fit.Linear;
        return new Vector3(
            (float)(linear.M11 * point.X + linear.M12 * point.Y + linear.M13 * point.Z),
            (float)(linear.M21 * point.X + linear.M22 * point.Y + linear.M23 * point.Z),
            (float)(linear.M31 * point.X + linear.M32 * point.Y + linear.M33 * point.Z)
        );
    }

    [TestCase(100f)]
    [TestCase(3f, Description = "Smaller than the existing matcher's absolute determinant cutoff")]
    public void RotatedAndTranslatedCopyFitsRigidly(float size)
    {
        var reference = Shape(Wedge(Millimetres, size));
        var copy = Shape(Wedge(Millimetres * Placement, size));

        Assert.That(FacetAlignment.TryFit(reference, copy, FacetMatchMode.Rigid, Tolerance, out var fit));
        Assert.Multiple(() =>
        {
            Assert.That(fit.Linear.Determinant, Is.EqualTo(1).Within(1e-9));
            for (var vertex = 0; vertex < reference.VertexCount; vertex++)
            {
                var distance = Vector3.Distance(Apply(fit, Point(reference, vertex)), Point(copy, vertex));
                Assert.That(distance, Is.LessThan(Tolerance));
            }
        });
    }

    [Test]
    public void MirroredCopyNeedsTheMirroredMode()
    {
        var reference = Shape(Wedge(Millimetres));
        var mirrored = Shape(Wedge(Millimetres * Matrix4x4.CreateScale(-1, 1, 1) * Placement));

        var rigid = FacetAlignment.TryFit(reference, mirrored, FacetMatchMode.Rigid, Tolerance, out _);
        var relaxed = FacetAlignment.TryFit(
            reference,
            mirrored,
            FacetMatchMode.RigidOrMirrored,
            Tolerance,
            out var fit
        );

        Assert.Multiple(() =>
        {
            Assert.That(rigid, Is.False);
            Assert.That(relaxed);
            Assert.That(fit.Mirrored);
            Assert.That(fit.MaxResidual, Is.LessThan(Tolerance));
        });
    }

    [Test]
    public void StretchedCopyOnlyFitsAffinely()
    {
        var reference = Shape(Wedge(Millimetres));
        var stretched = Shape(Wedge(Millimetres * Matrix4x4.CreateScale(1, 2.5f, 1) * Placement));

        var rigid = FacetAlignment.TryFit(reference, stretched, FacetMatchMode.RigidOrMirrored, Tolerance, out _);
        var affine = FacetAlignment.TryFit(reference, stretched, FacetMatchMode.Affine, Tolerance, out var fit);

        Assert.Multiple(() =>
        {
            Assert.That(rigid, Is.False);
            Assert.That(affine);
            Assert.That(fit.MaxResidual, Is.LessThan(Tolerance));
        });
    }

    [Test]
    public void PlanarFacetGroupFitsRigidly()
    {
        static RvmFacetGroup Plate(Matrix4x4 matrix)
        {
            (Vector3, Vector3)[] outline =
            [
                (new Vector3(0, 0, 0), Vector3.UnitZ),
                (new Vector3(300, 0, 0), Vector3.UnitZ),
                (new Vector3(260, 200, 0), Vector3.UnitZ),
                (new Vector3(40, 120, 0), Vector3.UnitZ),
            ];
            return new RvmFacetGroup(
                1,
                matrix,
                new RvmBoundingBox(Vector3.Zero, new Vector3(300, 200, 0)),
                [new RvmFacetGroup.RvmPolygon([new RvmFacetGroup.RvmContour(outline)])]
            );
        }

        var reference = Shape(Plate(Millimetres));
        var copy = Shape(Plate(Millimetres * Placement));

        Assert.That(FacetAlignment.TryFit(reference, copy, FacetMatchMode.Rigid, Tolerance, out var fit));
        Assert.That(fit.MaxResidual, Is.LessThan(Tolerance));
    }

    [TestCase(0.05f, true)]
    [TestCase(0.5f, false)]
    public void OneMovedVertexMatchesOnlyWithinTolerance(float nudgeMillimetres, bool expected)
    {
        var reference = Shape(Wedge(Millimetres));
        var nudged = Shape(Wedge(Millimetres * Placement, nudge: new Vector3(0, 0, nudgeMillimetres)));

        var matched = FacetAlignment.TryFit(reference, nudged, FacetMatchMode.Rigid, Tolerance, out _);

        Assert.That(matched, Is.EqualTo(expected));
    }

    [Test]
    public void ClusterGroupsCopiesAndIsIndependentOfInputOrder()
    {
        var shapes = new List<FacetShape>();
        for (var index = 0; index < 6; index++)
        {
            var placement =
                Matrix4x4.CreateFromYawPitchRoll(index, 0.3f * index, -index)
                * Matrix4x4.CreateTranslation(10 * index, -3 * index, index);
            shapes.Add(Shape(Wedge(Millimetres * placement), shapes.Count));
            shapes.Add(Shape(Wedge(Millimetres * placement, 250f), shapes.Count));
        }
        shapes.Add(Shape(Wedge(Millimetres, nudge: new Vector3(0, 0, 4)), shapes.Count));

        static string Members(List<FacetClass> classes) =>
            string.Join(
                " | ",
                classes
                    .Select(group => string.Join(",", group.Members.Select(member => member.Shape.Ordinal).Order()))
                    .Order(StringComparer.Ordinal)
            );
        static int[] Representatives(List<FacetClass> classes) =>
            classes.Select(group => group.Representative.Ordinal).Order().ToArray();

        var forward = NativeFacetMatcher.Cluster(shapes, FacetMatchMode.Rigid, Tolerance);
        var reversed = NativeFacetMatcher.Cluster(
            shapes.AsEnumerable().Reverse().ToList(),
            FacetMatchMode.Rigid,
            Tolerance
        );

        Assert.Multiple(() =>
        {
            Assert.That(Members(forward), Is.EqualTo("0,2,4,6,8,10 | 1,3,5,7,9,11 | 12"));
            Assert.That(Members(reversed), Is.EqualTo(Members(forward)));
            Assert.That(Representatives(reversed), Is.EqualTo(Representatives(forward)));
        });
    }
}
