namespace CadRevealRvmProvider.Tests.Converters;

using CadRevealRvmProvider.Converters;
using RvmSharp.Operations;
using RvmSharp.Primitives;
using System.Numerics;
using VectorD = MathNet.Numerics.LinearAlgebra.Vector<double>;

[TestFixture]
public class RvmSnoutExtensionsTests
{
    [Test]
    public void RvmSnout_TestCapCalculation_Cone()
    {
        var snout = new RvmSnout(
            0,
            new Matrix4x4(),
            new RvmBoundingBox(new Vector3(), new Vector3()),
            2.0f, // bottom radius
            1.2928932188134525f, // top radius
            5.656854249492381f, // height
            0.0f, // offset x
            0.0f, // offset y
            MathF.PI / 4.0f, // bottom shear x
            MathF.PI / 4.0f, // bottom shear y
            0.0f, // top shear x
            -MathF.PI / 4.0f // top shear y
        );
        var topEllipse = snout.GetTopCapEllipse();

        Assert.That(topEllipse.ellipse2DPolar.semiMajorAxis, Is.EqualTo(1.857449777519938f));
        Assert.That(topEllipse.ellipse2DPolar.semiMinorAxis, Is.EqualTo(1.3031138776160802));
    }

    [Test]
    public void RvmSnout_TestCapCalculation_Cone_CappedInApex()
    {
        var snout = new RvmSnout(
            0,
            new Matrix4x4(),
            new RvmBoundingBox(new Vector3(), new Vector3()),
            76.0f, // bottom radius
            0.0f, // top radius
            90.0f, // height
            0.0f, // offset x
            0.0f, // offset y
            0.0f, // bottom shear x
            0.0f, // bottom shear y
            0.0f, // top shear x
            0.0f // top shear y
        );
        var topEllipse = snout.GetTopCapEllipse();

        Assert.That(topEllipse.ellipse2DPolar.semiMajorAxis, Is.EqualTo(0.0f));
        Assert.That(topEllipse.ellipse2DPolar.semiMinorAxis, Is.EqualTo(0.0f));
    }

    [Test]
    public void RvmSnout_TestCapCalculation_FlippedCone_ApexInOrigin()
    {
        var snout = new RvmSnout(
            0,
            new Matrix4x4(),
            new RvmBoundingBox(new Vector3(), new Vector3()),
            0.0f, // bottom radius
            155.5f, // top radius
            187.0f, // height
            0.0f, // offset x
            0.0f, // offset y
            0.0f, // bottom shear x
            0.0f, // bottom shear y
            0.0f, // top shear x
            0.0f // top shear y
        );
        var topEllipse = snout.GetTopCapEllipse();

        Assert.That(topEllipse.ellipse2DPolar.semiMajorAxis, Is.EqualTo(155.5));
        Assert.That(topEllipse.ellipse2DPolar.semiMinorAxis, Is.EqualTo(155.5));
    }

    [Test]
    public void RvmSnout_TestCapCalculation_Cone_Fzero()
    {
        var snout = new RvmSnout(
            0,
            new Matrix4x4(),
            new RvmBoundingBox(new Vector3(), new Vector3()),
            24.0f, // bottom radius
            16.5f, // top radius
            64.0f, // height
            7.5f, // offset x
            0.0f, // offset y
            0.0f, // bottom shear x
            0.0f, // bottom shear y
            0.0f, // top shear x
            0.0f // top shear y
        );
        var topEllipse = snout.GetTopCapEllipse();

        Assert.That(topEllipse.ellipse2DPolar.semiMajorAxis, Is.EqualTo(16.5));
        Assert.That(topEllipse.ellipse2DPolar.semiMinorAxis, Is.EqualTo(16.5));
    }

