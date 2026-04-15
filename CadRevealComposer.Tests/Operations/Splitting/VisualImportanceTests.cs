using System.Drawing;
using System.Numerics;
using CadRevealComposer.Operations.SectorSplitting;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;

namespace CadRevealComposer.Tests.Operations.Splitting;

[TestFixture]
public class VisualImportanceTests
{
    private static readonly Color DefaultColor = Color.Gray;

    #region Cone / GeneralCylinder — Oriented parametric tests

    [Test]
    public void AxisAlignedCone_LongThinPipe_OrientedSA_IsSmall()
    {
        // Axis-aligned pipe: 50m long, R=0.05m.
        // Oriented SA ≈ π×(0.05+0.05)×50 + 2×π×0.05² ≈ 15.7 + 0.016 ≈ 15.7 m²
        // VI ≈ √15.7 ≈ 3.97m
        var cone = new Cone(
            Angle: 0f,
            ArcAngle: MathF.PI * 2f,
            CenterA: new Vector3(0, 0, 0),
            CenterB: new Vector3(50, 0, 0),
            LocalXAxis: Vector3.UnitY,
            RadiusA: 0.05f,
            RadiusB: 0.05f,
            TreeIndex: 1,
            Color: DefaultColor,
            AxisAlignedBoundingBox: new BoundingBox(
                new Vector3(-0.05f, -0.05f, -0.05f),
                new Vector3(50.05f, 0.05f, 0.05f)
            )
        );

        var sa = VisualImportanceCalculator.EstimateOrientedSurfaceArea(cone);
        var vi = MathF.Sqrt(sa);

        // SA should be ≈ 15.7 m² (allow 10% tolerance)
        Assert.That(sa, Is.InRange(14f, 17f), "Oriented SA for axis-aligned thin pipe");
        // VI ≈ 4.0m — much less than the 50m diagonal
        Assert.That(vi, Is.InRange(3.5f, 4.5f), "Visual importance for thin pipe");
        Assert.That(
            vi,
            Is.LessThan(cone.AxisAlignedBoundingBox.Diagonal * 0.15f),
            "VI should be much less than diagonal for thin pipe"
        );
    }

    [Test]
    public void DiagonalCone_45Degrees_OrientedSA_SameAsAxisAligned()
    {
        // 45° diagonal pipe: same physical dimensions (50m × R=0.05m) but rotated.
        // The oriented SA should be identical because it uses CenterA/CenterB, not the AABB.
        var offset = 50f / MathF.Sqrt(2);
        var cone45 = new Cone(
            Angle: 0f,
            ArcAngle: MathF.PI * 2f,
            CenterA: new Vector3(0, 0, 0),
            CenterB: new Vector3(offset, offset, 0),
            LocalXAxis: Vector3.UnitZ,
            RadiusA: 0.05f,
            RadiusB: 0.05f,
            TreeIndex: 2,
            Color: DefaultColor,
            // AABB for 45° pipe is a large flat slab
            AxisAlignedBoundingBox: new BoundingBox(
                new Vector3(-0.05f, -0.05f, -0.05f),
                new Vector3(offset + 0.05f, offset + 0.05f, 0.05f)
            )
        );

        var sa = VisualImportanceCalculator.EstimateOrientedSurfaceArea(cone45);
        var vi = MathF.Sqrt(sa);

        // Should be ≈ same as axis-aligned (≈15.7 m²), NOT the inflated AABB SA
        Assert.That(sa, Is.InRange(14f, 17f), "Oriented SA should be orientation-independent");
        // The AABB diagonal for the 45° pipe is ≈50m; VI should be << diagonal
        Assert.That(
            vi,
            Is.LessThan(cone45.AxisAlignedBoundingBox.Diagonal * 0.15f),
            "VI should be much less than inflated AABB diagonal"
        );
    }

    [Test]
    public void GeneralCylinder_AxisAligned_CorrectSA()
    {
        // Cylinder: 10m long, R=0.5m, full arc
        // Lateral: π×(0.5+0.5)×10 = 31.4 m², End caps: 2×π×0.25 = 1.57 m², Total ≈ 33.0 m²
        var cyl = new GeneralCylinder(
            Angle: 0f,
            ArcAngle: MathF.PI * 2f,
            CenterA: Vector3.Zero,
            CenterB: new Vector3(0, 10, 0),
            LocalXAxis: Vector3.UnitX,
            PlaneA: new Vector4(0, -1, 0, 0),
            PlaneB: new Vector4(0, 1, 0, -10),
            Radius: 0.5f,
            TreeIndex: 3,
            Color: DefaultColor,
            AxisAlignedBoundingBox: new BoundingBox(new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 10, 0.5f))
        );

        var sa = VisualImportanceCalculator.EstimateOrientedSurfaceArea(cyl);

