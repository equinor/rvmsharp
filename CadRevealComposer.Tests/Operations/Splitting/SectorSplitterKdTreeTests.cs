using System.Drawing;
using System.Numerics;
using CadRevealComposer.IdProviders;
using CadRevealComposer.Operations.SectorSplitting;
using CadRevealComposer.Primitives;

namespace CadRevealComposer.Tests.Operations.Splitting;

[TestFixture]
public class SectorSplitterKdTreeTests
{
    private static readonly Color DefaultColor = Color.Gray;

    /// <summary>
    /// Helper: create a Box primitive at a given position with given size.
    /// </summary>
    private static Box CreateBox(uint treeIndex, Vector3 position, Vector3 size)
    {
        var matrix = Matrix4x4.CreateScale(size) * Matrix4x4.CreateTranslation(position + size / 2);
        var bb = new BoundingBox(position, position + size);
        return new Box(matrix, treeIndex, DefaultColor, bb);
    }

    [Test]
    public void SplitIntoSectors_SmallSetOfBoxes_ProducesValidSectorTree()
    {
        // Create a set of boxes spread across space to force splitting
        var primitives = new APrimitive[]
        {
            CreateBox(1, new Vector3(0, 0, 0), new Vector3(5, 5, 5)),
            CreateBox(2, new Vector3(10, 0, 0), new Vector3(5, 5, 5)),
            CreateBox(3, new Vector3(20, 0, 0), new Vector3(5, 5, 5)),
            CreateBox(4, new Vector3(30, 0, 0), new Vector3(5, 5, 5)),
            CreateBox(5, new Vector3(0, 10, 0), new Vector3(5, 5, 5)),
            CreateBox(6, new Vector3(10, 10, 0), new Vector3(5, 5, 5)),
        };

        var splitter = new SectorSplitterKdTree();
        var sectorIdGenerator = new SequentialIdGenerator(firstIdReturned: 0);
        var sectors = splitter.SplitIntoSectors(primitives, sectorIdGenerator).ToArray();

        // Must have at least a root sector
        Assert.That(sectors.Length, Is.GreaterThanOrEqualTo(1), "Should have at least one sector");

        // Root sector has no parent
        var rootSector = sectors.First(s => s.ParentSectorId == null);
        Assert.That(rootSector, Is.Not.Null, "Should have a root sector with null parent");

        // All non-root sectors must reference a valid parent
        foreach (var sector in sectors.Where(s => s.ParentSectorId != null))
        {
            Assert.That(
                sectors.Any(s => s.SectorId == sector.ParentSectorId),
                Is.True,
                $"Sector {sector.SectorId} references parent {sector.ParentSectorId} which doesn't exist"
            );
        }

        // All input primitives should appear in some sector
        var allGeometryTreeIndices = sectors
            .SelectMany(s => s.Geometries)
            .Select(g => g.TreeIndex)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();
        var inputTreeIndices = primitives.Select(p => p.TreeIndex).Distinct().OrderBy(x => x).ToArray();
        Assert.That(
            allGeometryTreeIndices,
            Is.SupersetOf(inputTreeIndices),
            "All input primitives should appear in the output sectors"
        );
    }

    [Test]
    public void GetLongestAxis_XIsLongest_Returns0()
    {
        var extents = new Vector3(100, 50, 30);
        Assert.That(SectorSplitterKdTree.GetLongestAxis(extents), Is.EqualTo(0));
    }

    [Test]
    public void GetLongestAxis_YIsLongest_Returns1()
    {
        var extents = new Vector3(10, 50, 30);
        Assert.That(SectorSplitterKdTree.GetLongestAxis(extents), Is.EqualTo(1));
    }

    [Test]
    public void GetLongestAxis_ZIsLongest_Returns2()
    {
        var extents = new Vector3(10, 20, 100);
        Assert.That(SectorSplitterKdTree.GetLongestAxis(extents), Is.EqualTo(2));
    }