    [Test]
    public void RvmSnout_TestCapCalculation_Cylinder()
    {
        var snout = new RvmSnout(
            0,
            new Matrix4x4(),
            new RvmBoundingBox(new Vector3(), new Vector3()),
            2.0f, // bottom radius
            2.0f, // top radius
            4.0f, // height
            0.0f, // offset x
            0.0f, // offset y
            MathF.PI / 4.0f, // bottom shear x
            MathF.PI / 4.0f, // bottom shear y
            0.0f, // top shear x
            -MathF.PI / 4.0f // top shear y
        );
        var topEllipse = snout.GetTopCapEllipse();

        Assert.That(topEllipse.ellipse2DPolar.semiMajorAxis, Is.EqualTo(2.0 / MathF.Cos(snout.TopShearY)));
        Assert.That(topEllipse.ellipse2DPolar.semiMinorAxis, Is.EqualTo(2.0));
    }

    [Test]
    public void RvmSnout_TestCapCalculation_Origin_Cylinder()
    {
        var snout = new RvmSnout(
            0,
            new Matrix4x4(),
            new RvmBoundingBox(new Vector3(), new Vector3()),
            712.0f, // bottom radius
            712.0f, // top radius
            640.0f, // height
            0.0f, // offset x
            0.0f, // offset y
            0, // bottom shear x
            0, // bottom shear y
            0.0f, // top shear x
            0.17453292f // top shear y
        );
        var topEllipse = snout.GetTopCapEllipse();

        Assert.That(topEllipse.ellipse2DPolar.x0, Is.EqualTo(0.0));
        Assert.That(topEllipse.ellipse2DPolar.y0, Is.EqualTo(0.0));
    }

