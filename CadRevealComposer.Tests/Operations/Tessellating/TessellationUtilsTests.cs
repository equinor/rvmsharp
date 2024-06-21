namespace CadRevealComposer.Tests.Operations.Tessellating;

using System.Numerics;
using CadRevealComposer.Operations.Tessellating;

[TestFixture]
public class TessellationUtilsTests
{
    [Test]
    public void AngleBetweenVectorsTest()
    {
        var unitX = Vector3.UnitX;
        var unitY = Vector3.UnitY;
        var unitZ = Vector3.UnitZ;

        Assert.That(TessellationUtils.AngleBetween(unitX, unitY), Is.EqualTo(MathF.PI / 2).Within(0.001f));
        Assert.That(TessellationUtils.AngleBetween(unitY, unitZ), Is.EqualTo(MathF.PI / 2).Within(0.001f));
        Assert.That(TessellationUtils.AngleBetween(unitX, unitZ), Is.EqualTo(MathF.PI / 2).Within(0.001f));

        Assert.That(TessellationUtils.AngleBetween(unitX, -unitX), Is.EqualTo(Math.PI).Within(0.001f));
        Assert.That(TessellationUtils.AngleBetween(unitX, unitX), Is.EqualTo(0).Within(0.001f));

        var rotation = Quaternion.CreateFromAxisAngle(unitZ, 0.3f);
        var rotatedVector = Vector3.Transform(unitX, rotation);

        Assert.That(TessellationUtils.AngleBetween(rotatedVector, unitX), Is.EqualTo(0.3f).Within(0.001f));
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

        Assert.That(TessellationUtils.AngleBetween(unitX, ortoX), Is.EqualTo(MathF.PI / 2).Within(0.001f));
        Assert.That(TessellationUtils.AngleBetween(unitY, ortoY), Is.EqualTo(MathF.PI / 2).Within(0.001f));
        Assert.That(TessellationUtils.AngleBetween(unitZ, ortoZ), Is.EqualTo(MathF.PI / 2).Within(0.001f));

        var v1 = new Vector3(1, 1, 1);
        var v2 = new Vector3(2, -2, 5);
        var v3 = new Vector3(23, 27, -111);

        var ortoV1 = TessellationUtils.CreateOrthogonalUnitVector(v1);
        var ortoV2 = TessellationUtils.CreateOrthogonalUnitVector(v2);
        var ortoV3 = TessellationUtils.CreateOrthogonalUnitVector(v3);

        Assert.That(TessellationUtils.AngleBetween(v1, ortoV1), Is.EqualTo(MathF.PI / 2).Within(0.001f));
        Assert.That(TessellationUtils.AngleBetween(v2, ortoV2), Is.EqualTo(MathF.PI / 2).Within(0.001f));
        Assert.That(TessellationUtils.AngleBetween(v3, ortoV3), Is.EqualTo(MathF.PI / 2).Within(0.001f));

        Assert.That(ortoV1.Length(), Is.EqualTo(Vector3.Normalize(ortoV1).Length()).Within(0.001f));
        Assert.That(ortoV2.Length(), Is.EqualTo(Vector3.Normalize(ortoV2).Length()).Within(0.001f));
        Assert.That(ortoV3.Length(), Is.EqualTo(Vector3.Normalize(ortoV3).Length()).Within(0.001f));
        Assert.That(ortoX.Length(), Is.EqualTo(Vector3.Normalize(ortoX).Length()).Within(0.001f));
        Assert.That(ortoY.Length(), Is.EqualTo(Vector3.Normalize(ortoY).Length()).Within(0.001f));
        Assert.That(ortoZ.Length(), Is.EqualTo(Vector3.Normalize(ortoZ).Length()).Within(0.001f));
    }
}
