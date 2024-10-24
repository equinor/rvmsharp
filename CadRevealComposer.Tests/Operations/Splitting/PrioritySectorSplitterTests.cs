namespace CadRevealComposer.Tests.Operations.Splitting;

using System.Drawing;
using System.Numerics;
using CadRevealComposer.Operations.SectorSplitting;
using IdProviders;
using Primitives;

[TestFixture]
public class PrioritySectorSplitterTests
{
    const string DisciplinePipe = "PIPE";
    const string DisciplineCable = "CABLE";

    [Test]
    public void OnePipeAndOneCablePrimitive_SplitIntoSectors_ShouldResultInRootSectorAndOneSectorForEachDiscipline()
    {
        APrimitive[] primitives = [CreatePrimitive(0f, 0, DisciplinePipe), CreatePrimitive(2f, 1, DisciplineCable)];

        var splitter = new PrioritySectorSplitter();
        var sectors = splitter.SplitIntoSectors(primitives, new SequentialIdGenerator()).ToArray();

        Assert.That(sectors, Has.Length.EqualTo(3));
    }

    [Test]
    public void FivePrimitivesWhereTwoAreFarAway_SplitIntoSectors_ShouldResultInRootSectorAndOneSectorWithThree()
    {
        var primitives = PositionsToPrimitives([-40f, -10f, 0f, 10f, 40f]);

        var splitter = new PrioritySectorSplitter();
        var sectors = splitter.SplitIntoSectors(primitives, new SequentialIdGenerator()).ToArray();

        Assert.That(sectors, Has.Length.EqualTo(2));
        Assert.That(sectors[1].Geometries, Has.Length.EqualTo(3));
    }

    [Test]
    public void ManyPrimitivesToExceedByteSizeBudget_SplitIntoSectors_ShouldResultMoreThanOneNonRootSectors()
    {
        const int count = 1000;
        var positions = Enumerable
            .Range(0, count + 1)
            .Select(i =>
            {
                var factor = i / (double)count;
                var value = (-1 + 2 * factor) * 10;

                return (float)(Math.Sign(value) * Math.Pow(value, 2));
            });

        var primitives = PositionsToPrimitives(positions);

        var splitter = new PrioritySectorSplitter();
        var sectors = splitter.SplitIntoSectors(primitives, new SequentialIdGenerator()).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(sectors, Has.Length.EqualTo(3));
            Assert.That(sectors[1].Geometries, Has.Length.EqualTo(569));
            Assert.That(sectors[2].Geometries, Has.Length.EqualTo(432));
        });
    }

    private static APrimitive[] PositionsToPrimitives(IEnumerable<float> positions) =>
        positions
            .Select((position, index) => CreatePrimitive(position, (uint)index, DisciplinePipe))
            .ToArray<APrimitive>();

    private static Box CreatePrimitive(float xPosition, uint treeIndex, string discipline)
    {
        var position = new Vector3(xPosition, 0, 0);

        return new Box(
            Matrix4x4.CreateTranslation(xPosition + 0.5f, 0, 0),
            treeIndex,
            Color.Black,
            new BoundingBox(position, position + Vector3.One)
        )
        {
            Discipline = discipline
        };
    }
}