    //0.1 millimeters precision
    [Test]
    [DefaultFloatingPointTolerance(0.1)]
    public void RvmSnout_CylinderCap_InSamePlane()
    {
        var snout1 = new RvmSnout(
            1,
            new Matrix4x4(
                0.001f,
                0.0f,
                0.0f,
                0.0f,
                0.0f,
                0.0008660254f,
                -0.0005f,
                0.0f,
                0.0f,
                0.0005f,
                0.0008660254f,
                0.0f,
                74.95f,
                289.608978f,
                36.95f,
                1.0f
            ),
            new RvmBoundingBox(new Vector3(-500.0f, -500.0f, 160.0475f), new Vector3(500.0f, 500.0f, 695.9459f)),
            500, // bottom radius
            500.0f, // top radius
            535.8984f, // height
            0.0f, // offset x
            0.0f, // offset y
            0, // bottom shear x
            0.2617994f, // bottom shear y
            0.0f, // top shear x
            -0.2617994f // top shear y
        );
        var ellipse1 = snout1.GetBottomCapEllipse();

        var snout2 = new RvmSnout(
            1,
            new Matrix4x4(
                0.0f,
                0.001f,
                0.0f,
                0.0f,
                -0.001f,
                0.0f,
                0.0f,
                0.0f,
                0.0f,
                0.0f,
                0.001f,
                0.0f,
                74.95f,
                289.475f,
                36.5839729f,
                1.0f
            ),
            new RvmBoundingBox(new Vector3(-500.0f, -500.0f, -133.9746f), new Vector3(500.0f, 500.0f, 561.9713f)),
            500.0f, // bottom radius
            500.0f, // top radius
            267.9492f, // height
            0.0f, // offset x
            0.0f, // offset y
            0.0f, // bottom shear x
            0.0f, // bottom shear y
            -0.2617994f, // top shear x
            0.0f // top shear y
        );
        var ellipse2 = snout2.GetTopCapEllipse();

        var origin = VectorD.Build.Dense(new double[] { 0.0, 0.0, 0.0, 1.0 });
        var pt_orig_ell1 = ellipse1.planeToModelCoord * origin;
        var pt_orig_ell2 = ellipse2.planeToModelCoord * origin;

        // snout1 -> BOTTOM!!
        var snout1CapCenter = -0.5f * (new Vector3(snout1.OffsetX, snout1.OffsetY, snout1.Height));
        (var snout1_n, var snout1_dc) = GeometryHelper.GetPlaneFromShearAndPoint(
            snout1.BottomShearX,
            snout1.BottomShearY,
            snout1CapCenter
        );
        var snout1_n_4d = new Vector4(snout1_n.X, snout1_n.Y, snout1_n.Z, 0.0f);

        var distance1 =
            snout1_n.X * pt_orig_ell1[0] + snout1_n.Y * pt_orig_ell1[1] + snout1_n.Z * pt_orig_ell1[2] + snout1_dc;

        // snout2 -> bottom TOP!!
        var snout2CapCenter = 0.5f * (new Vector3(snout2.OffsetX, snout2.OffsetY, snout2.Height));
        (var snout2_n, var snout2_dc) = GeometryHelper.GetPlaneFromShearAndPoint(
            snout2.TopShearX,
            snout2.TopShearY,
            snout2CapCenter
        );
        var snout2_n_4d = new Vector4(snout2_n.X, snout2_n.Y, snout2_n.Z, 0.0f);

        var distance2 =
            snout2_n.X * pt_orig_ell2[0] + snout2_n.Y * pt_orig_ell2[1] + snout2_n.Z * pt_orig_ell2[2] + snout2_dc;

        var v4transfNormal1 = Vector4.Transform(snout1_n_4d, snout1.Matrix);
        var v4transfNormal2 = Vector4.Transform(snout2_n_4d, snout2.Matrix);
        var transfNormal1 = new Vector3(v4transfNormal1.X, v4transfNormal1.Y, v4transfNormal1.Z);
        var transfNormal2 = new Vector3(v4transfNormal2.X, v4transfNormal2.Y, v4transfNormal2.Z);
        transfNormal1 = Vector3.Normalize(transfNormal1);
        transfNormal2 = Vector3.Normalize(transfNormal2);

        Assert.AreEqual(Vector3.Dot(transfNormal1, transfNormal2), 1.0f, 0.001);

        // are the planes going through the same pt?
        // they are not for this snout!!

        Matrix4x4 s1Mat = (snout1.Matrix);
        Matrix4x4 s2Mat = (snout2.Matrix);
        Matrix4x4 sn2MatInv;
        Matrix4x4.Invert(s2Mat, out sn2MatInv);
        Matrix4x4 sn1MatInv;
        Matrix4x4.Invert(s1Mat, out sn1MatInv);

        var p1_w = Vector4.Transform(snout1CapCenter, s1Mat);
        var p1_m2 = Vector4.Transform(p1_w, sn2MatInv);
        var d_c1_pl2 = Vector3.Dot(snout2_n, new Vector3(p1_m2.X, p1_m2.Y, p1_m2.Z)) + snout2_dc;

        var p2 = Vector4.Transform(Vector4.Transform(snout2CapCenter, s2Mat), sn1MatInv);
        var d_c2_pl1 = Vector3.Dot(snout1_n, new Vector3(p2.X, p2.Y, p2.Z)) + snout1_dc;

        Assert.AreEqual(0.0, d_c1_pl2);
        Assert.AreEqual(0.0, d_c2_pl1);

        Assert.AreEqual(0.0, distance1 * 1000.0);
        Assert.AreEqual(0.0, distance2 * 1000.0);
    }
    
