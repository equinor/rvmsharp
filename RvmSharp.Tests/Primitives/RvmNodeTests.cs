namespace RvmSharp.Tests.Primitives;

using NUnit.Framework;
using RvmSharp.Primitives;
using System.Linq;
using System.Numerics;

[TestFixture]
public class RvmNodeTests
{
    private RvmNode _rootNode;

    [SetUp]
    public void Setup()
    {
        _rootNode = new RvmNode(0, "root", Vector3.Zero, 0);
        var rvmNodeChild1 = new RvmNode(0, "c1", Vector3.Zero, 0);
        var rvmNodeChild2 = new RvmNode(0, "c2", Vector3.Zero, 0);

        var rvmNodeChild11 = new RvmNode(0, "c11", Vector3.Zero, 0);
        var rvmNodeChild12 = new RvmNode(0, "c12", Vector3.Zero, 0);
        var rvmNodeChild21 = new RvmNode(0, "c21", Vector3.Zero, 0);

        var rvmNodeChild121 = new RvmNode(0, "c121", Vector3.Zero, 0);

        var dummyBb = new RvmBoundingBox(Vector3.Zero, Vector3.Zero);

        var rvmBox = new RvmBox(0, Matrix4x4.Identity, dummyBb, 1f, 1f, 1f);
        var rvmCylinder1 = new RvmCylinder(0, Matrix4x4.Identity, dummyBb, 1f, 1f);
        var rvmCylinder2 = new RvmCylinder(0, Matrix4x4.Identity, dummyBb, 1f, 1f);

        rvmNodeChild11.Children.Add(rvmBox);
        rvmNodeChild11.Children.Add(rvmCylinder1);
        rvmNodeChild121.Children.Add(rvmCylinder2);

        rvmNodeChild12.Children.Add(rvmNodeChild121);
        rvmNodeChild1.Children.Add(rvmNodeChild11);
        rvmNodeChild1.Children.Add(rvmNodeChild12);
        rvmNodeChild2.Children.Add(rvmNodeChild21);
        _rootNode.Children.Add(rvmNodeChild1);
        _rootNode.Children.Add(rvmNodeChild2);
    }

    [Test]
    public void GetAllPrimitivesFlat_ReturnsAllPrimitives()
    {
        var primitives = RvmNode.GetAllPrimitivesFlat(_rootNode).ToArray();

        Assert.That(primitives, Has.Exactly(3).Items);
        Assert.That(primitives.OfType<RvmBox>(), Has.Exactly(1).Items);
        Assert.That(primitives.OfType<RvmCylinder>(), Has.Exactly(2).Items);
    }
}
