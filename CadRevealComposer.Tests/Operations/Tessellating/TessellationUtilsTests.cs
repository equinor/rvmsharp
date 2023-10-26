namespace CadRevealComposer.Tests.Operations.Tessellating;

using CadRevealComposer.Operations.Tessellating;
using System;
using System.Numerics;

[TestFixture]
public class TessellationUtilsTests
{
    [Test]
    public void AngleBetweenVectorsTest()
    {
        var unitX = Vector3.UnitX;
        var unitY = Vector3.UnitY;
        var unitZ = Vector3.UnitZ;

        Assert.AreEqual(TessellationUtils.AngleBetween(unitX, unitY), MathF.PI / 2, 0.001f);
        Assert.AreEqual(TessellationUtils.AngleBetween(unitY, unitZ), MathF.PI / 2, 0.001f);
        Assert.AreEqual(TessellationUtils.AngleBetween(unitX, unitZ), MathF.PI / 2, 0.001f);

        Assert.AreEqual(TessellationUtils.AngleBetween(unitX, -unitX), Math.PI, 0.001f);
        Assert.AreEqual(TessellationUtils.AngleBetween(unitX, unitX), 0, 0.001f);

        var rotation = Quaternion.CreateFromAxisAngle(unitZ, 0.3f);
        var rotatedVector = Vector3.Transform(unitX, rotation);

        Assert.AreEqual(TessellationUtils.AngleBetween(rotatedVector, unitX), 0.3f, 0.001);
    }

    [Test]
    public void CreateOrthogonalVectorTest()
    {
        var unitX = Vector3.UnitX;
        var unitY = Vector3.UnitY;
        var unitZ = Vector3.UnitZ;

        var ortoX = TessellationUtils.CreateOrthogonalUnitVector(unitX);
        var ortoY = TessellationUtils.CreateOrthogonalUnitVector(unitY);
        var ortoZ = TessellationUtils.CreateOrthogonalUnitVector(unitZ);

        Assert.AreEqual(TessellationUtils.AngleBetween(unitX, ortoX), MathF.PI / 2, 0.001f);
        Assert.AreEqual(TessellationUtils.AngleBetween(unitY, ortoY), MathF.PI / 2, 0.001f);
        Assert.AreEqual(TessellationUtils.AngleBetween(unitZ, ortoZ), MathF.PI / 2, 0.001f);

        var v1 = new Vector3(1, 1, 1);
        var v2 = new Vector3(2, -2, 5);
        var v3 = new Vector3(23, 27, -111);

        var ortoV1 = TessellationUtils.CreateOrthogonalUnitVector(v1);
        var ortoV2 = TessellationUtils.CreateOrthogonalUnitVector(v2);
        var ortoV3 = TessellationUtils.CreateOrthogonalUnitVector(v3);

        Assert.AreEqual(TessellationUtils.AngleBetween(v1, ortoV1), MathF.PI / 2, 0.001f);
        Assert.AreEqual(TessellationUtils.AngleBetween(v2, ortoV2), MathF.PI / 2, 0.001f);
        Assert.AreEqual(TessellationUtils.AngleBetween(v3, ortoV3), MathF.PI / 2, 0.001f);

        Assert.AreEqual(ortoV1.Length(), Vector3.Normalize(ortoV1).Length(), 0.001f);
        Assert.AreEqual(ortoV2.Length(), Vector3.Normalize(ortoV2).Length(), 0.001f);
        Assert.AreEqual(ortoV3.Length(), Vector3.Normalize(ortoV3).Length(), 0.001f);
        Assert.AreEqual(ortoX.Length(), Vector3.Normalize(ortoX).Length(), 0.001f);
        Assert.AreEqual(ortoY.Length(), Vector3.Normalize(ortoY).Length(), 0.001f);
        Assert.AreEqual(ortoZ.Length(), Vector3.Normalize(ortoZ).Length(), 0.001f);
    }
}
