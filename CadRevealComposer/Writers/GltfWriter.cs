namespace CadRevealComposer.Writers;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using Commons.Utils;
using GltfWriterUtils;
using Primitives;
using SharpGLTF.Memory;
using SharpGLTF.Schema2;

/// <summary>
/// Cognite Reveal format:
/// - Primitives are written with GLTF instancing extension. One GLTF node per type of primitive.
/// - All triangle meshes written in one single GLTF mesh.
/// - One GLTF node per instanced mesh.
///
/// https://github.com/KhronosGroup/glTF-Tutorials/tree/master/gltfTutorial
/// </summary>
public static class GltfWriter
{
    public static int WritePrimitives<T>(
        IReadOnlyList<APrimitive> primitives,
        Action<T[], ModelRoot, Scene> writeFunction,
        ModelRoot model,
        Scene scene
    )
    {
        var selectedPrimitives = primitives.OfType<T>().ToArray();
        var primitiveNumber = selectedPrimitives.Length;
        if (selectedPrimitives.Length > 0)
        {
            writeFunction(selectedPrimitives, model, scene);
        }

        return primitiveNumber;
    }

    public static void WriteSector(IReadOnlyList<APrimitive> primitives, Stream outputStream)
    {
        if (!BitConverter.IsLittleEndian)
        {
            throw new Exception(
                "This code copies bytes directly from memory to output and is coded to work with machines having little endian."
            );
        }

        var model = ModelRoot.CreateModel();
        var scene = model.UseScene(null);
        model.DefaultScene = scene;

        int counter = 0;
        counter += WritePrimitives<InstancedMesh>(primitives, WriteInstancedMeshes, model, scene);
        counter += WritePrimitives<TriangleMesh>(primitives, WriteTriangleMeshes, model, scene);
        counter += WritePrimitives<Box>(primitives, WriteBoxes, model, scene);
        counter += WritePrimitives<Circle>(primitives, WriteCircles, model, scene);
        counter += WritePrimitives<Cone>(primitives, WriteCones, model, scene);
        counter += WritePrimitives<EccentricCone>(primitives, WriteEccentricCones, model, scene);
        counter += WritePrimitives<EllipsoidSegment>(primitives, WriteEllipsoidSegments, model, scene);
        counter += WritePrimitives<GeneralCylinder>(primitives, WriteGeneralCylinders, model, scene);
        counter += WritePrimitives<GeneralRing>(primitives, WriteGeneralRings, model, scene);
        counter += WritePrimitives<Nut>(primitives, WriteNuts, model, scene);
        counter += WritePrimitives<Quad>(primitives, WriteQuads, model, scene);
        counter += WritePrimitives<TorusSegment>(primitives, WriteTorusSegments, model, scene);
        counter += WritePrimitives<Trapezium>(primitives, WriteTrapeziums, model, scene);

        Trace.Assert(counter == primitives.Count, "Not all primitives were processed in GltfWriter.");

        model.Asset.Copyright = $"Equinor ASA {DateTime.UtcNow.Year}";
        model.Asset.Generator = "rvmsharp";

        model.WriteGLB(outputStream);
    }

