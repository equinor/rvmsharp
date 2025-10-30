namespace CadRevealComposer.Tests.Operations;

using CadRevealComposer.Operations.SectorSplitting;
using NUnit.Framework;

[TestFixture]
public class SectorSplitterBudgetInfoTests
{
    private const long ByteSizeBudget = 2_000_000;

    [Test]
    public void DetermineBudgetExceededInfo_SingleBudgetExceeded_ReturnsCorrectReason()
    {
        var (reason1, info1) = SectorSplitterOctree.DetermineBudgetExceededInfo(ByteSizeBudget, -100, 100, 100);
        Assert.That(reason1, Is.EqualTo(SplitReason.BudgetByteSize));
        Assert.That(info1.ByteSizeBudget, Is.Not.Null);
        Assert.That(info1.PrimitiveCountBudget, Is.Null);

        var (reason2, info2) = SectorSplitterOctree.DetermineBudgetExceededInfo(ByteSizeBudget, 100, 0, 100);
        Assert.That(reason2, Is.EqualTo(SplitReason.BudgetPrimitiveCount));
        Assert.That(info2.PrimitiveCountBudget, Is.Not.Null);
        Assert.That(info2.ByteSizeBudget, Is.Null);

        var (reason3, info3) = SectorSplitterOctree.DetermineBudgetExceededInfo(ByteSizeBudget, 100, 100, -100);
        Assert.That(reason3, Is.EqualTo(SplitReason.BudgetTriangleCount));
        Assert.That(info3.TriangleCountBudget, Is.Not.Null);
        Assert.That(info3.ByteSizeBudget, Is.Null);
    }

    [Test]
    public void DetermineBudgetExceededInfo_MultipleBudgetsExceeded_ReturnsBudgetMultiple()
    {
        var (reason, info) = SectorSplitterOctree.DetermineBudgetExceededInfo(ByteSizeBudget, -100, 0, 100);

        Assert.That(reason, Is.EqualTo(SplitReason.BudgetMultiple));
        Assert.That(info.ByteSizeBudget, Is.Not.Null);
        Assert.That(info.PrimitiveCountBudget, Is.Not.Null);
        Assert.That(info.TriangleCountBudget, Is.Null);
    }

    [Test]
    public void DetermineBudgetExceededInfo_BudgetUsedCalculation_IsCorrect()
    {
        var (_, info) = SectorSplitterOctree.DetermineBudgetExceededInfo(ByteSizeBudget, -1000, 100, 100);

        Assert.That(info.ByteSizeUsed, Is.EqualTo(ByteSizeBudget + 1000));
    }

    [TestCase(0L, true)]
    [TestCase(-1L, true)]
    [TestCase(1L, false)]
    public void DetermineBudgetExceededInfo_BoundaryConditions_Correct(long budgetLeft, bool isExceeded)
    {
        var (reason, _) = SectorSplitterOctree.DetermineBudgetExceededInfo(ByteSizeBudget, budgetLeft, 100, 100);

        Assert.That(reason == SplitReason.BudgetByteSize, Is.EqualTo(isExceeded));
    }
}
