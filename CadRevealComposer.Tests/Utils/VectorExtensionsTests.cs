namespace CadRevealComposer.Tests.Utils
{
    using CadRevealComposer.Utils;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using System.Text;
    using System.Threading.Tasks;
    [TestFixture]
    class VectorExtensionsTests
    {

        [Test]
        [TestCase(-1f, 2f, 3f)]
        public void Vector3_CopyToNewArray_ExpectedOrder(float x, float y, float z)
        {
            var vector = new Vector3(x, y, z);
            var array = vector.CopyToNewArray();

            Assert.That(array, Is.EqualTo(new[] { x, y, z }));
            Assert.That(array, Has.Exactly(3).Items);
        }

        [Test]
        [TestCase(-1f, 2f, 3f, 4f)]
        public void Vector4_CopyToNewArray_ExpectedOrder(float x, float y, float z, float w)
        {
            var vector = new Vector4(x, y, z, w);
            var array = vector.CopyToNewArray();

            Assert.That(array, Is.EqualTo(new[] { x, y, z, w }));
            Assert.That(array, Has.Exactly(4).Items);
        }
    }
}
