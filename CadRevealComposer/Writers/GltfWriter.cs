namespace CadRevealComposer.Writers;

using Primitives;
using SharpGLTF.IO;
using SharpGLTF.Schema2;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// Cognite Reveal format:
/// - Primitives are written with GLTF instancing extension. One GLTF node per type of primitive.
/// - All triangle meshes written in one single GLTF mesh.
/// - One GLTF node per instanced mesh.
///  
/// https://github.com/KhronosGroup/glTF-Tutorials/tree/master/gltfTutorial
/// GltfSectorParser.ts
/// GltfSectorLoader.ts
///
/// Little endian, Vector3, Vector4, float, ushort, Matrix4x4, arrays, Color
/// </summary>
public static class GltfWriter
{
    public static void WriteSector(APrimitive[] /* do NOT replace with IEnumerable */ primitives, Stream stream)
    {
        if (!BitConverter.IsLittleEndian)
        {
            throw new Exception("This code copies bytes directly from memory to output and is coded to work with machines having little endian.");
        }

        var model = ModelRoot.CreateModel();
        var scene = model.UseScene(null);
        model.DefaultScene = scene;

        var instancedMeshes = primitives.OfType<InstancedMesh>().ToArray();
        if (instancedMeshes.Length > 0)
        {
            WriteInstancedMeshes(instancedMeshes, model, scene);
        }

        var triangleMeshes = primitives.OfType<TriangleMesh>().ToArray();
        if (triangleMeshes.Length > 0)
        {
            WriteTriangleMeshes(triangleMeshes, model, scene);
        }

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

        var ellipsoids = primitives.OfType<EllipsoidSegment>().ToArray();
        if (ellipsoids.Length > 0)
        {
            WriteEllipsoidSegments(ellipsoids, model, scene);
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

        var torus = primitives.OfType<TorusSegment>().ToArray();
        if (torus.Length > 0)
        {
            WriteTorusSegments(torus, model, scene);
        }

        var trapeziums = primitives.OfType<Trapezium>().ToArray();
        if (trapeziums.Length > 0)
        {
            WriteTrapeziums(trapeziums, model, scene);
        }

        model.Asset.Copyright = "Equinor ASA";
        model.WriteGLB(stream);
    }

    private static void WriteInstancedMeshes(InstancedMesh[] meshes, ModelRoot model, Scene scene)
    {
        // TODO: don't write single mesh
        // TODO: don't write single mesh
        // TODO: don't write single mesh
        // TODO: don't write single mesh

        var instanceMeshGroups = meshes.GroupBy(m => m.InstanceId);

        foreach (var instanceMeshGroup in instanceMeshGroups)
        {
            var instanceId = instanceMeshGroup.Key;
            var sourceMesh = instanceMeshGroup.First().Mesh;

            // create GLTF byte buffer
            var indexCount = sourceMesh.Triangles.Length;
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
                target: BufferMode.ELEMENT_ARRAY_BUFFER);
            var vertexBuffer = model.UseBufferView(
                buffer,
                byteOffset: indicesBufferSize,
                byteLength: vertexBufferSize,
                target: BufferMode.ARRAY_BUFFER);
            var instanceBuffer = model.UseBufferView(
                buffer,
                byteOffset: indicesBufferSize + vertexBufferSize,
                byteLength: instanceBufferSize,
                byteStride: byteStride);

            // write indices
            var indexBufferInt = MemoryMarshal.Cast<byte, uint>(indexBuffer.Content.AsSpan());
            sourceMesh.Triangles.CopyTo(indexBufferInt);

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

            indexAccessor.SetData(indexBuffer, 0, indexCount, DimensionType.SCALAR, EncodingType.UNSIGNED_INT, false);
            vertexAccessor.SetData(vertexBuffer, 0, vertexCount, DimensionType.VEC3, EncodingType.FLOAT, false);

            // create instance buffer accessors
            var treeIndexAccessor = model.CreateAccessor();
            var colorAccessor = model.CreateAccessor();
            var instanceMatrixAccessor = model.CreateAccessor();

            treeIndexAccessor.SetData(instanceBuffer, 0, instanceCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
            colorAccessor.SetData(instanceBuffer, 4, instanceCount, DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, false);
            instanceMatrixAccessor.SetData(instanceBuffer, 8, instanceCount, DimensionType.MAT4, EncodingType.FLOAT, false);

            // create node
            var node = scene.CreateNode("InstanceMesh");
            var mesh = model.CreateMesh();
            mesh.Extras = JsonContent.Parse(FormattableString.Invariant($"{{\"InstanceId\":{instanceId}}}"));
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
        var indexCount = triangleMeshes.Sum(m => m.Mesh.Triangles.Length);
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
            target: BufferMode.ELEMENT_ARRAY_BUFFER);
        var vertexBuffer = model.UseBufferView(
            buffer,
            byteOffset: indexBufferSize,
            byteLength: vertexBufferSize,
            byteStride: vertexBufferByteStride,
            target: BufferMode.ARRAY_BUFFER);

        // write all triangle meshes to same buffer
        var indexOffset = 0;
        var vertexOffset = 0;
        foreach (var triangleMesh in triangleMeshes)
        {
            var sourceMesh = triangleMesh.Mesh;

            // write indices
            var indices = sourceMesh.Triangles;
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
            var vertexBufferSpan = vertexBuffer.Content.AsSpan()
                .Slice(vertexOffset * vertexBufferByteStride, sourceMesh.Vertices.Length * vertexBufferByteStride);

            var bufferPos = 0;
            foreach (var vertex in sourceMesh.Vertices)
            {
                vertexBufferSpan.Write(treeIndex, ref bufferPos);
                vertexBufferSpan.Write(color, ref bufferPos);
                vertexBufferSpan.Write(vertex, ref bufferPos);
            }

            indexOffset += sourceMesh.Triangles.Length;
            vertexOffset += sourceMesh.Vertices.Length;
        }

        // create mesh buffer accessors
        var indexAccessor = model.CreateAccessor();
        var treeIndexAccessor = model.CreateAccessor();
        var colorAccessor = model.CreateAccessor();
        var positionAccessor = model.CreateAccessor();

        indexAccessor.SetData(indexBuffer, 0, indexCount, DimensionType.SCALAR, EncodingType.UNSIGNED_INT, false);
        treeIndexAccessor.SetData(vertexBuffer, 0, vertexCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        colorAccessor.SetData(vertexBuffer, 4, vertexCount, DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, true);
        positionAccessor.SetData(vertexBuffer, 8, vertexCount, DimensionType.VEC3, EncodingType.FLOAT, false);

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
        var bufferView = model.CreateBufferView(byteStride * boxCount, byteStride);
        var buffer = bufferView.Content.AsSpan();
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

        treeIndexAccessor.SetData(bufferView, 0, boxCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        colorAccessor.SetData(bufferView, 4, boxCount, DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, false);
        instanceMatrixAccessor.SetData(bufferView, 8, boxCount, DimensionType.MAT4, EncodingType.FLOAT, false);

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

        treeIndexAccessor.SetData(bufferView, 0, circleCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        colorAccessor.SetData(bufferView, 4, circleCount, DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, false);
        instanceMatrixAccessor.SetData(bufferView, 8, circleCount, DimensionType.MAT4, EncodingType.FLOAT, false);
        normalAccessor.SetData(bufferView, 72, circleCount, DimensionType.VEC3, EncodingType.FLOAT, false);

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

        treeIndexAccessor.SetData(bufferView, 0, coneCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        colorAccessor.SetData(bufferView, 4, coneCount, DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, false);
        angleAccessor.SetData(bufferView, 8, coneCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        arcAngleAccessor.SetData(bufferView, 12, coneCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        centerAAccessor.SetData(bufferView, 16, coneCount, DimensionType.VEC3, EncodingType.FLOAT, false);
        centerBAccessor.SetData(bufferView, 28, coneCount, DimensionType.VEC3, EncodingType.FLOAT, false);
        localXAxisAccessor.SetData(bufferView, 40, coneCount, DimensionType.VEC3, EncodingType.FLOAT, false);
        radiusAAccessor.SetData(bufferView, 52, coneCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        radiusBAccessor.SetData(bufferView, 56, coneCount, DimensionType.SCALAR, EncodingType.FLOAT, false);

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

        treeIndexAccessor.SetData(bufferView, 0, eccentricConeCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        colorAccessor.SetData(bufferView, 4, eccentricConeCount, DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, false);
        centerAAccessor.SetData(bufferView, 8, eccentricConeCount, DimensionType.VEC3, EncodingType.FLOAT, false);
        centerBAccessor.SetData(bufferView, 20, eccentricConeCount, DimensionType.VEC3, EncodingType.FLOAT, false);
        normalAccessor.SetData(bufferView, 32, eccentricConeCount, DimensionType.VEC3, EncodingType.FLOAT, false);
        radiusAAccessor.SetData(bufferView, 44, eccentricConeCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        radiusBAccessor.SetData(bufferView, 48, eccentricConeCount, DimensionType.SCALAR, EncodingType.FLOAT, false);

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

        treeIndexAccessor.SetData(bufferView, 0, ellipsoidCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        colorAccessor.SetData(bufferView, 4, ellipsoidCount, DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, false);
        horizontalRadiusAccessor.SetData(bufferView, 8, ellipsoidCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        verticalRadiusAccessor.SetData(bufferView, 12, ellipsoidCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        heightAccessor.SetData(bufferView, 16, ellipsoidCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        centerAccessor.SetData(bufferView, 20, ellipsoidCount, DimensionType.VEC3, EncodingType.FLOAT, false);
        normalAccessor.SetData(bufferView, 32, ellipsoidCount, DimensionType.VEC3, EncodingType.FLOAT, false);

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

        treeIndexAccessor.SetData(bufferView, 0, generalCylinderCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        colorAccessor.SetData(bufferView, 4, generalCylinderCount, DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, false);
        centerAAccessor.SetData(bufferView, 8, generalCylinderCount, DimensionType.VEC3, EncodingType.FLOAT, false);
        centerBAccessor.SetData(bufferView, 20, generalCylinderCount, DimensionType.VEC3, EncodingType.FLOAT, false);
        radiusAccessor.SetData(bufferView, 32, generalCylinderCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        planeAAccessor.SetData(bufferView, 36, generalCylinderCount, DimensionType.VEC4, EncodingType.FLOAT, false);
        planeBAccessor.SetData(bufferView, 52, generalCylinderCount, DimensionType.VEC4, EncodingType.FLOAT, false);
        localXAxisAccessor.SetData(bufferView, 68, generalCylinderCount, DimensionType.VEC3, EncodingType.FLOAT, false);
        angleAccessor.SetData(bufferView, 80, generalCylinderCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        arcAngleAccessor.SetData(bufferView, 84, generalCylinderCount, DimensionType.SCALAR, EncodingType.FLOAT, false);

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

        treeIndexAccessor.SetData(bufferView, 0, generalRingCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        colorAccessor.SetData(bufferView, 4, generalRingCount, DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, false);
        angleAccessor.SetData(bufferView, 8, generalRingCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        arcAngleAccessor.SetData(bufferView, 12, generalRingCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        instanceMatrixAccessor.SetData(bufferView, 16, generalRingCount, DimensionType.MAT4, EncodingType.FLOAT, false);
        normalAccessor.SetData(bufferView, 80, generalRingCount, DimensionType.VEC3, EncodingType.FLOAT, false);
        thicknessAccessor.SetData(bufferView, 92, generalRingCount, DimensionType.SCALAR, EncodingType.FLOAT, false);

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

        treeIndexAccessor.SetData(bufferView, 0, nutCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        colorAccessor.SetData(bufferView, 4, nutCount, DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, false);
        instanceMatrixAccessor.SetData(bufferView, 8, nutCount, DimensionType.MAT4, EncodingType.FLOAT, false);

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

        treeIndexAccessor.SetData(bufferView, 0, quadCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        colorAccessor.SetData(bufferView, 4, quadCount, DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, false);
        instanceMatrixAccessor.SetData(bufferView, 8, quadCount, DimensionType.MAT4, EncodingType.FLOAT, false);

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

        treeIndexAccessor.SetData(bufferView, 0, torusCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        colorAccessor.SetData(bufferView, 4, torusCount, DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, false);
        arcAngleAccessor.SetData(bufferView, 8, torusCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        instanceMatrixAccessor.SetData(bufferView, 12, torusCount, DimensionType.MAT4, EncodingType.FLOAT, false);
        radiusAccessor.SetData(bufferView, 76, torusCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        tubeRadiusAccessor.SetData(bufferView, 80, torusCount, DimensionType.SCALAR, EncodingType.FLOAT, false);

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
        const int byteStride = (1 + 1 + 1 + 1 + 1 + 1) * sizeof(float); // id + color + matrix
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

        treeIndexAccessor.SetData(bufferView, 0, trapeziumCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        colorAccessor.SetData(bufferView, 4, trapeziumCount, DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, false);
        vertex1Accessor.SetData(bufferView, 8, trapeziumCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        vertex2Accessor.SetData(bufferView, 12, trapeziumCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        vertex3Accessor.SetData(bufferView, 16, trapeziumCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
        vertex4Accessor.SetData(bufferView, 20, trapeziumCount, DimensionType.SCALAR, EncodingType.FLOAT, false);

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
        // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Numerics/Vector4.cs
        // writes Vector4 memory byte layout directly to buffer
        var source = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref vector, 1));
        var target = buffer.Slice(bufferPos, sizeof(float) * 4);
        Debug.Assert(source.Length == target.Length);
        source.CopyTo(target);
        bufferPos += sizeof(float) * 4;
    }
}