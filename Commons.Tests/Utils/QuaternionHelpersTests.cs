namespace Commons.Tests.Utils;

using Commons.Utils;
using System.Numerics;

[TestFixture]
public class QuaternionHelpersTests
{
    [Test]
    public void QuaternionContainsInfiniteValue_ReturnsFalse_IfFiniteValues()
    {
        var quaternion = Quaternion.Identity;

        Assert.IsFalse(QuaternionHelpers.ContainsInfiniteValue(quaternion));
    }

    [Test]
    public void QuaternionContainsInfiniteValue_ReturnsTrue_IfInfiniteValues()
    {
        var quaternion = new Quaternion(0.4f, 0.6f, float.PositiveInfinity, 0.3f);

        Assert.IsTrue(QuaternionHelpers.ContainsInfiniteValue(quaternion));
    }
}