    [Test]
    [DefaultFloatingPointTolerance(0.1)]
    public void RvmSnout_TrivialConeCap_InSamePlane()
    {
        var snout1 = new RvmSnout(
            1,
            new Matrix4x4(
                0.0f,
                0.0f,
                -0.001f,
                0.0f,
                -0.000422618265f,
                -0.0009063078f,
                0.0f,
                0.0f,
                0.0f,
                -0.0009063078f,
                0.000422618265f,
                0.0f,
                112.532379f,
                297.9548f,
                25.335f,
                1.0f
            ),
            new RvmBoundingBox(new Vector3(-17.355f, -17.355f, -13.639999f), new Vector3(17.355f, 17.355f, 13.639999f)),
            17.355f, // bottom radius
            13.35f, // top radius
            27.2799988f, // height
            0.0f, // offset x
            0.0f, // offset y
            0.0f, // bottom shear x
            0.0f, // bottom shear y
            0.0f, // top shear x
            0.0f // top shear y
        );

        var snout2 = new RvmSnout(
            1,
            new Matrix4x4(
                0.0f,
                0.0f,
                -0.001f,
                0.0f,
                -0.000422618265f,
                -0.0009063078f,
                0.0f,
                0.0f,
                0.0f,
                -0.0009063078f,
                0.000422618265f,
                0.0f,
                112.532379f,
                297.9548f,
                25.335f,
                1.0f
            ),
            new RvmBoundingBox(new Vector3(-17.355f, -17.355f, -38.64f), new Vector3(17.355f, 17.355f, 38.64f)),
            13.35f, // bottom radius
            17.355f, // top radius
            77.28f, // height
            0.0f, // offset x
            0.0f, // offset y
            0.0f, // bottom shear x
            0.0f, // bottom shear y
            0.0f, // top shear x
            0.0f // top shear y
        );

        var ellipse1 = snout1.GetTopCapEllipse();
        var ellipse2 = snout2.GetBottomCapEllipse();

        var origin = VectorD.Build.Dense(new double[] { 0.0, 0.0, 0.0, 1.0 });
        var pt_orig_ell1 = ellipse1.planeToModelCoord * origin;
        var pt_orig_ell2 = ellipse2.planeToModelCoord * origin;

        // snout1 -> top
        var snout1CapCenter = 0.5f * (new Vector3(snout1.OffsetX, snout1.OffsetY, snout1.Height));
        (var snout1_n, var snout1_dc) = GeometryHelper.GetPlaneFromShearAndPoint(
            snout1.TopShearX,
            snout1.TopShearY,
            snout1CapCenter
        );
        var snout1_n_4d = new Vector4(snout1_n.X, snout1_n.Y, snout1_n.Z, 0.0f);

        var distance1 =
            snout1_n.X * pt_orig_ell1[0] + snout1_n.Y * pt_orig_ell1[1] + snout1_n.Z * pt_orig_ell1[2] + snout1_dc;

        // snout2 -> bottom
        var snout2CapCenter = -0.5f * (new Vector3(snout2.OffsetX, snout2.OffsetY, snout2.Height));
        (var snout2_n, var snout2_dc) = GeometryHelper.GetPlaneFromShearAndPoint(
            snout2.BottomShearX,
            snout2.BottomShearY,
            snout2CapCenter
        );
        var snout2_n_4d = new Vector4(snout2_n.X, snout2_n.Y, snout2_n.Z, 0.0f);

        var distance2 =
            snout2_n.X * pt_orig_ell2[0] + snout2_n.Y * pt_orig_ell2[1] + snout2_n.Z * pt_orig_ell2[2] + snout2_dc;

        var v4transfNormal1 = Vector4.Transform(snout1_n_4d, snout1.Matrix);
        var v4transfNormal2 = Vector4.Transform(snout2_n_4d, snout2.Matrix);
        var transfNormal1 = new Vector3(v4transfNormal1.X, v4transfNormal1.Y, v4transfNormal1.Z);
        var transfNormal2 = new Vector3(v4transfNormal2.X, v4transfNormal2.Y, v4transfNormal2.Z);
        transfNormal1 = Vector3.Normalize(transfNormal1);
        transfNormal2 = Vector3.Normalize(transfNormal2);

        Assert.AreEqual(Vector3.Dot(transfNormal1, transfNormal2), 1.0f, 0.001);

        Assert.AreEqual(0.0, distance1 * 1000.0);
        Assert.AreEqual(0.0, distance2 * 1000.0);
    }
    
