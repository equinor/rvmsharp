using NUnit.Framework;

namespace RvmSharp.Tests.Tesselator
{
    using Primitives;
    using rvmsharp.Tessellator;
    using System.Numerics;

    [TestFixture]
    public class TesselatorBridgeTests
    {
        [TestFixture]
        public class TesselateBoxTests
        {
            [Test]
            public void TesselateBox_WithUnitBox_ReturnsExpected1x1Mesh()
            {
                var unitBox = new RvmBox(1, Matrix4x4.Identity, new RvmBoundingBox() {Max = new Vector3(0.5f, 0.5f, 0.5f), Min = new Vector3(-0.5f, -0.5f, -0.5f)}, 1, 1, 1);
                
                var box = TessellatorBridge.TessellateBox(unitBox, 1);
                Assert.That(box.Vertices, Has.Exactly(24).Items);
                Assert.That(box, Is.Not.Null);
            }
        }
    }
}