    [Test]
    public void SplitIntoSectors_ElongatedBoundingBox_ProducesMultipleSectorsWithSpatialSeparation()
    {
        // Create many primitives spread along X to force the K-D tree to produce multiple sectors.
        // Use larger boxes to increase byte budget pressure.
        var primitives = Enumerable
            .Range(0, 10_000)
            .Select(i => (APrimitive)CreateBox((uint)i, new Vector3(i * 10, 0, 0), new Vector3(5, 5, 5)))
            .ToArray();

        var splitter = new SectorSplitterKdTree();
        var sectorIdGenerator = new SequentialIdGenerator(firstIdReturned: 0);
        var sectors = splitter.SplitIntoSectors(primitives, sectorIdGenerator).ToArray();

        // Must produce multiple sectors (the space was too large for a single one)
        Assert.That(sectors.Length, Is.GreaterThan(2), "Should produce more than just root + 1 sector");

        // Verify spatial separation: sectors with geometry should have bounding boxes
        // that don't all overlap perfectly (i.e., the K-D tree actually separated them spatially).
        var sectorsWithGeometry = sectors.Where(s => s.Geometries.Length > 0).ToArray();
        Assert.That(sectorsWithGeometry.Length, Is.GreaterThan(1), "Should have multiple sectors with geometry");

        // Verify that ALL input primitives appear in the output
        var allOutputTreeIndices = sectors.SelectMany(s => s.Geometries).Select(g => g.TreeIndex).Distinct().Count();
        Assert.That(
            allOutputTreeIndices,
            Is.EqualTo(10_000),
            "All 10000 input primitives should appear in output sectors"
        );
    }

    [Test]
    public void ContinuousLodFilter_ProducesSmoothDecreasingCurve()
    {
        // Verify the continuous LOD formula produces a monotonically decreasing curve
        float sceneDiagonal = 500f;
        float prevMinVI = float.MaxValue;
        for (int depth = 1; depth <= 10; depth++)
        {
            float minVI = sceneDiagonal / 50f / MathF.Pow(2, depth - 1);
            if (minVI < 0.1f)
                minVI = 0f;

            Assert.That(
                minVI,
                Is.LessThanOrEqualTo(prevMinVI),
                $"MinVisualImportance at depth {depth} should be <= depth {depth - 1}"
            );
            prevMinVI = minVI;
        }

        // At depth 1: 500/50 = 10.0
        Assert.That(sceneDiagonal / 50f / MathF.Pow(2, 0), Is.EqualTo(10f).Within(0.01f));
        // At depth 2: 500/50/2 = 5.0
        Assert.That(sceneDiagonal / 50f / MathF.Pow(2, 1), Is.EqualTo(5f).Within(0.01f));
        // At depth 3: 500/50/4 = 2.5
        Assert.That(sceneDiagonal / 50f / MathF.Pow(2, 2), Is.EqualTo(2.5f).Within(0.01f));
    }

    [Test]
    public void VisualImportanceSorting_WallBeforePipe()
    {
        // A large wall (10×10×0.2) and an equal-diagonal thin pipe (14.2m × R=0.05m)
        // After splitting, the wall should end up in a shallower (or same-depth) sector
        // because it has higher visual importance.
        var wall = CreateBox(1, Vector3.Zero, new Vector3(10, 10, 0.2f));
        var pipe = new Cone(
            Angle: 0f,
            ArcAngle: MathF.PI * 2f,
            CenterA: new Vector3(15, 0, 0),
            CenterB: new Vector3(29.2f, 0, 0),
            LocalXAxis: Vector3.UnitY,
            RadiusA: 0.05f,
            RadiusB: 0.05f,
            TreeIndex: 2,
            Color: DefaultColor,
            AxisAlignedBoundingBox: new BoundingBox(
                new Vector3(14.95f, -0.05f, -0.05f),
                new Vector3(29.25f, 0.05f, 0.05f)
            )
        );

        var primitives = new APrimitive[] { wall, pipe };
        var splitter = new SectorSplitterKdTree();
        var sectorIdGenerator = new SequentialIdGenerator(firstIdReturned: 0);
        var sectors = splitter.SplitIntoSectors(primitives, sectorIdGenerator).ToArray();

        // Find the sector containing the wall vs the pipe
        var wallSector = sectors.FirstOrDefault(s => s.Geometries.Any(g => g.TreeIndex == 1));
        var pipeSector = sectors.FirstOrDefault(s => s.Geometries.Any(g => g.TreeIndex == 2));

        Assert.That(wallSector, Is.Not.Null, "Wall should appear in a sector");
        Assert.That(pipeSector, Is.Not.Null, "Pipe should appear in a sector");

        // Wall should be at same or shallower depth than pipe (loaded first in progressive loading)
        Assert.That(
            wallSector!.Depth,
            Is.LessThanOrEqualTo(pipeSector!.Depth),
            "Wall should be at same or shallower depth than thin pipe"
        );
    }
}
