namespace CadRevealProvider.Tests.Utils;

using CadRevealComposer.Utils;
using NUnit.Framework;
using System;

[TestFixture]
public class AlgebraUtilsTests
{
    [DefaultFloatingPointTolerance(0.01d)]
    [TestCase(MathF.PI * 2 + 2, ExpectedResult=2)]
    [TestCase(MathF.PI, ExpectedResult=-MathF.PI)]
    [TestCase(MathF.PI * 2, ExpectedResult=0)]
    [TestCase(float.NegativeInfinity, ExpectedResult=float.NaN)]
    [TestCase(float.NaN, ExpectedResult=float.NaN)]
    public float NormalizeRadiansTest(float r) => AlgebraUtils.NormalizeRadians(r);
}