    private static void WriteInstancedMeshes(InstancedMesh[] meshes, ModelRoot model, Scene scene)
    {
        // Remark: InstancedMesh.InstanceId is shared across Sectors, and that means that we can have InstancedMesh groups
        // with only one item in this sector, but it will use the shared instance from another sector if already loaded from that sector.
        // So small instance groups increases file size, but not memory use.

        var instanceMeshGroups = meshes.GroupBy(m => m.InstanceId);

        foreach (var instanceMeshGroup in instanceMeshGroups)
        {
            var instanceId = instanceMeshGroup.Key;
            var sourceMesh = instanceMeshGroup.First().TemplateMesh;

            // create GLTF byte buffer
            var indexCount = sourceMesh.Indices.Length;
            var vertexCount = sourceMesh.Vertices.Length;
            var indicesBufferSize = indexCount * sizeof(uint);
            var vertexBufferSize = vertexCount * 3 * sizeof(float);
            var instanceCount = instanceMeshGroup.Count();
            const int byteStride = (1 + 1 + 16) * sizeof(float);
            var instanceBufferSize = byteStride * instanceCount;

            var bufferSize = indicesBufferSize + vertexBufferSize + instanceBufferSize;
            var buffer = model.CreateBuffer(bufferSize);

            // create GLTF buffer views
            var indexBuffer = model.UseBufferView(
                buffer,
                byteLength: indicesBufferSize,
                target: BufferMode.ELEMENT_ARRAY_BUFFER
            );
            var vertexBuffer = model.UseBufferView(
                buffer,
                byteOffset: indicesBufferSize,
                byteLength: vertexBufferSize,
                target: BufferMode.ARRAY_BUFFER
            );
            var instanceBuffer = model.UseBufferView(
                buffer,
                byteOffset: indicesBufferSize + vertexBufferSize,
                byteLength: instanceBufferSize,
                byteStride: byteStride
            );

            // write indices
            var indexBufferInt = MemoryMarshal.Cast<byte, uint>(indexBuffer.Content.AsSpan());
            sourceMesh.Indices.CopyTo(indexBufferInt);

            // write vertices
            var vertexBufferVector = MemoryMarshal.Cast<byte, Vector3>(vertexBuffer.Content.AsSpan());
            sourceMesh.Vertices.CopyTo(vertexBufferVector);

            // write instances
            var instanceBufferSpan = instanceBuffer.Content.AsSpan();
            var instanceBufferPos = 0;
            foreach (var instancedMesh in instanceMeshGroup)
            {
                var treeIndex = (float)instancedMesh.TreeIndex;
                instanceBufferSpan.Write(treeIndex, ref instanceBufferPos);
                instanceBufferSpan.Write(instancedMesh.Color, ref instanceBufferPos);
                instanceBufferSpan.Write(instancedMesh.InstanceMatrix, ref instanceBufferPos);
            }

            // create mesh buffer accessors
            var indexAccessor = model.CreateAccessor();
            var vertexAccessor = model.CreateAccessor();

            indexAccessor.SetData(indexBuffer, 0, indexCount, AttFormat.Uint);
            vertexAccessor.SetData(vertexBuffer, 0, vertexCount, AttFormat.Vec3Float);

            // create instance buffer accessors
            var treeIndexAccessor = model.CreateAccessor();
            var colorAccessor = model.CreateAccessor();
            var instanceMatrixAccessor = model.CreateAccessor();

            var instanceBufferWrapper = new BufferViewAutoOffset(instanceBuffer, instanceCount);
            treeIndexAccessor.SetDataAutoOffset(instanceBufferWrapper, AttFormat.Float);
            colorAccessor.SetDataAutoOffset(instanceBufferWrapper, AttFormat.Vec4UByteNormalized);
            instanceMatrixAccessor.SetDataAutoOffset(instanceBufferWrapper, AttFormat.Mat4x4Float);

            // create node
            var node = scene.CreateNode("InstanceMesh");
            var mesh = model.CreateMesh();
            mesh.Extras = JsonNode.Parse(FormattableString.Invariant($"{{\"InstanceId\":{instanceId}}}"));
            var meshPrimitive = mesh.CreatePrimitive();
            meshPrimitive.SetIndexAccessor(indexAccessor);
            meshPrimitive.SetVertexAccessor("POSITION", vertexAccessor);
            node.Mesh = mesh;
            var meshGpuInstancing = node.UseExtension<MeshGpuInstancing>();
            meshGpuInstancing.SetAccessor("_treeIndex", treeIndexAccessor);
            meshGpuInstancing.SetAccessor("_color", colorAccessor);
            meshGpuInstancing.SetAccessor("_instanceMatrix", instanceMatrixAccessor);
        }
    }

