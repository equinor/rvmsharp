namespace CadRevealComposer.Tests.Utils;

using CadRevealComposer.Utils;
using NUnit.Framework;
using System.Collections.Generic;
using System.Numerics;

[TestFixture]
class VectorExtensionsTests
{

    [Test]
    [TestCase(-1f, 2f, 3f)]
    public void Vector3_CopyToNewArray_ExpectedOrder(float x, float y, float z)
    {
        var vector = new Vector3(x, y, z);
        var array = vector.CopyToNewArray();

        Assert.That(array, Is.EqualTo(new[] {x, y, z}));
        Assert.That(array, Has.Exactly(3).Items);
    }

    [Test]
    [TestCase(-1f, 2f, 3f, 4f)]
    public void Vector4_CopyToNewArray_ExpectedOrder(float x, float y, float z, float w)
    {
        var vector = new Vector4(x, y, z, w);
        var array = vector.CopyToNewArray();

        Assert.That(array, Is.EqualTo(new[] {x, y, z, w}));
        Assert.That(array, Has.Exactly(4).Items);
    }

    [Test]
    [TestCase(new[] {0f, 0, 0}, ExpectedResult = true)]
    [TestCase(new[] {0f, 0, 1}, ExpectedResult = false)]
    [TestCase(new[] {1f, 0, 0}, ExpectedResult = false)]
    [TestCase(new[] {1f, 1, 0}, ExpectedResult = false)]
    [TestCase(new[] {0.00001f, 0.00002f, 0.00001f}, ExpectedResult = true)] // Tolerance is lower than this.
    public bool Vector3_IsUniform(float[] v) => new Vector3(v[0], v[1], v[2]).IsUniform(0.0001f);
        
        
    [Test]
    [TestCase(new[] {0f, 1, 2}, ExpectedResult = new[] {0f, 1, 2})]
    [TestCase(new[] {0.00001f, 0.00002f, 0.00001f}, ExpectedResult = new[] {0.00001f, 0.00002f, 0.00001f})] // Tolerance is lower than this.
    public IEnumerable<float> Vector3_AsEnumerable(float[] v) => new Vector3(v[0], v[1], v[2]).AsEnumerable();

    [Test]
    public void EqualsWithinTolerance()
    {
        Assert.IsTrue(Vector3.One.EqualsWithinTolerance(Vector3.One, 0.000_001f));
        Assert.IsFalse(Vector3.One.EqualsWithinTolerance(Vector3.Zero, 0.000_001f));
    }

    [Test]
    public void EqualsWithinFactor()
    {
        Assert.IsTrue(Vector3.One.EqualsWithinFactor(Vector3.One, 0f));
        Assert.IsFalse(Vector3.One.EqualsWithinFactor(Vector3.Zero, 0f));

        // dividing zero with zero leads to NaN which is a special case
        var almostZero = new Vector3(1E-20f, 1E-20f, 1E-20f);
        Assert.IsTrue(Vector3.Zero.EqualsWithinFactor(Vector3.Zero, 0f));
        Assert.IsTrue(almostZero.EqualsWithinFactor(Vector3.Zero, 0f));
    }
}