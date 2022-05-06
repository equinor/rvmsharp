namespace CadRevealComposer.Writers;

using Primitives;
using SharpGLTF.Schema2;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public static class GltfWriter
{
    public static void WriteSector(APrimitive[] /* do NOT replace with IEnumerable */ primitives, Stream stream)
    {
        var model = ModelRoot.CreateModel();
        var scene = model.UseScene(null);
        model.DefaultScene = scene;

        var boxes = primitives.OfType<Box>().ToArray();
        if (boxes.Length > 0)
        {
            WriteBoxes(boxes, model, scene);
        }

        var circles = primitives.OfType<Circle>().ToArray();
        if (circles.Length > 0)
        {
            WriteCircles(circles, model, scene);
        }

        var cones = primitives.OfType<Cone>().ToArray();
        if (cones.Length > 0)
        {
            WriteCones(cones, model, scene);
        }

        var eccentricCones = primitives.OfType<EccentricCone>().ToArray();
        if (eccentricCones.Length > 0)
        {
            WriteEccentricCones(eccentricCones, model, scene);
        }

        var ellipsoids = primitives.OfType<Ellipsoid>().ToArray();
        if (ellipsoids.Length > 0)
        {
            WriteEllipsoids(ellipsoids, model, scene);
        }

        var generalCylinders = primitives.OfType<GeneralCylinder>().ToArray();
        if (generalCylinders.Length > 0)
        {
            WriteGeneralCylinders(generalCylinders, model, scene);
        }

        var generalRings = primitives.OfType<GeneralRing>().ToArray();
        if (generalRings.Length > 0)
        {
            WriteGeneralRings(generalRings, model, scene);
        }

        var nuts = primitives.OfType<Nut>().ToArray();
        if (nuts.Length > 0)
        {
            WriteNuts(nuts, model, scene);
        }

        var quads = primitives.OfType<Quad>().ToArray();
        if (quads.Length > 0)
        {
            WriteQuads(quads, model, scene);
        }

        var torus = primitives.OfType<Torus>().ToArray();
        if (torus.Length > 0)
        {
            WriteTorus(torus, model, scene);
        }

        var trapeziums = primitives.OfType<Trapezium>().ToArray();
        if (trapeziums.Length > 0)
        {
            WriteTrapeziums(trapeziums, model, scene);
        }

        model.WriteGLB(stream);
    }

    private static void WriteBoxes(Box[] boxes, ModelRoot model, Scene scene)
    {
        var boxCount = boxes.Length;

        // create byte buffer
        const int byteStride = (1 + 1 + 16) * sizeof(float); // id + color + matrix
        var bufferView = model.CreateBufferView(byteStride * boxCount, byteStride);
        var buffer = bufferView.Content.Array!;
        var bufferPos = 0;
        foreach (var box in boxes)
        {
            var treeIndex = (float)box.TreeIndex;
            buffer.Write(treeIndex, ref bufferPos);
            buffer.Write(box.Color, ref bufferPos);
            buffer.Write(box.InstanceMatrix, ref bufferPos);
        }

        var node = scene.CreateNode("BoxCollection");
        var meshGpuInstancing = node.UseExtension<MeshGpuInstancing>();

        var treeIndexAccessor = model.CreateAccessor();
        var colorAccessor = model.CreateAccessor();
        var instanceMatrixAccessor = model.CreateAccessor();

        meshGpuInstancing.SetAccessor("_treeIndex", treeIndexAccessor);
        meshGpuInstancing.SetAccessor("_color", colorAccessor);
        meshGpuInstancing.SetAccessor("_instanceMatrix", instanceMatrixAccessor);

        treeIndexAccessor.SetData(bufferView, 0, boxCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        colorAccessor.SetData(bufferView, 4, boxCount, DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, false);
        instanceMatrixAccessor.SetData(bufferView, 8, boxCount, DimensionType.MAT4, EncodingType.FLOAT, false);
    }

    private static void WriteCircles(Circle[] circles, ModelRoot model, Scene scene)
    {
        var circleCount = circles.Length;

        var treeIndexAccessor = model.CreateAccessor();
        var colorAccessor = model.CreateAccessor();
        var instanceMatrixAccessor = model.CreateAccessor();
        var normalAccessor = model.CreateAccessor();

        var node = scene.CreateNode("CircleCollection");
        var meshGpuInstancing = node.UseExtension<MeshGpuInstancing>();
        meshGpuInstancing.SetAccessor("_treeIndex", treeIndexAccessor);
        meshGpuInstancing.SetAccessor("_color", colorAccessor);
        meshGpuInstancing.SetAccessor("_instanceMatrix", instanceMatrixAccessor);
        meshGpuInstancing.SetAccessor("_normal", normalAccessor);

        // create byte buffer
        const int byteStride = (1 + 1 + 16 + 3) * sizeof(float); // id + color + matrix + normal
        var bufferView = model.CreateBufferView(byteStride * circleCount, byteStride);
        var buffer = bufferView.Content.Array!;
        var bufferPos = 0;
        foreach (var circle in circles)
        {
            var treeIndex = (float)circle.TreeIndex;
            buffer.Write(treeIndex, ref bufferPos);
            buffer.Write(circle.Color, ref bufferPos);
            buffer.Write(circle.InstanceMatrix, ref bufferPos);
            buffer.Write(circle.Normal, ref bufferPos);
        }

        treeIndexAccessor.SetData(bufferView, 0, circleCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        colorAccessor.SetData(bufferView, 4, circleCount, DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, false);
        instanceMatrixAccessor.SetData(bufferView, 8, circleCount, DimensionType.MAT4, EncodingType.FLOAT, false);
        normalAccessor.SetData(bufferView, 72, circleCount, DimensionType.VEC3, EncodingType.FLOAT, false);
    }

    private static void WriteCones(Cone[] cones, ModelRoot model, Scene scene)
    {
        var coneCount = cones.Length;

        var treeIndexAccessor = model.CreateAccessor();
        var colorAccessor = model.CreateAccessor();
        var angleAccessor = model.CreateAccessor();
        var arcAngleAccessor = model.CreateAccessor();
        var centerAAccessor = model.CreateAccessor();
        var centerBAccessor = model.CreateAccessor();
        var localXAxisAccessor = model.CreateAccessor();
        var radiusAAccessor = model.CreateAccessor();
        var radiusBAccessor = model.CreateAccessor();

        var node = scene.CreateNode("ConeCollection");
        var meshGpuInstancing = node.UseExtension<MeshGpuInstancing>();
        meshGpuInstancing.SetAccessor("_treeIndex", treeIndexAccessor);
        meshGpuInstancing.SetAccessor("_color", colorAccessor);
        meshGpuInstancing.SetAccessor("_angle", angleAccessor);
        meshGpuInstancing.SetAccessor("_arcAngle", arcAngleAccessor);
        meshGpuInstancing.SetAccessor("_centerA", centerAAccessor);
        meshGpuInstancing.SetAccessor("_centerB", centerBAccessor);
        meshGpuInstancing.SetAccessor("_localXAxis", localXAxisAccessor);
        meshGpuInstancing.SetAccessor("_radiusA", radiusAAccessor);
        meshGpuInstancing.SetAccessor("_radiusB", radiusBAccessor);

        // create byte buffer
        const int byteStride = (1 + 1 + 1 + 1 + 3 + 3 + 3 + 1 + 1) * sizeof(float);
        var bufferView = model.CreateBufferView(byteStride * coneCount, byteStride);
        var buffer = bufferView.Content.Array!;
        var bufferPos = 0;
        foreach (var cone in cones)
        {
            var treeIndex = (float)cone.TreeIndex;
            buffer.Write(treeIndex, ref bufferPos);
            buffer.Write(cone.Color, ref bufferPos);
            buffer.Write(cone.Angle, ref bufferPos);
            buffer.Write(cone.ArcAngle, ref bufferPos);
            buffer.Write(cone.CenterA, ref bufferPos);
            buffer.Write(cone.CenterB, ref bufferPos);
            buffer.Write(cone.LocalXAxis, ref bufferPos);
            buffer.Write(cone.RadiusA, ref bufferPos);
            buffer.Write(cone.RadiusB, ref bufferPos);
        }

        treeIndexAccessor.SetData(bufferView, 0, coneCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        colorAccessor.SetData(bufferView, 4, coneCount, DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, false);
        angleAccessor.SetData(bufferView, 8, coneCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        arcAngleAccessor.SetData(bufferView, 12, coneCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        centerAAccessor.SetData(bufferView, 16, coneCount, DimensionType.VEC3, EncodingType.FLOAT, false);
        centerBAccessor.SetData(bufferView, 28, coneCount, DimensionType.VEC3, EncodingType.FLOAT, false);
        localXAxisAccessor.SetData(bufferView, 40, coneCount, DimensionType.VEC3, EncodingType.FLOAT, false);
        radiusAAccessor.SetData(bufferView, 52, coneCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        radiusBAccessor.SetData(bufferView, 56, coneCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
    }

    private static void WriteEccentricCones(EccentricCone[] eccentricCones, ModelRoot model, Scene scene)
    {
        var eccentricConeCount = eccentricCones.Length;

        var treeIndexAccessor = model.CreateAccessor();
        var colorAccessor = model.CreateAccessor();
        var normalAccessor = model.CreateAccessor();
        var radiusAAccessor = model.CreateAccessor();
        var radiusBAccessor = model.CreateAccessor();

        var node = scene.CreateNode("EccentricConeCollection");
        var meshGpuInstancing = node.UseExtension<MeshGpuInstancing>();
        meshGpuInstancing.SetAccessor("_treeIndex", treeIndexAccessor);
        meshGpuInstancing.SetAccessor("_color", colorAccessor);
        meshGpuInstancing.SetAccessor("_normal", normalAccessor);
        meshGpuInstancing.SetAccessor("_radiusA", radiusAAccessor);
        meshGpuInstancing.SetAccessor("_radiusB", radiusBAccessor);

        // create byte buffer
        const int byteStride = (1 + 1 + 3 + 1 + 1) * sizeof(float);
        var bufferView = model.CreateBufferView(byteStride * eccentricConeCount, byteStride);
        var buffer = bufferView.Content.Array!;
        var bufferPos = 0;
        foreach (var eccentricCone in eccentricCones)
        {
            var treeIndex = (float)eccentricCone.TreeIndex;
            buffer.Write(treeIndex, ref bufferPos);
            buffer.Write(eccentricCone.Color, ref bufferPos);
            buffer.Write(eccentricCone.Normal, ref bufferPos);
            buffer.Write(eccentricCone.RadiusA, ref bufferPos);
            buffer.Write(eccentricCone.RadiusB, ref bufferPos);
        }

        treeIndexAccessor.SetData(bufferView, 0, eccentricConeCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        colorAccessor.SetData(bufferView, 4, eccentricConeCount, DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, false);
        normalAccessor.SetData(bufferView, 8, eccentricConeCount, DimensionType.VEC3, EncodingType.FLOAT, false);
        radiusAAccessor.SetData(bufferView, 20, eccentricConeCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        radiusBAccessor.SetData(bufferView, 24, eccentricConeCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
    }

    private static void WriteEllipsoids(Ellipsoid[] ellipsoids, ModelRoot model, Scene scene)
    {
        var ellipsoidCount = ellipsoids.Length;

        var treeIndexAccessor = model.CreateAccessor();
        var colorAccessor = model.CreateAccessor();
        var horizontalRadiusAccessor = model.CreateAccessor();
        var verticalRadiusAccessor = model.CreateAccessor();
        var heightAccessor = model.CreateAccessor();
        var centerAccessor = model.CreateAccessor();

        var node = scene.CreateNode("EllipsoidSegmentCollection");
        var meshGpuInstancing = node.UseExtension<MeshGpuInstancing>();
        meshGpuInstancing.SetAccessor("_treeIndex", treeIndexAccessor);
        meshGpuInstancing.SetAccessor("_color", colorAccessor);
        meshGpuInstancing.SetAccessor("_horizontalRadius", horizontalRadiusAccessor);
        meshGpuInstancing.SetAccessor("_verticalRadius", verticalRadiusAccessor);
        meshGpuInstancing.SetAccessor("_height", heightAccessor);
        meshGpuInstancing.SetAccessor("_center", centerAccessor);

        // create byte buffer
        const int byteStride = (1 + 1 + 1 + 1 + 1 + 3) * sizeof(float); // id + color + matrix
        var bufferView = model.CreateBufferView(byteStride * ellipsoidCount, byteStride);
        var buffer = bufferView.Content.Array!;
        var bufferPos = 0;
        foreach (var ellipsoid in ellipsoids)
        {
            var treeIndex = (float)ellipsoid.TreeIndex;
            buffer.Write(treeIndex, ref bufferPos);
            buffer.Write(ellipsoid.Color, ref bufferPos);
            buffer.Write(ellipsoid.HorizontalRadius, ref bufferPos);
            buffer.Write(ellipsoid.VerticalRadius, ref bufferPos);
            buffer.Write(ellipsoid.Height, ref bufferPos);
            buffer.Write(ellipsoid.Center, ref bufferPos);
        }

        treeIndexAccessor.SetData(bufferView, 0, ellipsoidCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        colorAccessor.SetData(bufferView, 4, ellipsoidCount, DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, false);
        horizontalRadiusAccessor.SetData(bufferView, 8, ellipsoidCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        verticalRadiusAccessor.SetData(bufferView, 12, ellipsoidCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        heightAccessor.SetData(bufferView, 16, ellipsoidCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        centerAccessor.SetData(bufferView, 20, ellipsoidCount, DimensionType.VEC3, EncodingType.FLOAT, false);
    }

    private static void WriteGeneralCylinders(GeneralCylinder[] generalCylinders, ModelRoot model, Scene scene)
    {
        var generalCylinderCount = generalCylinders.Length;

        var treeIndexAccessor = model.CreateAccessor();
        var colorAccessor = model.CreateAccessor();
        var angleAccessor = model.CreateAccessor();
        var arcAngleAccessor = model.CreateAccessor();
        var centerAAccessor = model.CreateAccessor();
        var centerBAccessor = model.CreateAccessor();
        var localXAxisAccessor = model.CreateAccessor();
        var planeAAccessor = model.CreateAccessor();
        var planeBAccessor = model.CreateAccessor();
        var radiusAccessor = model.CreateAccessor();

        var node = scene.CreateNode("GeneralCylinderCollection");
        var meshGpuInstancing = node.UseExtension<MeshGpuInstancing>();
        meshGpuInstancing.SetAccessor("_treeIndex", treeIndexAccessor);
        meshGpuInstancing.SetAccessor("_color", colorAccessor);
        meshGpuInstancing.SetAccessor("_angle", angleAccessor);
        meshGpuInstancing.SetAccessor("_arcAngle", arcAngleAccessor);
        meshGpuInstancing.SetAccessor("_centerA", centerAAccessor);
        meshGpuInstancing.SetAccessor("_centerB", centerBAccessor);
        meshGpuInstancing.SetAccessor("_localXAxis", localXAxisAccessor);
        meshGpuInstancing.SetAccessor("_planeA", planeAAccessor);
        meshGpuInstancing.SetAccessor("_planeA", planeBAccessor);
        meshGpuInstancing.SetAccessor("_radius", radiusAccessor);

        // create byte buffer
        const int byteStride = (1 + 1 + 1 + 1 + 3 + 3 + 3 + 4 + 4 + 1) * sizeof(float);
        var bufferView = model.CreateBufferView(byteStride * generalCylinderCount, byteStride);
        var buffer = bufferView.Content.Array!;
        var bufferPos = 0;
        foreach (var generalCylinder in generalCylinders)
        {
            var treeIndex = (float)generalCylinder.TreeIndex;
            buffer.Write(treeIndex, ref bufferPos);
            buffer.Write(generalCylinder.Color, ref bufferPos);
            buffer.Write(generalCylinder.Angle, ref bufferPos);
            buffer.Write(generalCylinder.ArcAngle, ref bufferPos);
            buffer.Write(generalCylinder.CenterA, ref bufferPos);
            buffer.Write(generalCylinder.CenterB, ref bufferPos);
            buffer.Write(generalCylinder.LocalXAxis, ref bufferPos);
            buffer.Write(generalCylinder.PlaneA, ref bufferPos);
            buffer.Write(generalCylinder.PlaneB, ref bufferPos);
            buffer.Write(generalCylinder.Radius, ref bufferPos);
        }

        treeIndexAccessor.SetData(bufferView, 0, generalCylinderCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        colorAccessor.SetData(bufferView, 4, generalCylinderCount, DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, false);
        angleAccessor.SetData(bufferView, 8, generalCylinderCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        arcAngleAccessor.SetData(bufferView, 12, generalCylinderCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        centerAAccessor.SetData(bufferView, 16, generalCylinderCount, DimensionType.VEC3, EncodingType.FLOAT, false);
        centerBAccessor.SetData(bufferView, 28, generalCylinderCount, DimensionType.VEC3, EncodingType.FLOAT, false);
        localXAxisAccessor.SetData(bufferView, 40, generalCylinderCount, DimensionType.VEC3, EncodingType.FLOAT, false);
        planeAAccessor.SetData(bufferView, 52, generalCylinderCount, DimensionType.VEC4, EncodingType.FLOAT, false);
        planeBAccessor.SetData(bufferView, 68, generalCylinderCount, DimensionType.VEC4, EncodingType.FLOAT, false);
        radiusAccessor.SetData(bufferView, 84, generalCylinderCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
    }

    private static void WriteGeneralRings(GeneralRing[] generalRings, ModelRoot model, Scene scene)
    {
        var generalRingCount = generalRings.Length;

        var treeIndexAccessor = model.CreateAccessor();
        var colorAccessor = model.CreateAccessor();
        var angleAccessor = model.CreateAccessor();
        var arcAngleAccessor = model.CreateAccessor();
        var instanceMatrixAccessor = model.CreateAccessor();
        var normalAccessor = model.CreateAccessor();
        var thicknessAccessor = model.CreateAccessor();

        var node = scene.CreateNode("GeneralRingCollection");
        var meshGpuInstancing = node.UseExtension<MeshGpuInstancing>();
        meshGpuInstancing.SetAccessor("_treeIndex", treeIndexAccessor);
        meshGpuInstancing.SetAccessor("_color", colorAccessor);
        meshGpuInstancing.SetAccessor("_angle", angleAccessor);
        meshGpuInstancing.SetAccessor("_arcAngle", arcAngleAccessor);
        meshGpuInstancing.SetAccessor("_instanceMatrix", instanceMatrixAccessor);
        meshGpuInstancing.SetAccessor("_normal", normalAccessor);
        meshGpuInstancing.SetAccessor("_thickness", thicknessAccessor);

        // create byte buffer
        const int byteStride = (1 + 1 + 1 + 1 + 16 + 3 + 1) * sizeof(float);
        var bufferView = model.CreateBufferView(byteStride * generalRingCount, byteStride);
        var buffer = bufferView.Content.Array!;
        var bufferPos = 0;
        foreach (var generalRing in generalRings)
        {
            var treeIndex = (float)generalRing.TreeIndex;
            buffer.Write(treeIndex, ref bufferPos);
            buffer.Write(generalRing.Color, ref bufferPos);
            buffer.Write(generalRing.Angle, ref bufferPos);
            buffer.Write(generalRing.ArcAngle, ref bufferPos);
            buffer.Write(generalRing.InstanceMatrix, ref bufferPos);
            buffer.Write(generalRing.Normal, ref bufferPos);
            buffer.Write(generalRing.Thickness, ref bufferPos);
        }

        treeIndexAccessor.SetData(bufferView, 0, generalRingCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        colorAccessor.SetData(bufferView, 4, generalRingCount, DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, false);
        angleAccessor.SetData(bufferView, 8, generalRingCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        arcAngleAccessor.SetData(bufferView, 12, generalRingCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        instanceMatrixAccessor.SetData(bufferView, 16, generalRingCount, DimensionType.MAT4, EncodingType.FLOAT, false);
        normalAccessor.SetData(bufferView, 80, generalRingCount, DimensionType.VEC3, EncodingType.FLOAT, false);
        thicknessAccessor.SetData(bufferView, 92, generalRingCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
    }

    private static void WriteNuts(Nut[] nuts, ModelRoot model, Scene scene)
    {
        var nutCount = nuts.Length;

        var treeIndexAccessor = model.CreateAccessor();
        var colorAccessor = model.CreateAccessor();
        var instanceMatrixAccessor = model.CreateAccessor();

        var node = scene.CreateNode("NutCollection");
        var meshGpuInstancing = node.UseExtension<MeshGpuInstancing>();
        meshGpuInstancing.SetAccessor("_treeIndex", treeIndexAccessor);
        meshGpuInstancing.SetAccessor("_color", colorAccessor);
        meshGpuInstancing.SetAccessor("_instanceMatrix", instanceMatrixAccessor);

        // create byte buffer
        const int byteStride = (1 + 1 + 16) * sizeof(float); // id + color + matrix
        var bufferView = model.CreateBufferView(byteStride * nutCount, byteStride);
        var buffer = bufferView.Content.Array!;
        var bufferPos = 0;
        foreach (var nut in nuts)
        {
            var treeIndex = (float)nut.TreeIndex;
            buffer.Write(treeIndex, ref bufferPos);
            buffer.Write(nut.Color, ref bufferPos);
            buffer.Write(nut.InstanceMatrix, ref bufferPos);
        }

        treeIndexAccessor.SetData(bufferView, 0, nutCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        colorAccessor.SetData(bufferView, 4, nutCount, DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, false);
        instanceMatrixAccessor.SetData(bufferView, 8, nutCount, DimensionType.MAT4, EncodingType.FLOAT, false);
    }

    private static void WriteQuads(Quad[] quads, ModelRoot model, Scene scene)
    {
        var quadCount = quads.Length;

        var treeIndexAccessor = model.CreateAccessor();
        var colorAccessor = model.CreateAccessor();
        var instanceMatrixAccessor = model.CreateAccessor();

        var node = scene.CreateNode("QuadCollection");
        var meshGpuInstancing = node.UseExtension<MeshGpuInstancing>();
        meshGpuInstancing.SetAccessor("_treeIndex", treeIndexAccessor);
        meshGpuInstancing.SetAccessor("_color", colorAccessor);
        meshGpuInstancing.SetAccessor("_instanceMatrix", instanceMatrixAccessor);

        // create byte buffer
        const int byteStride = (1 + 1 + 16) * sizeof(float); // id + color + matrix
        var bufferView = model.CreateBufferView(byteStride * quadCount, byteStride);
        var buffer = bufferView.Content.Array!;
        var bufferPos = 0;
        foreach (var quad in quads)
        {
            var treeIndex = (float)quad.TreeIndex;
            buffer.Write(treeIndex, ref bufferPos);
            buffer.Write(quad.Color, ref bufferPos);
            buffer.Write(quad.InstanceMatrix, ref bufferPos);
        }

        treeIndexAccessor.SetData(bufferView, 0, quadCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        colorAccessor.SetData(bufferView, 4, quadCount, DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, false);
        instanceMatrixAccessor.SetData(bufferView, 8, quadCount, DimensionType.MAT4, EncodingType.FLOAT, false);
    }

    private static void WriteTorus(Torus[] torus, ModelRoot model, Scene scene)
    {
        var torusCount = torus.Length;

        var treeIndexAccessor = model.CreateAccessor();
        var colorAccessor = model.CreateAccessor();
        var arcAngleAccessor = model.CreateAccessor();
        var instanceMatrixAccessor = model.CreateAccessor();
        var radiusAccessor = model.CreateAccessor();
        var tubeRadiusAccessor = model.CreateAccessor();

        var node = scene.CreateNode("TorusSegmentCollection");
        var meshGpuInstancing = node.UseExtension<MeshGpuInstancing>();
        meshGpuInstancing.SetAccessor("_treeIndex", treeIndexAccessor);
        meshGpuInstancing.SetAccessor("_color", colorAccessor);
        meshGpuInstancing.SetAccessor("_arcAngle", arcAngleAccessor);
        meshGpuInstancing.SetAccessor("_instanceMatrix", instanceMatrixAccessor);
        meshGpuInstancing.SetAccessor("_radius", radiusAccessor);
        meshGpuInstancing.SetAccessor("_tubeRadius", tubeRadiusAccessor);

        // create byte buffer
        const int byteStride = (1 + 1 + 1 + 16 + 1 + 1) * sizeof(float);
        var bufferView = model.CreateBufferView(byteStride * torusCount, byteStride);
        var buffer = bufferView.Content.Array!;
        var bufferPos = 0;
        foreach (var tor in torus)
        {
            var treeIndex = (float)tor.TreeIndex;
            buffer.Write(treeIndex, ref bufferPos);
            buffer.Write(tor.Color, ref bufferPos);
            buffer.Write(tor.ArcAngle, ref bufferPos);
            buffer.Write(tor.InstanceMatrix, ref bufferPos);
            buffer.Write(tor.Radius, ref bufferPos);
            buffer.Write(tor.TubeRadius, ref bufferPos);
        }

        treeIndexAccessor.SetData(bufferView, 0, torusCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        colorAccessor.SetData(bufferView, 4, torusCount, DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, false);
        arcAngleAccessor.SetData(bufferView, 8, torusCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        instanceMatrixAccessor.SetData(bufferView, 12, torusCount, DimensionType.MAT4, EncodingType.FLOAT, false);
        radiusAccessor.SetData(bufferView, 76, torusCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        tubeRadiusAccessor.SetData(bufferView, 80, torusCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
    }

    private static void WriteTrapeziums(Trapezium[] trapeziums, ModelRoot model, Scene scene)
    {
        var trapeziumCount = trapeziums.Length;

        var treeIndexAccessor = model.CreateAccessor();
        var colorAccessor = model.CreateAccessor();
        var vertex1Accessor = model.CreateAccessor();
        var vertex2Accessor = model.CreateAccessor();
        var vertex3Accessor = model.CreateAccessor();
        var vertex4Accessor = model.CreateAccessor();

        var node = scene.CreateNode("TrapeziumCollection");
        var meshGpuInstancing = node.UseExtension<MeshGpuInstancing>();
        meshGpuInstancing.SetAccessor("_treeIndex", treeIndexAccessor);
        meshGpuInstancing.SetAccessor("_color", colorAccessor);
        meshGpuInstancing.SetAccessor("_vertex1", vertex1Accessor);
        meshGpuInstancing.SetAccessor("_vertex2", vertex2Accessor);
        meshGpuInstancing.SetAccessor("_vertex3", vertex3Accessor);
        meshGpuInstancing.SetAccessor("_vertex4", vertex4Accessor);

        // create byte buffer
        const int byteStride = (1 + 1 + 1 + 1 + 1 + 1) * sizeof(float); // id + color + matrix
        var bufferView = model.CreateBufferView(byteStride * trapeziumCount, byteStride);
        var buffer = bufferView.Content.Array!;
        var bufferPos = 0;
        foreach (var trapezium in trapeziums)
        {
            var treeIndex = (float)trapezium.TreeIndex;
            buffer.Write(treeIndex, ref bufferPos);
            buffer.Write(trapezium.Color, ref bufferPos);
            buffer.Write(trapezium.Vertex1, ref bufferPos);
            buffer.Write(trapezium.Vertex2, ref bufferPos);
            buffer.Write(trapezium.Vertex3, ref bufferPos);
            buffer.Write(trapezium.Vertex4, ref bufferPos);
        }

        treeIndexAccessor.SetData(bufferView, 0, trapeziumCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        colorAccessor.SetData(bufferView, 4, trapeziumCount, DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, false);
        vertex1Accessor.SetData(bufferView, 8, trapeziumCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        vertex2Accessor.SetData(bufferView, 12, trapeziumCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        vertex3Accessor.SetData(bufferView, 16, trapeziumCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        vertex4Accessor.SetData(bufferView, 20, trapeziumCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Write(this byte[] buffer, float value, ref int bufferPos)
    {
        var source = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref value, 1));
        var target = buffer.AsSpan(bufferPos, sizeof(float));
        Debug.Assert(source.Length == target.Length);
        source.CopyTo(target);
        bufferPos += sizeof(float);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Write(this byte[] buffer, Color color, ref int bufferPos)
    {
        // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Drawing.Primitives/src/System/Drawing/Color.cs
        // writes Color memory byte layout directly to buffer
        var source = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref color, 1));
        var target = buffer.AsSpan(bufferPos, sizeof(float));
        Debug.Assert(source.Length == target.Length);
        source.CopyTo(target);
        bufferPos += 4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Write(this byte[] buffer, Matrix4x4 matrix, ref int bufferPos)
    {
        // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Numerics/Matrix4x4.cs
        // writes Matrix4x4 memory byte layout directly to buffer
        var source = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref matrix, 1));
        var target = buffer.AsSpan(bufferPos, sizeof(float) * 16);
        Debug.Assert(source.Length == target.Length);
        source.CopyTo(target);
        bufferPos += sizeof(float) * 16;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Write(this byte[] buffer, Vector3 vector, ref int bufferPos)
    {
        // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Numerics/Vector3.cs
        // writes Vector3 memory byte layout directly to buffer
        var source = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref vector, 1));
        var target = buffer.AsSpan(bufferPos, sizeof(float) * 3);
        Debug.Assert(source.Length == target.Length);
        source.CopyTo(target);
        bufferPos += sizeof(float) * 3;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Write(this byte[] buffer, Vector4 vector, ref int bufferPos)
    {
        // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Numerics/Vector4.cs
        // writes Vector3 memory byte layout directly to buffer
        var source = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref vector, 1));
        var target = buffer.AsSpan(bufferPos, sizeof(float) * 3);
        Debug.Assert(source.Length == target.Length);
        source.CopyTo(target);
        bufferPos += sizeof(float) * 4;
    }
}