    private static void WriteTriangleMeshes(TriangleMesh[] triangleMeshes, ModelRoot model, Scene scene)
    {
        var indexCount = triangleMeshes.Sum(m => m.Mesh.Indices.Length);
        var vertexCount = triangleMeshes.Sum(m => m.Mesh.Vertices.Length);
        var indexBufferSize = indexCount * sizeof(uint);
        var vertexBufferByteStride = (1 + 1 + 3) * sizeof(float);
        var vertexBufferSize = vertexCount * vertexBufferByteStride;

        // create GLTF byte buffer
        var bufferSize = indexBufferSize + vertexBufferSize;
        var buffer = model.CreateBuffer(bufferSize);

        // create GLTF buffer views
        var indexBuffer = model.UseBufferView(
            buffer,
            byteLength: indexBufferSize,
            target: BufferMode.ELEMENT_ARRAY_BUFFER
        );
        var vertexBuffer = model.UseBufferView(
            buffer,
            byteOffset: indexBufferSize,
            byteLength: vertexBufferSize,
            byteStride: vertexBufferByteStride,
            target: BufferMode.ARRAY_BUFFER
        );

        // write all triangle meshes to same buffer
        var indexOffset = 0;
        var vertexOffset = 0;
        foreach (var triangleMesh in triangleMeshes)
        {
            var sourceMesh = triangleMesh.Mesh;

            // write indices
            var indices = sourceMesh.Indices;
            var indexBufferSpan = MemoryMarshal
                .Cast<byte, uint>(indexBuffer.Content.AsSpan())
                .Slice(indexOffset, indices.Length);
            for (var i = 0; i < indices.Length; i++)
            {
                indexBufferSpan[i] = (uint)vertexOffset + indices[i];
            }

            // write vertices
            var treeIndex = (float)triangleMesh.TreeIndex;
            var color = triangleMesh.Color;
            var vertexBufferSpan = vertexBuffer
                .Content.AsSpan()
                .Slice(vertexOffset * vertexBufferByteStride, sourceMesh.Vertices.Length * vertexBufferByteStride);

            var bufferPos = 0;
            foreach (var vertex in sourceMesh.Vertices)
            {
                vertexBufferSpan.Write(treeIndex, ref bufferPos);
                vertexBufferSpan.Write(color, ref bufferPos);
                vertexBufferSpan.Write(vertex, ref bufferPos);
            }

            indexOffset += sourceMesh.Indices.Length;
            vertexOffset += sourceMesh.Vertices.Length;
        }

        // create mesh buffer accessors
        var indexAccessor = model.CreateAccessor();
        var treeIndexAccessor = model.CreateAccessor();
        var colorAccessor = model.CreateAccessor();
        var positionAccessor = model.CreateAccessor();

        indexAccessor.SetData(indexBuffer, 0, indexCount, AttFormat.Uint);
        var vertexBufferWrapper = new BufferViewAutoOffset(vertexBuffer, vertexCount);
        treeIndexAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Float);
        colorAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Vec4UByteNormalized);
        positionAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Vec3Float);

        // create node
        var node = scene.CreateNode("TriangleMesh");
        var mesh = model.CreateMesh();
        var meshPrimitive = mesh.CreatePrimitive();
        meshPrimitive.SetIndexAccessor(indexAccessor);
        meshPrimitive.SetVertexAccessor("_treeIndex", treeIndexAccessor);
        meshPrimitive.SetVertexAccessor("COLOR_0", colorAccessor);
        meshPrimitive.SetVertexAccessor("POSITION", positionAccessor);
        node.Mesh = mesh;
    }

    private static void WriteBoxes(Box[] boxes, ModelRoot model, Scene scene)
    {
        var boxCount = boxes.Length;

        // create byte buffer
        const int byteStride = (1 + 1 + 16) * sizeof(float);
        var vertexBufferView = model.CreateBufferView(byteStride * boxCount, byteStride);
        var buffer = vertexBufferView.Content.AsSpan();
        var bufferPos = 0;
        foreach (var box in boxes)
        {
            var treeIndex = (float)box.TreeIndex;
            buffer.Write(treeIndex, ref bufferPos);
            buffer.Write(box.Color, ref bufferPos);
            buffer.Write(box.InstanceMatrix, ref bufferPos);
        }

        // create buffer accessors
        var treeIndexAccessor = model.CreateAccessor();
        var colorAccessor = model.CreateAccessor();
        var instanceMatrixAccessor = model.CreateAccessor();

        var vertexBufferWrapper = new BufferViewAutoOffset(vertexBufferView, boxCount);
        treeIndexAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Float);
        colorAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Vec4UByteNormalized);
        instanceMatrixAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Mat4x4Float);

        // create node
        var node = scene.CreateNode("BoxCollection");
        var meshGpuInstancing = node.UseExtension<MeshGpuInstancing>();
        meshGpuInstancing.SetAccessor("_treeIndex", treeIndexAccessor);
        meshGpuInstancing.SetAccessor("_color", colorAccessor);
        meshGpuInstancing.SetAccessor("_instanceMatrix", instanceMatrixAccessor);
    }

    private static void WriteCircles(Circle[] circles, ModelRoot model, Scene scene)
    {
        var circleCount = circles.Length;

        // create byte buffer
        const int byteStride = (1 + 1 + 16 + 3) * sizeof(float); // id + color + matrix + normal
        var bufferView = model.CreateBufferView(byteStride * circleCount, byteStride);
        var buffer = bufferView.Content.AsSpan();
        var bufferPos = 0;
        foreach (var circle in circles)
        {
            var treeIndex = (float)circle.TreeIndex;
            buffer.Write(treeIndex, ref bufferPos);
            buffer.Write(circle.Color, ref bufferPos);
            buffer.Write(circle.InstanceMatrix, ref bufferPos);
            buffer.Write(circle.Normal, ref bufferPos);
        }

        // create buffer accessors
        var treeIndexAccessor = model.CreateAccessor();
        var colorAccessor = model.CreateAccessor();
        var instanceMatrixAccessor = model.CreateAccessor();
        var normalAccessor = model.CreateAccessor();

        var vertexBufferWrapper = new BufferViewAutoOffset(bufferView, circleCount);
        treeIndexAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Float);
        colorAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Vec4UByteNormalized);
        instanceMatrixAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Mat4x4Float);
        normalAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Vec3Float);

        // create node
        var node = scene.CreateNode("CircleCollection");
        var meshGpuInstancing = node.UseExtension<MeshGpuInstancing>();
        meshGpuInstancing.SetAccessor("_treeIndex", treeIndexAccessor);
        meshGpuInstancing.SetAccessor("_color", colorAccessor);
        meshGpuInstancing.SetAccessor("_instanceMatrix", instanceMatrixAccessor);
        meshGpuInstancing.SetAccessor("_normal", normalAccessor);
    }

    private static void WriteCones(Cone[] cones, ModelRoot model, Scene scene)
    {
        var coneCount = cones.Length;

        // create byte buffer
        const int byteStride = (1 + 1 + 1 + 1 + 3 + 3 + 3 + 1 + 1) * sizeof(float);
        var bufferView = model.CreateBufferView(byteStride * coneCount, byteStride);
        var buffer = bufferView.Content.AsSpan();
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

        // create buffer accessors
        var treeIndexAccessor = model.CreateAccessor();
        var colorAccessor = model.CreateAccessor();
        var angleAccessor = model.CreateAccessor();
        var arcAngleAccessor = model.CreateAccessor();
        var centerAAccessor = model.CreateAccessor();
        var centerBAccessor = model.CreateAccessor();
        var localXAxisAccessor = model.CreateAccessor();
        var radiusAAccessor = model.CreateAccessor();
        var radiusBAccessor = model.CreateAccessor();

        var vertexBufferWrapper = new BufferViewAutoOffset(bufferView, coneCount);
        treeIndexAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Float);
        colorAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Vec4UByteNormalized);
        angleAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Float);
        arcAngleAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Float);
        centerAAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Vec3Float);
        centerBAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Vec3Float);
        localXAxisAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Vec3Float);
        radiusAAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Float);
        radiusBAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Float);

        // create node
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
    }

    private static void WriteEccentricCones(EccentricCone[] eccentricCones, ModelRoot model, Scene scene)
    {
        var eccentricConeCount = eccentricCones.Length;

        // create byte buffer
        const int byteStride = (1 + 1 + 3 + 3 + 3 + 1 + 1) * sizeof(float);
        var bufferView = model.CreateBufferView(byteStride * eccentricConeCount, byteStride);
        var buffer = bufferView.Content.AsSpan();
        var bufferPos = 0;
        foreach (var eccentricCone in eccentricCones)
        {
            var treeIndex = (float)eccentricCone.TreeIndex;
            buffer.Write(treeIndex, ref bufferPos);
            buffer.Write(eccentricCone.Color, ref bufferPos);
            buffer.Write(eccentricCone.CenterA, ref bufferPos);
            buffer.Write(eccentricCone.CenterB, ref bufferPos);
            buffer.Write(eccentricCone.Normal, ref bufferPos);
            buffer.Write(eccentricCone.RadiusA, ref bufferPos);
            buffer.Write(eccentricCone.RadiusB, ref bufferPos);
        }

        // create buffer accessors
        var treeIndexAccessor = model.CreateAccessor();
        var colorAccessor = model.CreateAccessor();
        var centerAAccessor = model.CreateAccessor();
        var centerBAccessor = model.CreateAccessor();
        var normalAccessor = model.CreateAccessor();
        var radiusAAccessor = model.CreateAccessor();
        var radiusBAccessor = model.CreateAccessor();

        var vertexBufferWrapper = new BufferViewAutoOffset(bufferView, eccentricConeCount);
        treeIndexAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Float);
        colorAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Vec4UByteNormalized);
        centerAAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Vec3Float);
        centerBAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Vec3Float);
        normalAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Vec3Float);
        radiusAAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Float);
        radiusBAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Float);

        // create node
        var node = scene.CreateNode("EccentricConeCollection");
        var meshGpuInstancing = node.UseExtension<MeshGpuInstancing>();
        meshGpuInstancing.SetAccessor("_treeIndex", treeIndexAccessor);
        meshGpuInstancing.SetAccessor("_color", colorAccessor);
        meshGpuInstancing.SetAccessor("_centerA", centerAAccessor);
        meshGpuInstancing.SetAccessor("_centerB", centerBAccessor);
        meshGpuInstancing.SetAccessor("_normal", normalAccessor);
        meshGpuInstancing.SetAccessor("_radiusA", radiusAAccessor);
        meshGpuInstancing.SetAccessor("_radiusB", radiusBAccessor);
    }

    private static void WriteEllipsoidSegments(EllipsoidSegment[] ellipsoids, ModelRoot model, Scene scene)
    {
        var ellipsoidCount = ellipsoids.Length;

        // create byte buffer
        const int byteStride = (1 + 1 + 1 + 1 + 1 + 3 + 3) * sizeof(float); // id + color + matrix
        var bufferView = model.CreateBufferView(byteStride * ellipsoidCount, byteStride);
        var buffer = bufferView.Content.AsSpan();
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
            buffer.Write(ellipsoid.Normal, ref bufferPos);
        }

        // create buffer accessors
        var treeIndexAccessor = model.CreateAccessor();
        var colorAccessor = model.CreateAccessor();
        var horizontalRadiusAccessor = model.CreateAccessor();
        var verticalRadiusAccessor = model.CreateAccessor();
        var heightAccessor = model.CreateAccessor();
        var centerAccessor = model.CreateAccessor();
        var normalAccessor = model.CreateAccessor();

        var vertexBufferWrapper = new BufferViewAutoOffset(bufferView, ellipsoidCount);
        treeIndexAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Float);
        colorAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Vec4UByteNormalized);
        horizontalRadiusAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Float);
        verticalRadiusAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Float);
        heightAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Float);
        centerAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Vec3Float);
        normalAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Vec3Float);

        // create node
        var node = scene.CreateNode("EllipsoidSegmentCollection");
        var meshGpuInstancing = node.UseExtension<MeshGpuInstancing>();
        meshGpuInstancing.SetAccessor("_treeIndex", treeIndexAccessor);
        meshGpuInstancing.SetAccessor("_color", colorAccessor);
        meshGpuInstancing.SetAccessor("_horizontalRadius", horizontalRadiusAccessor);
        meshGpuInstancing.SetAccessor("_verticalRadius", verticalRadiusAccessor);
        meshGpuInstancing.SetAccessor("_height", heightAccessor);
        meshGpuInstancing.SetAccessor("_center", centerAccessor);
        meshGpuInstancing.SetAccessor("_normal", normalAccessor);
    }

    private static void WriteGeneralCylinders(GeneralCylinder[] generalCylinders, ModelRoot model, Scene scene)
    {
        var generalCylinderCount = generalCylinders.Length;

        // create byte buffer
        const int byteStride = (1 + 1 + 1 + 1 + 3 + 3 + 3 + 4 + 4 + 1) * sizeof(float);
        var bufferView = model.CreateBufferView(byteStride * generalCylinderCount, byteStride);
        var buffer = bufferView.Content.AsSpan();
        var bufferPos = 0;
        foreach (var generalCylinder in generalCylinders)
        {
            var treeIndex = (float)generalCylinder.TreeIndex;
            buffer.Write(treeIndex, ref bufferPos);
            buffer.Write(generalCylinder.Color, ref bufferPos);
            buffer.Write(generalCylinder.CenterA, ref bufferPos);
            buffer.Write(generalCylinder.CenterB, ref bufferPos);
            buffer.Write(generalCylinder.Radius, ref bufferPos);
            buffer.Write(generalCylinder.PlaneA, ref bufferPos);
            buffer.Write(generalCylinder.PlaneB, ref bufferPos);
            buffer.Write(generalCylinder.LocalXAxis, ref bufferPos);
            buffer.Write(generalCylinder.Angle, ref bufferPos);
            buffer.Write(generalCylinder.ArcAngle, ref bufferPos);
        }

        // create buffer accessors
        var treeIndexAccessor = model.CreateAccessor();
        var colorAccessor = model.CreateAccessor();
        var centerAAccessor = model.CreateAccessor();
        var centerBAccessor = model.CreateAccessor();
        var radiusAccessor = model.CreateAccessor();
        var planeAAccessor = model.CreateAccessor();
        var planeBAccessor = model.CreateAccessor();
        var localXAxisAccessor = model.CreateAccessor();
        var angleAccessor = model.CreateAccessor();
        var arcAngleAccessor = model.CreateAccessor();

        var vertexDataWrapper = new BufferViewAutoOffset(bufferView, generalCylinderCount);
        treeIndexAccessor.SetDataAutoOffset(vertexDataWrapper, AttFormat.Float);
        colorAccessor.SetDataAutoOffset(vertexDataWrapper, AttFormat.Vec4UByteNormalized);
        centerAAccessor.SetDataAutoOffset(vertexDataWrapper, AttFormat.Vec3Float);
        centerBAccessor.SetDataAutoOffset(vertexDataWrapper, AttFormat.Vec3Float);
        radiusAccessor.SetDataAutoOffset(vertexDataWrapper, AttFormat.Float);
        planeAAccessor.SetDataAutoOffset(vertexDataWrapper, AttFormat.Vec4Float);
        planeBAccessor.SetDataAutoOffset(vertexDataWrapper, AttFormat.Vec4Float);
        localXAxisAccessor.SetDataAutoOffset(vertexDataWrapper, AttFormat.Vec3Float);
        angleAccessor.SetDataAutoOffset(vertexDataWrapper, AttFormat.Float);
        arcAngleAccessor.SetDataAutoOffset(vertexDataWrapper, AttFormat.Float);

        // create node
        var node = scene.CreateNode("GeneralCylinderCollection");
        var meshGpuInstancing = node.UseExtension<MeshGpuInstancing>();
        meshGpuInstancing.SetAccessor("_treeIndex", treeIndexAccessor);
        meshGpuInstancing.SetAccessor("_color", colorAccessor);
        meshGpuInstancing.SetAccessor("_centerA", centerAAccessor);
        meshGpuInstancing.SetAccessor("_centerB", centerBAccessor);
        meshGpuInstancing.SetAccessor("_radius", radiusAccessor);
        meshGpuInstancing.SetAccessor("_planeA", planeAAccessor);
        meshGpuInstancing.SetAccessor("_planeB", planeBAccessor);
        meshGpuInstancing.SetAccessor("_localXAxis", localXAxisAccessor);
        meshGpuInstancing.SetAccessor("_angle", angleAccessor);
        meshGpuInstancing.SetAccessor("_arcAngle", arcAngleAccessor);
    }

    private static void WriteGeneralRings(GeneralRing[] generalRings, ModelRoot model, Scene scene)
    {
        var generalRingCount = generalRings.Length;

        // create byte buffer
        const int byteStride = (1 + 1 + 1 + 1 + 16 + 3 + 1) * sizeof(float);
        var bufferView = model.CreateBufferView(byteStride * generalRingCount, byteStride);
        var buffer = bufferView.Content.AsSpan();
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

        // create buffer accessors
        var treeIndexAccessor = model.CreateAccessor();
        var colorAccessor = model.CreateAccessor();
        var angleAccessor = model.CreateAccessor();
        var arcAngleAccessor = model.CreateAccessor();
        var instanceMatrixAccessor = model.CreateAccessor();
        var normalAccessor = model.CreateAccessor();
        var thicknessAccessor = model.CreateAccessor();

        var vertexBufferWrapper = new BufferViewAutoOffset(bufferView, generalRingCount);
        treeIndexAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Float);
        colorAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Vec4UByteNormalized);
        angleAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Float);
        arcAngleAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Float);
        instanceMatrixAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Mat4x4Float);
        normalAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Vec3Float);
        thicknessAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Float);

        // create node
        var node = scene.CreateNode("GeneralRingCollection");
        var meshGpuInstancing = node.UseExtension<MeshGpuInstancing>();
        meshGpuInstancing.SetAccessor("_treeIndex", treeIndexAccessor);
        meshGpuInstancing.SetAccessor("_color", colorAccessor);
        meshGpuInstancing.SetAccessor("_angle", angleAccessor);
        meshGpuInstancing.SetAccessor("_arcAngle", arcAngleAccessor);
        meshGpuInstancing.SetAccessor("_instanceMatrix", instanceMatrixAccessor);
        meshGpuInstancing.SetAccessor("_normal", normalAccessor);
        meshGpuInstancing.SetAccessor("_thickness", thicknessAccessor);
    }

    private static void WriteNuts(Nut[] nuts, ModelRoot model, Scene scene)
    {
        var nutCount = nuts.Length;

        // create byte buffer
        const int byteStride = (1 + 1 + 16) * sizeof(float); // id + color + matrix
        var bufferView = model.CreateBufferView(byteStride * nutCount, byteStride);
        var buffer = bufferView.Content.AsSpan();
        var bufferPos = 0;
        foreach (var nut in nuts)
        {
            var treeIndex = (float)nut.TreeIndex;
            buffer.Write(treeIndex, ref bufferPos);
            buffer.Write(nut.Color, ref bufferPos);
            buffer.Write(nut.InstanceMatrix, ref bufferPos);
        }

        // create buffer accessors
        var treeIndexAccessor = model.CreateAccessor();
        var colorAccessor = model.CreateAccessor();
        var instanceMatrixAccessor = model.CreateAccessor();

        var vertexBufferWrapper = new BufferViewAutoOffset(bufferView, nutCount);
        treeIndexAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Float);
        colorAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Vec4UByteNormalized);
        instanceMatrixAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Mat4x4Float);

        // create node
        var node = scene.CreateNode("NutCollection");
        var meshGpuInstancing = node.UseExtension<MeshGpuInstancing>();
        meshGpuInstancing.SetAccessor("_treeIndex", treeIndexAccessor);
        meshGpuInstancing.SetAccessor("_color", colorAccessor);
        meshGpuInstancing.SetAccessor("_instanceMatrix", instanceMatrixAccessor);
    }

    private static void WriteQuads(Quad[] quads, ModelRoot model, Scene scene)
    {
        var quadCount = quads.Length;

        // create byte buffer
        const int byteStride = (1 + 1 + 16) * sizeof(float); // id + color + matrix
        var bufferView = model.CreateBufferView(byteStride * quadCount, byteStride);
        var buffer = bufferView.Content.AsSpan();
        var bufferPos = 0;
        foreach (var quad in quads)
        {
            var treeIndex = (float)quad.TreeIndex;
            buffer.Write(treeIndex, ref bufferPos);
            buffer.Write(quad.Color, ref bufferPos);
            buffer.Write(quad.InstanceMatrix, ref bufferPos);
        }

        // create buffer accessors
        var treeIndexAccessor = model.CreateAccessor();
        var colorAccessor = model.CreateAccessor();
        var instanceMatrixAccessor = model.CreateAccessor();

        var vertexBufferWrapper = new BufferViewAutoOffset(bufferView, quadCount);
        treeIndexAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Float);
        colorAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Vec4UByteNormalized);
        instanceMatrixAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Mat4x4Float);

        // create node
        var node = scene.CreateNode("QuadCollection");
        var meshGpuInstancing = node.UseExtension<MeshGpuInstancing>();
        meshGpuInstancing.SetAccessor("_treeIndex", treeIndexAccessor);
        meshGpuInstancing.SetAccessor("_color", colorAccessor);
        meshGpuInstancing.SetAccessor("_instanceMatrix", instanceMatrixAccessor);
    }

    private static void WriteTorusSegments(TorusSegment[] torus, ModelRoot model, Scene scene)
    {
        var torusCount = torus.Length;

        // create byte buffer
        const int byteStride = (1 + 1 + 1 + 16 + 1 + 1) * sizeof(float);
        var bufferView = model.CreateBufferView(byteStride * torusCount, byteStride);
        var buffer = bufferView.Content.AsSpan();
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

        // create buffer accessors
        var treeIndexAccessor = model.CreateAccessor();
        var colorAccessor = model.CreateAccessor();
        var arcAngleAccessor = model.CreateAccessor();
        var instanceMatrixAccessor = model.CreateAccessor();
        var radiusAccessor = model.CreateAccessor();
        var tubeRadiusAccessor = model.CreateAccessor();

        var vertexBufferWrapper = new BufferViewAutoOffset(bufferView, torusCount);
        treeIndexAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Float);
        colorAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Vec4UByteNormalized);
        arcAngleAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Float);
        instanceMatrixAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Mat4x4Float);
        radiusAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Float);
        tubeRadiusAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Float);

        // create node
        var node = scene.CreateNode("TorusSegmentCollection");
        var meshGpuInstancing = node.UseExtension<MeshGpuInstancing>();
        meshGpuInstancing.SetAccessor("_treeIndex", treeIndexAccessor);
        meshGpuInstancing.SetAccessor("_color", colorAccessor);
        meshGpuInstancing.SetAccessor("_arcAngle", arcAngleAccessor);
        meshGpuInstancing.SetAccessor("_instanceMatrix", instanceMatrixAccessor);
        meshGpuInstancing.SetAccessor("_radius", radiusAccessor);
        meshGpuInstancing.SetAccessor("_tubeRadius", tubeRadiusAccessor);
    }

    private static void WriteTrapeziums(Trapezium[] trapeziums, ModelRoot model, Scene scene)
    {
        var trapeziumCount = trapeziums.Length;

        // create byte buffer
        const int byteStride = (1 + 1 + 3 + 3 + 3 + 3) * sizeof(float); // id + color + matrix
        var bufferView = model.CreateBufferView(byteStride * trapeziumCount, byteStride);
        var buffer = bufferView.Content.AsSpan();
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

        // create buffer accessors
        var treeIndexAccessor = model.CreateAccessor();
        var colorAccessor = model.CreateAccessor();
        var vertex1Accessor = model.CreateAccessor();
        var vertex2Accessor = model.CreateAccessor();
        var vertex3Accessor = model.CreateAccessor();
        var vertex4Accessor = model.CreateAccessor();

        var vertexBufferWrapper = new BufferViewAutoOffset(bufferView, trapeziumCount);
        treeIndexAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Float);
        colorAccessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Vec4UByteNormalized);
        vertex1Accessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Vec3Float);
        vertex2Accessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Vec3Float);
        vertex3Accessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Vec3Float);
        vertex4Accessor.SetDataAutoOffset(vertexBufferWrapper, AttFormat.Vec3Float);

        // create node
        var node = scene.CreateNode("TrapeziumCollection");
        var meshGpuInstancing = node.UseExtension<MeshGpuInstancing>();
        meshGpuInstancing.SetAccessor("_treeIndex", treeIndexAccessor);
        meshGpuInstancing.SetAccessor("_color", colorAccessor);
        meshGpuInstancing.SetAccessor("_vertex1", vertex1Accessor);
        meshGpuInstancing.SetAccessor("_vertex2", vertex2Accessor);
        meshGpuInstancing.SetAccessor("_vertex3", vertex3Accessor);
        meshGpuInstancing.SetAccessor("_vertex4", vertex4Accessor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Write(this Span<byte> buffer, float value, ref int bufferPos)
    {
        ThrowIfValueIsNotFinite(value);
        var source = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref value, 1));
        var target = buffer.Slice(bufferPos, sizeof(float));
        Debug.Assert(source.Length == target.Length);
        source.CopyTo(target);
        bufferPos += sizeof(float);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Write(this Span<byte> buffer, Color color, ref int bufferPos)
    {
        // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Drawing.Primitives/src/System/Drawing/Color.cs
        // writes Color memory byte layout directly to buffer
        var target = buffer.Slice(bufferPos, 4);
        target[0] = color.R;
        target[1] = color.G;
        target[2] = color.B;
        target[3] = color.A;
        bufferPos += 4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Write(this Span<byte> buffer, Matrix4x4 matrix, ref int bufferPos)
    {
        foreach (float f in matrix.AsEnumerableRowMajor())
        {
            ThrowIfValueIsNotFinite(f);
        }

        // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Numerics/Matrix4x4.cs
        // writes Matrix4x4 memory byte layout directly to buffer
        var source = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref matrix, 1));
        var target = buffer.Slice(bufferPos, sizeof(float) * 16);
        Debug.Assert(source.Length == target.Length);
        source.CopyTo(target);
        bufferPos += sizeof(float) * 16;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Write(this Span<byte> buffer, Vector3 vector, ref int bufferPos)
    {
        ThrowIfValueIsNotFinite(vector.X);
        ThrowIfValueIsNotFinite(vector.Y);
        ThrowIfValueIsNotFinite(vector.Z);
        // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Numerics/Vector3.cs
        // writes Vector3 memory byte layout directly to buffer
        var source = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref vector, 1));
        var target = buffer.Slice(bufferPos, sizeof(float) * 3);
        Debug.Assert(source.Length == target.Length);
        source.CopyTo(target);
        bufferPos += sizeof(float) * 3;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Write(this Span<byte> buffer, Vector4 vector, ref int bufferPos)
    {
        ThrowIfValueIsNotFinite(vector.X);
        ThrowIfValueIsNotFinite(vector.Y);
        ThrowIfValueIsNotFinite(vector.Z);
        ThrowIfValueIsNotFinite(vector.W);
        // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Numerics/Vector4.cs
        // writes Vector4 memory byte layout directly to buffer
        var source = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref vector, 1));
        var target = buffer.Slice(bufferPos, sizeof(float) * 4);
        Debug.Assert(source.Length == target.Length);
        source.CopyTo(target);
        bufferPos += sizeof(float) * 4;
    }

    /// <summary>
    /// From Gltf v2 spec: (https://www.khronos.org/registry/glTF/specs/2.0/glTF-2.0.html#accessor-data-types)
    /// > Values of NaN, +Infinity, and -Infinity MUST NOT be present.
    ///
    /// This guards from writing non-finite values of floats.
    /// Will throw an ArgumentOutOfRangeException if input value is not finite.
    ///
    /// This is done to avoid this error coming when we try to serialize to gltf,
    /// as we then do not have any stack trace to what primitive or other was trying
    /// to write the non-finite number.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ThrowIfValueIsNotFinite(float value)
    {
        if (!float.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                $"value was {value}, and cannot be serialized to gltf json."
            );
        }
    }

    /// <summary>
    /// Helper for common AttributeFormats
    /// </summary>
    private static class AttFormat
    {
        // ReSharper disable once UnusedMember.Local -- kept for possible future use
        public static readonly AttributeFormat Vec4UByte = new AttributeFormat(
            DimensionType.VEC4,
            EncodingType.UNSIGNED_BYTE
        );

        public static readonly AttributeFormat Vec4UByteNormalized = new AttributeFormat(
            DimensionType.VEC4,
            EncodingType.UNSIGNED_BYTE,
            true
        );

        public static readonly AttributeFormat Mat4x4Float = new AttributeFormat(
            DimensionType.MAT4,
            EncodingType.FLOAT
        );

        public static readonly AttributeFormat Vec4Float = new AttributeFormat(DimensionType.VEC4, EncodingType.FLOAT);
        public static readonly AttributeFormat Vec3Float = new AttributeFormat(DimensionType.VEC3, EncodingType.FLOAT);
        public static readonly AttributeFormat Float = new AttributeFormat(DimensionType.SCALAR, EncodingType.FLOAT);

        public static readonly AttributeFormat Uint = new AttributeFormat(
            DimensionType.SCALAR,
            EncodingType.UNSIGNED_INT
        );
    }
}
