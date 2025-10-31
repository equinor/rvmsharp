namespace CadRevealComposer.Tests.Operations;

using CadRevealComposer.Operations.SectorSplitting;
using NUnit.Framework;

[TestFixture]
public class SectorSplitterBudgetInfoTests
{
    private const long ByteSizeBudget = 2_000_000;
    private const long PrimitiveBudget = 5_000;
    private const long TriangleBudget = 300_000;

    [Test]
    public void DetermineBudgetExceededInfo_OnlyByteSizeExceeded_ReturnsCorrectReason()
    {
        var (reason, info) = SectorSplitterOctree.DetermineBudgetExceededInfo(ByteSizeBudget, -100, 100, 100);

        Assert.That(reason, Is.EqualTo(SplitReason.BudgetByteSize));
        Assert.That(info.ByteSizeBudget, Is.EqualTo(ByteSizeBudget));
        Assert.That(info.ByteSizeUsed, Is.EqualTo(ByteSizeBudget + 100));
    }

    [Test]
    public void DetermineBudgetExceededInfo_OnlyPrimitiveCountExceeded_ReturnsCorrectReason()
    {
        var (reason, info) = SectorSplitterOctree.DetermineBudgetExceededInfo(ByteSizeBudget, 100, 0, 100);

        Assert.That(reason, Is.EqualTo(SplitReason.BudgetPrimitiveCount));
        Assert.That(info.PrimitiveCountBudget, Is.EqualTo(PrimitiveBudget));
        Assert.That(info.PrimitiveCountUsed, Is.EqualTo(PrimitiveBudget));
    }

    [Test]
    public void DetermineBudgetExceededInfo_OnlyTriangleCountExceeded_ReturnsCorrectReason()
    {
        var (reason, info) = SectorSplitterOctree.DetermineBudgetExceededInfo(ByteSizeBudget, 100, 100, -100);

        Assert.That(reason, Is.EqualTo(SplitReason.BudgetTriangleCount));
        Assert.That(info.TriangleCountBudget, Is.EqualTo(TriangleBudget));
        Assert.That(info.TriangleCountUsed, Is.EqualTo(TriangleBudget + 100));
    }

    [Test]
    public void DetermineBudgetExceededInfo_MultipleBudgetsExceeded_ReturnsBudgetMultiple()
    {
        var (reason, _) = SectorSplitterOctree.DetermineBudgetExceededInfo(ByteSizeBudget, -100, 0, 100);

        Assert.That(reason, Is.EqualTo(SplitReason.BudgetMultiple));
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
