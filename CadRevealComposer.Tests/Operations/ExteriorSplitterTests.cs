namespace CadRevealComposer.Tests.Operations;

using CadRevealComposer.Operations;
using NUnit.Framework.Legacy;
using Primitives;
using System.Drawing;
using System.Numerics;

[TestFixture]
public class ExteriorSplitterTests
{
    [Test]
    public void ExteriorTest()
    {
        var exterior = CreateBoxCenteredInOrigin(1, 10);
        var interior = CreateBoxCenteredInOrigin(2, 5);
        var (exteriorList, interiorList) = ExteriorSplitter.Split(new APrimitive[] { interior, exterior });

        Assert.That(exteriorList.Length, Is.EqualTo(1));
        Assert.That(interiorList.Length, Is.EqualTo(1));
        Assert.That(exteriorList.Single(), Is.EqualTo(exterior));
        Assert.That(interiorList.Single(), Is.EqualTo(interior));
    }

    /// <summary>
    /// The exterior splitter uses axis aligned bounding box for Box primitive. All other data is irrelevant.
    /// </summary>
    private static Box CreateBoxCenteredInOrigin(ulong treeIndex, float boxSize)
    {
        return new Box(
            Matrix4x4.Identity,
            treeIndex,
            Color.Red,
            new BoundingBox(new Vector3(-boxSize / 2f), new Vector3(boxSize / 2f))
        );
    }
}