    [Test]
    [DefaultFloatingPointTolerance(0.1)]
    public void RvmSnout_GeneralConeCap_InSamePlane()
    {
        var snout1 = new RvmSnout(
            1,
            new Matrix4x4(
                0.0f,
                0.0f,
                -0.001f,
                0.0f,
                -0.000422618265f,
                -0.0009063078f,
                0.0f,
                0.0f,
                0.0f,
                -0.0009063078f,
                0.000422618265f,
                0.0f,
                112.532379f,
                297.9548f,
                25.335f,
                1.0f
            ),
            new RvmBoundingBox(new Vector3(-17.355f, -17.355f, -13.639999f), new Vector3(17.355f, 17.355f, 13.639999f)),
            17.355f, // bottom radius
            13.35f, // top radius
            27.2799988f, // height
            10.0f, // offset x
            20.0f, // offset y
            0.0f, // bottom shear x
            0.0f, // bottom shear y
            -0.1f, // top shear x
            0.0f // top shear y
        );

        Matrix4x4 translate = Matrix4x4.CreateTranslation(snout1.OffsetX, snout1.OffsetY, 0.0f);
        var snout2 = new RvmSnout(
            1,
            new Matrix4x4(
                0.0f,
                0.0f,
                -0.001f,
                0.0f,
                -0.000422618265f,
                -0.0009063078f,
                0.0f,
                0.0f,
                0.0f,
                -0.0009063078f,
                0.000422618265f,
                0.0f,
                112.532379f,
                297.9548f,
                25.335f,
                1.0f
            ) * translate,
            new RvmBoundingBox(new Vector3(-17.355f, -17.355f, -38.64f), new Vector3(17.355f, 17.355f, 38.64f)),
            13.35f, // bottom radius
            17.355f, // top radius
            77.28f, // height
            15.0f, // offset x
            25.0f, // offset y
            -0.1f, // bottom shear x
            0.0f, // bottom shear y
            0.0f, // top shear x
            0.0f // top shear y
        );

        var ellipse1 = snout1.GetTopCapEllipse();
        var ellipse2 = snout2.GetBottomCapEllipse();

        var origin = VectorD.Build.Dense(new double[] { 0.0, 0.0, 0.0, 1.0 });
        var pt_orig_ell1 = ellipse1.planeToModelCoord * origin;
        var pt_orig_ell2 = ellipse2.planeToModelCoord * origin;

        // snout1 -> top
        var snout1CapCenter = 0.5f * (new Vector3(snout1.OffsetX, snout1.OffsetY, snout1.Height));
        (var snout1_n, var snout1_dc) = GeometryHelper.GetPlaneFromShearAndPoint(
            snout1.TopShearX,
            snout1.TopShearY,
            snout1CapCenter
        );
        var snout1_n_4d = new Vector4(snout1_n.X, snout1_n.Y, snout1_n.Z, 0.0f);

        var distance1 =
            snout1_n.X * pt_orig_ell1[0] + snout1_n.Y * pt_orig_ell1[1] + snout1_n.Z * pt_orig_ell1[2] + snout1_dc;

        // snout2 -> bottom
        var snout2CapCenter = -0.5f * (new Vector3(snout2.OffsetX, snout2.OffsetY, snout2.Height));
        (var snout2_n, var snout2_dc) = GeometryHelper.GetPlaneFromShearAndPoint(
            snout2.BottomShearX,
            snout2.BottomShearY,
            snout2CapCenter
        );
        var snout2_n_4d = new Vector4(snout2_n.X, snout2_n.Y, snout2_n.Z, 0.0f);

        var distance2 =
            snout2_n.X * pt_orig_ell2[0] + snout2_n.Y * pt_orig_ell2[1] + snout2_n.Z * pt_orig_ell2[2] + snout2_dc;

        var v4transfNormal1 = Vector4.Transform(snout1_n_4d, snout1.Matrix);
        var v4transfNormal2 = Vector4.Transform(snout2_n_4d, snout2.Matrix);
        var transfNormal1 = new Vector3(v4transfNormal1.X, v4transfNormal1.Y, v4transfNormal1.Z);
        var transfNormal2 = new Vector3(v4transfNormal2.X, v4transfNormal2.Y, v4transfNormal2.Z);
        transfNormal1 = Vector3.Normalize(transfNormal1);
        transfNormal2 = Vector3.Normalize(transfNormal2);

        Assert.AreEqual(Vector3.Dot(transfNormal1, transfNormal2), 1.0f, 0.001);

        Assert.AreEqual(0.0, distance1 * 1000.0);
        Assert.AreEqual(0.0, distance2 * 1000.0);
    }
}