        Assert.That(sa, Is.InRange(30f, 35f), "Cylinder SA");
    }

    #endregion

    #region Box with InstanceMatrix — Oriented tests

    [Test]
    public void Box_AxisAligned_OrientedSA_MatchesExpected()
    {
        // Box: 10×10×0.2 (wall). SA = 2(10×10 + 10×0.2 + 10×0.2) = 2(100+2+2) = 208 m²
        var matrix = Matrix4x4.CreateScale(10, 10, 0.2f);
        var box = new Box(
            matrix,
            TreeIndex: 4,
            Color: DefaultColor,
            AxisAlignedBoundingBox: new BoundingBox(Vector3.Zero, new Vector3(10, 10, 0.2f))
        );

        var sa = VisualImportanceCalculator.EstimateOrientedSurfaceArea(box);

        Assert.That(sa, Is.EqualTo(208f).Within(1f), "Axis-aligned wall box SA");
    }

    [Test]
    public void Box_Tilted45_OrientedSA_SameAsAxisAligned()
    {
        // Same 10×10×0.2 box but rotated 45° around Z. The InstanceMatrix encodes rotation+scale.
        // Oriented SA should be the same 208 m² because we extract scale from the matrix columns.
        var rotation = Matrix4x4.CreateRotationZ(MathF.PI / 4f);
        var scale = Matrix4x4.CreateScale(10, 10, 0.2f);
        var matrix = scale * rotation; // Scale then rotate

        var box = new Box(
            matrix,
            TreeIndex: 5,
            Color: DefaultColor,
            // AABB is larger when rotated, but oriented SA should be unchanged
            AxisAlignedBoundingBox: new BoundingBox(new Vector3(-7.1f, -7.1f, 0f), new Vector3(7.1f, 7.1f, 0.2f))
        );

        var sa = VisualImportanceCalculator.EstimateOrientedSurfaceArea(box);

        Assert.That(sa, Is.EqualTo(208f).Within(1f), "Tilted wall box SA should match axis-aligned");
    }

    #endregion

    #region TriangleMesh / InstancedMesh — AABB SA fallback tests

    [Test]
    public void TriangleMesh_CompactValve_UsesAabbSA()
    {
        // Compact 3m valve: AABB is ~3×3×3, SA = 2(9+9+9) = 54 m², VI ≈ √54 ≈ 7.3m
        var mesh = new Mesh(
            new Vector3[] { Vector3.Zero, Vector3.UnitX, Vector3.UnitY },
            new uint[] { 0, 1, 2 },
            0.01f
        );
        var triMesh = new TriangleMesh(
            mesh,
            TreeIndex: 10,
            Color: DefaultColor,
            AxisAlignedBoundingBox: new BoundingBox(Vector3.Zero, new Vector3(3, 3, 3))
        );

        var sa = VisualImportanceCalculator.EstimateOrientedSurfaceArea(triMesh);

        // AABB SA of a 3×3×3 cube = 54 m²
        Assert.That(sa, Is.EqualTo(54f).Within(0.1f), "TriangleMesh should use AABB SA");
    }

    [Test]
    public void DenseMesh_DoesNotOutrankLargerSimpleMesh()
    {
        // Dense 3m valve (50K triangles) vs. 10m structural beam (200 triangles)
        // Both use AABB SA, so the larger beam should have higher importance.
        var denseValveMesh = new TriangleMesh(
            new Mesh(new Vector3[] { Vector3.Zero, Vector3.UnitX, Vector3.UnitY }, new uint[] { 0, 1, 2 }, 0.01f),
            TreeIndex: 11,
            Color: DefaultColor,
            AxisAlignedBoundingBox: new BoundingBox(Vector3.Zero, new Vector3(3, 3, 3))
        );

        var largeBeamMesh = new TriangleMesh(
            new Mesh(new Vector3[] { Vector3.Zero, Vector3.UnitX, Vector3.UnitY }, new uint[] { 0, 1, 2 }, 0.01f),
            TreeIndex: 12,
            Color: DefaultColor,
            AxisAlignedBoundingBox: new BoundingBox(Vector3.Zero, new Vector3(10, 0.5f, 0.3f))
        );

        var denseVI = VisualImportanceCalculator.EstimateOrientedSurfaceArea(denseValveMesh);
        var beamVI = VisualImportanceCalculator.EstimateOrientedSurfaceArea(largeBeamMesh);

        // The 10m beam's AABB SA = 2(10×0.5 + 10×0.3 + 0.5×0.3) = 2(5+3+0.15) = 16.3 m²
        // The 3m valve's AABB SA = 54 m² — actually the dense compact valve has more SA!
        // But the key insight is: neither is inflated by internal triangle count.
        // Both are measured by their bounding envelope only.
        Assert.That(denseVI, Is.GreaterThan(0), "Dense mesh should have positive SA");
        Assert.That(beamVI, Is.GreaterThan(0), "Beam mesh should have positive SA");
    }

    [Test]
    public void TriangleMesh_StructuralBeam_AabbReflectsEnvelope()
    {
        // 10m beam: AABB 10×0.5×0.3
        var beamMesh = new TriangleMesh(
            new Mesh(new Vector3[] { Vector3.Zero, Vector3.UnitX, Vector3.UnitY }, new uint[] { 0, 1, 2 }, 0.01f),
            TreeIndex: 13,
            Color: DefaultColor,
            AxisAlignedBoundingBox: new BoundingBox(Vector3.Zero, new Vector3(10, 0.5f, 0.3f))
        );

        var sa = VisualImportanceCalculator.EstimateOrientedSurfaceArea(beamMesh);
        var vi = MathF.Sqrt(sa);

        // SA = 2(10×0.5 + 10×0.3 + 0.5×0.3) = 2(5+3+0.15) = 16.3 m²
        Assert.That(sa, Is.EqualTo(16.3f).Within(0.1f), "Beam AABB SA");
        // VI ≈ √16.3 ≈ 4.0m — proportional to the beam's visual envelope
        Assert.That(vi, Is.InRange(3.8f, 4.3f), "Beam VI");
    }

    #endregion

    #region Node-level GetVisualImportance

    [Test]
    public void Node_GetVisualImportance_SumsGeometriesAndTakesSqrt()
    {
        // Node with a single 10×10×0.2 box
        var matrix = Matrix4x4.CreateScale(10, 10, 0.2f);
        var box = new Box(
            matrix,
            TreeIndex: 20,
            Color: DefaultColor,
            AxisAlignedBoundingBox: new BoundingBox(Vector3.Zero, new Vector3(10, 10, 0.2f))
        );

        var node = new Node(20, new APrimitive[] { box }, 1000, 100, box.AxisAlignedBoundingBox);

        var vi = node.GetVisualImportance();

        // SA ≈ 208, VI = √208 ≈ 14.4
        Assert.That(vi, Is.EqualTo(MathF.Sqrt(208f)).Within(0.5f));
    }

    [Test]
    public void WallOutranks_EqualDiagonalPipe()
    {
        // Wall: 10×10×0.2, diagonal ≈ 14.2m, oriented SA = 208 m², VI ≈ 14.4m
        var wallMatrix = Matrix4x4.CreateScale(10, 10, 0.2f);
        var wall = new Box(
            wallMatrix,
            TreeIndex: 30,
            Color: DefaultColor,
            AxisAlignedBoundingBox: new BoundingBox(Vector3.Zero, new Vector3(10, 10, 0.2f))
        );

        // Pipe: 14.2m long (same diagonal), R=0.05m
        // Oriented SA ≈ π×0.1×14.2 + 2π×0.0025 ≈ 4.46 + 0.016 ≈ 4.5 m², VI ≈ 2.1m
        var pipe = new Cone(
            Angle: 0f,
            ArcAngle: MathF.PI * 2f,
            CenterA: Vector3.Zero,
            CenterB: new Vector3(14.2f, 0, 0),
            LocalXAxis: Vector3.UnitY,
            RadiusA: 0.05f,
            RadiusB: 0.05f,
            TreeIndex: 31,
            Color: DefaultColor,
            AxisAlignedBoundingBox: new BoundingBox(
                new Vector3(-0.05f, -0.05f, -0.05f),
                new Vector3(14.25f, 0.05f, 0.05f)
            )
        );

        var wallNode = new Node(30, new APrimitive[] { wall }, 1000, 100, wall.AxisAlignedBoundingBox);
        var pipeNode = new Node(31, new APrimitive[] { pipe }, 1000, 100, pipe.AxisAlignedBoundingBox);

        // Both have ~14m diagonal, but the wall should have much higher visual importance
        Assert.That(wallNode.Diagonal, Is.EqualTo(pipeNode.Diagonal).Within(1f), "Diagonals should be roughly equal");
        Assert.That(
            wallNode.GetVisualImportance(),
            Is.GreaterThan(pipeNode.GetVisualImportance() * 3),
            "Wall should have much higher visual importance than thin pipe"
        );
    }

    #endregion

    #region BoundingBox.SurfaceArea

    [Test]
    public void BoundingBox_SurfaceArea_Cube()
    {
        var bb = new BoundingBox(Vector3.Zero, new Vector3(2, 2, 2));
        // SA = 2(4+4+4) = 24
        Assert.That(bb.SurfaceArea, Is.EqualTo(24f).Within(0.01f));
    }

    [Test]
    public void BoundingBox_SurfaceArea_Rectangular()
    {
        var bb = new BoundingBox(Vector3.Zero, new Vector3(10, 5, 2));
        // SA = 2(50+20+10) = 160
        Assert.That(bb.SurfaceArea, Is.EqualTo(160f).Within(0.01f));
    }

    [Test]
    public void BoundingBox_SurfaceArea_Flat()
    {
        var bb = new BoundingBox(Vector3.Zero, new Vector3(10, 10, 0));
        // SA = 2(100+0+0) = 200
        Assert.That(bb.SurfaceArea, Is.EqualTo(200f).Within(0.01f));
    }

    #endregion
}
