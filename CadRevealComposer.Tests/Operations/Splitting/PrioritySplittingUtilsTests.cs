#nullable enable
namespace CadRevealComposer.Tests.Operations.Splitting;

using System.Drawing;
using System.Numerics;
using CadRevealComposer.Operations.SectorSplitting;
using Primitives;

[TestFixture]
public class PrioritySplittingUtilsTests
{
    const string DisciplinePipe = "PIPE";
    const string DisciplineCable = "CABLE";

    [Test]
    public void CadRevealNodesWithVariousConfigurations_SetPriorityForHighlightSplittingWithMutation_VerifyCorrectlyMutated()
    {
        const uint treeIndexDisciplinePipeWithTag = 0;
        const uint treeIndexDisciplinePipeMissingTag = 1;
        const uint treeIndexDisciplineCableWithTag = 2;
        const uint treeIndexDisciplineCableMissingTag = 3;

        CadRevealNode[] nodes =
        [
            CreateCadRevealNode(treeIndexDisciplinePipeWithTag, DisciplinePipe, true),
            CreateCadRevealNode(treeIndexDisciplinePipeMissingTag, DisciplinePipe, false),
            CreateCadRevealNode(treeIndexDisciplineCableWithTag, DisciplineCable, true),
            CreateCadRevealNode(treeIndexDisciplineCableMissingTag, DisciplineCable, false),
        ];

        PrioritySplittingUtils.SetPriorityForHighlightSplittingWithMutation(nodes);

        AssertDisciplineAndPriority(nodes[treeIndexDisciplinePipeWithTag].Geometries[0], DisciplinePipe, 1);
        AssertDisciplineAndPriority(nodes[treeIndexDisciplinePipeMissingTag].Geometries[0], DisciplinePipe, 0);
        AssertDisciplineAndPriority(nodes[treeIndexDisciplineCableWithTag].Geometries[0], null, 0);
        AssertDisciplineAndPriority(nodes[treeIndexDisciplineCableMissingTag].Geometries[0], null, 0);
        return;

        void AssertDisciplineAndPriority(APrimitive primitive, string? expectedDiscipline, int expectedPriority)
        {
            Assert.Multiple(() =>
            {
                Assert.That(primitive.Discipline, Is.EqualTo(expectedDiscipline));
                Assert.That(primitive.Priority, Is.EqualTo(expectedPriority));
            });
        }
    }

    [Test]
    public void ThreePrimitiveGroupsWithDifferentCombinationsOfSizes_ConvertPrimitiveGroupsToNodes_ShouldReturnNodesWithCorrectNumberOfPrimitives()
    {
        const float smallPrimitiveSize = 0.01f;
        const float largePrimitiveSize = 1.0f;
        const uint treeIndexOneLargeOneSmallPrimitive = 0;
        const uint treeIndexTwoSmallPrimitives = 1;
        const uint treeIndexTwoLargePrimitives = 2;

        APrimitive[] primitives =
        [
            CreatePrimitive(smallPrimitiveSize, treeIndexOneLargeOneSmallPrimitive, DisciplinePipe),
            CreatePrimitive(largePrimitiveSize, treeIndexOneLargeOneSmallPrimitive, DisciplinePipe),
            CreatePrimitive(smallPrimitiveSize, treeIndexTwoSmallPrimitives, DisciplinePipe),
            CreatePrimitive(smallPrimitiveSize, treeIndexTwoSmallPrimitives, DisciplinePipe),
            CreatePrimitive(largePrimitiveSize, treeIndexTwoLargePrimitives, DisciplinePipe),
            CreatePrimitive(largePrimitiveSize, treeIndexTwoLargePrimitives, DisciplinePipe),
        ];

        var geometryGroups = primitives.GroupBy(primitive => primitive.TreeIndex);

        var result = PrioritySplittingUtils.ConvertPrimitiveGroupsToNodes(geometryGroups);

        Assert.That(result, Has.Length.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(result[treeIndexOneLargeOneSmallPrimitive].Geometries, Has.Length.EqualTo(1));
            Assert.That(result[treeIndexTwoSmallPrimitives].Geometries, Has.Length.EqualTo(2));
            Assert.That(result[treeIndexTwoLargePrimitives].Geometries, Has.Length.EqualTo(2));
        });
    }

    private static Box CreatePrimitive(float size, uint treeIndex, string? discipline = null)
    {
        return new Box(Matrix4x4.Identity, treeIndex, Color.Black, new BoundingBox(Vector3.Zero, size * Vector3.One))
        {
            Discipline = discipline
        };
    }

    private static CadRevealNode CreateCadRevealNode(uint treeIndex, string discipline, bool hasTagAttribute)
    {
        const string dummyName = "dummyName";

        return new CadRevealNode
        {
            Attributes = hasTagAttribute
                ? new Dictionary<string, string> { ["Discipline"] = discipline, ["Tag"] = "tag" }
                : new Dictionary<string, string> { ["Discipline"] = discipline },
            Geometries = [CreatePrimitive(1.0f, treeIndex)],
            TreeIndex = treeIndex,
            Name = dummyName,
            Parent = null
        };
    }
}
