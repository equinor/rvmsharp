namespace Commons.Tests;

using System;
using Commons.Utils;

[TestFixture]
public class SagittaUtilsTests
{
    [Test]
    public void SagittaBasedSegmentCount()
    {
        const float radius = 1;
        const int scale = 1;
        const float maximumSagitta = 1; // If the sagitta is equalTo the radius, we get four 90 degrees corners
        var res = SagittaUtils.SagittaBasedSegmentCount(Math.PI * 2, radius, scale, maximumSagitta);
        Assert.That(res, Is.EqualTo(4));
    }

    [Test]
    public void SagittaBasedSegmentCount_WhenScaled()
    {
        const float radius = 1f;
        const int scale = 2;
        const float maximumSagitta = radius;
        var res = SagittaUtils.SagittaBasedSegmentCount(Math.PI * 2, radius, scale, maximumSagitta);
        Assert.That(res, Is.EqualTo(6)); // We expect the circumference to double, but the segment count will not as the sagittaHeight is not scaled.
    }
}
