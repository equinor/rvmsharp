namespace CadRevealComposer.Tests.Primitives.Converters;

using CadRevealComposer.Operations.Converters;
using CadRevealComposer.Primitives;
using NUnit.Framework;
using RvmSharp.Primitives;
using System.Drawing;
using System.Linq;
using System.Numerics;

[TestFixture]
public class RvmBoxConverterTests
{

    [Test]
    public void ConvertRvmBoxToBox()
    {
        const int treeIndex = 1337;

        var transform = Matrix4x4.Identity; // No rotation, scale 1, position at 0

        var rvmBox = new RvmBox(Version: 2,
            transform,
            new RvmBoundingBox(new Vector3(-1, -2, -3), new Vector3(1, 2, 3)),
            LengthX: 2, LengthY: 4, LengthZ: 6);
        var box = rvmBox.ConvertToRevealPrimitive(treeIndex, Color.Red).SingleOrDefault() as Box;

        Assert.That(box, Is.Not.Null);
        Assert.That(box, Is.TypeOf<Box>());
        Assert.That(box.TreeIndex, Is.EqualTo(treeIndex));
    }
}