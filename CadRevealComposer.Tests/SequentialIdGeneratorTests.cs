namespace CadRevealComposer.Tests;

using IdProviders;

[TestFixture]
public class SequentialIdGeneratorTests
{
    [TestCase(0u)]
    [TestCase(10u)]
    public void GetNodeId_ForFirstId_ReturnsSameAsStartingId(uint firstIdReturned)
    {
        // This test tests N random values. It should never fail.
        var sequentialIdGenerator = new SequentialIdGenerator(firstIdReturned);
        uint nextId = sequentialIdGenerator.GetNextId();
        Assert.That(nextId, Is.EqualTo(firstIdReturned));
        var expectedNextIdFromPeekNextBeforeGetNextId = sequentialIdGenerator.PeekNextId;
        var nextId2 = sequentialIdGenerator.GetNextId();
        Assert.That(nextId2, Is.EqualTo(firstIdReturned + 1));
        Assert.That(nextId2, Is.EqualTo(expectedNextIdFromPeekNextBeforeGetNextId));
    }
}
