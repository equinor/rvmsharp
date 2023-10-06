namespace CadRevealObjProvider;

using CadRevealComposer;
using CadRevealComposer.Configuration;
using CadRevealComposer.IdProviders;
using CadRevealComposer.ModelFormatProvider;
using CadRevealComposer.Operations;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using ObjLoader.Loader.Data.Elements;
using ObjLoader.Loader.Data.VertexData;
using ObjLoader.Loader.Loaders;
using System.Drawing;
using System.Numerics;

public class ObjProvider : IModelFormatProvider
{
    public (IReadOnlyList<CadRevealNode>, ModelMetadata?) ParseFiles(
        IEnumerable<FileInfo> filesToParse,
        TreeIndexGenerator treeIndexGenerator,
        InstanceIdGenerator instanceIdGenerator,
        NodeNameFiltering nodeNameFiltering
    )
    {
        var objLoaderFactory = new ObjLoaderFactory();
        var objLoader = objLoaderFactory.Create();
        var meshes = new List<ObjMesh>();
        foreach (
            FileInfo filePath in filesToParse.Where(x => x.Extension.Equals(".obj", StringComparison.OrdinalIgnoreCase))
        )
        {
            using var objFileStream = filePath.OpenRead();
            var result = objLoader.Load(objFileStream);
            foreach (Group group in result.Groups)
            {
                var mesh = ReadMeshFromGroup(group, result);
                if (mesh == null)
                    continue;
                meshes.Add(mesh);
            }
        }

        var nodes = new List<CadRevealNode>();
        foreach (ObjMesh meshGroup in meshes)
        {
            var treeIndex = treeIndexGenerator.GetNextId();
            if (nodeNameFiltering.ShouldExcludeNode(meshGroup.Name))
                continue;
            nodes.Add(
                new CadRevealNode()
                {
                    BoundingBoxAxisAligned = meshGroup.CalculateBoundingBox(),
                    Children = null,
                    TreeIndex = treeIndex,
                    Parent = null,
                    Name = meshGroup.Name,
                    Geometries = ConvertObjMeshToAPrimitive(meshGroup, treeIndex)
                }
            );
        }

        return (nodes, null);
    }

    private static APrimitive[] ConvertObjMeshToAPrimitive(ObjMesh mesh, ulong treeIndex)
    {
        return new APrimitive[]
        {
            // Reveal does not use normals, so we discard them here.
            new TriangleMesh(
                new Mesh(mesh.Vertices, mesh.Triangles, 0),
                treeIndex,
                Color.Magenta /* TODO: Add color support */
                ,
                mesh.CalculateBoundingBox()
            )
        };
    }

    public APrimitive[] ProcessGeometries(
        APrimitive[] geometries,
        ComposerParameters composerParameters,
        ModelParameters modelParameters,
        InstanceIdGenerator instanceIdGenerator
    )
    {
        return geometries;
    }

    public record ObjMesh
    {
        public string Name { get; init; } = "";
        public uint[] Triangles { get; init; } = Array.Empty<uint>();
        public Vector3[] Vertices { get; init; } = Array.Empty<Vector3>();

        public Vector3[] Normals { get; init; } = Array.Empty<Vector3>();

        // public int[] ColorIndices { get; set; }
        // public Color[] Colors { get; set; }

        public BoundingBox CalculateBoundingBox()
        {
            var min = Vertices.Aggregate(Vector3.Min);
            var max = Vertices.Aggregate(Vector3.Max);
            return new BoundingBox(Min: min, Max: max);
        }
    }

    private record VertexData(Vector3 Vertex, Vector3 Normal);

    private static ObjMesh? ReadMeshFromGroup(Group group, LoadResult result)
    {
        var index = 0u;
        var vertexData = new List<VertexData>();
        var triangles = new List<uint>();
        foreach (Face groupFace in group.Faces)
        {
            Vector3? generatedNormal = null;
            if (groupFace[0].NormalIndex == 0)
            {
                // Calculate/Generate normal for face
                var p1 = ToVector3(result.Vertices[groupFace[0].VertexIndex - 1]);
                var p2 = ToVector3(result.Vertices[groupFace[1].VertexIndex - 1]);
                var p3 = ToVector3(result.Vertices[groupFace[2].VertexIndex - 1]);

                // Calculate normals =
                var u = p2 - p1;
                var v = p3 - p1;

                generatedNormal = Vector3.Cross(u, v);
                generatedNormal = Vector3.Normalize(generatedNormal.Value);
            }

            var i1 = groupFace[0];
            vertexData.Add(
                new VertexData(
                    ToVector3(result.Vertices[i1.VertexIndex - 1]),
                    generatedNormal ?? ToVector3(result.Normals[i1.NormalIndex - 1])
                )
            );
            triangles.Add(index++);
            var i2 = groupFace[1];
            vertexData.Add(
                new VertexData(
                    ToVector3(result.Vertices[i2.VertexIndex - 1]),
                    generatedNormal ?? ToVector3(result.Normals[i2.NormalIndex - 1])
                )
            );
            triangles.Add(index++);
            var i3 = groupFace[2];
            vertexData.Add(
                new VertexData(
                    ToVector3(result.Vertices[i3.VertexIndex - 1]),
                    generatedNormal ?? ToVector3(result.Normals[i3.NormalIndex - 1])
                )
            );
            triangles.Add(index++);
        }

        var mesh = new ObjMesh()
        {
            Name = group.Name,
            Normals = vertexData.Select(x => x.Normal).ToArray(),
            Vertices = vertexData.Select(x => x.Vertex).ToArray(),
            Triangles = triangles.ToArray()
        };

        if (mesh.Vertices.Length == 0)
            return null;

        return mesh;
    }

    private static Vector3 ToVector3(Vertex x)
    {
        return new Vector3(x.X, x.Y, x.Z);
    }

    private static Vector3 ToVector3(Normal x)
    {
        return new Vector3(x.X, x.Y, x.Z);
    }
}
