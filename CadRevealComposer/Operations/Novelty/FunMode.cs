namespace CadRevealComposer.Operations.Novelty;

using Primitives;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using Tessellation;
using Utils;

public static class FunMode
{
    private static string folderName = "FunMode";
    private static string candicate = "/56-VS50";

    public static IReadOnlyList<CadRevealNode> SwitchPrimitives(
        IReadOnlyList<CadRevealNode> allNodes,
        DirectoryInfo inputFolder
    )
    {
        var funDirectory = new DirectoryInfo(inputFolder.FullName + $"/{folderName}");
        var files = funDirectory.GetFiles("*.obj");

        if (files.Length == 0)
        {
            return allNodes;
        }

        var funMesh = CreateMeshFromObj(files[0]);

        var funNodes = allNodes.Select(node =>
        {
            if (node.Name.Equals(candicate))
            {
                var nodes = CadRevealNode.GetAllNodesFlat(node);
                foreach (var n in nodes)
                {
                    n.Geometries = Array.Empty<APrimitive>();
                }

                var geometries = node.Geometries;

                var min = node.BoundingBoxAxisAligned.Min;
                var max = node.BoundingBoxAxisAligned.Max;
                var center = node.BoundingBoxAxisAligned.Center;

                var scale = (max - min) / 2f;
                var position = center;
                var rotation = Quaternion.Identity;

                var matrix =
                    Matrix4x4.CreateScale(scale)
                    * Matrix4x4.CreateFromQuaternion(rotation)
                    * Matrix4x4.CreateTranslation(position);

                var transformedVertices = funMesh.Vertices.Select(x => Vector3.Transform(x, matrix)).ToArray();

                var mesh = new Mesh(transformedVertices, funMesh.Indices, 0);
                var triangleMesh = new TriangleMesh(mesh, node.TreeIndex, Color.DimGray, node.BoundingBoxAxisAligned);

                node.Geometries = new APrimitive[] { triangleMesh };
                return node;
            }

            return node;
        });

        return funNodes.ToList().AsReadOnly();
    }

    private static Mesh CreateMeshFromObj(FileInfo file)
    {
        var vertices = new List<Vector3>();
        var indices = new List<uint>();

        using (StreamReader sr = file.OpenText())
        {
            var line = "";
            while ((line = sr.ReadLine()) != null)
            {
                if (line.StartsWith("v "))
                {
                    vertices.Add(GetVertex(line));
                }

                if (line.StartsWith("f "))
                {
                    indices.AddRange(GetIndices(line));
                }
            }
        }

        return new Mesh(vertices.ToArray(), indices.ToArray(), 0f);
    }

    private static Vector3 GetVertex(string textLine)
    {
        var split = textLine.Split(" ");
        return new Vector3(float.Parse(split[1]), float.Parse(split[2]), float.Parse(split[3]));
    }

    private static uint[] GetIndices(string textLine)
    {
        var split = textLine.Split(" ");

        if (split.Length == 5)
        {
            var rectangleIndices = new uint[]
            {
                uint.Parse(split[1].Split("/")[0]) - 1,
                uint.Parse(split[2].Split("/")[0]) - 1,
                uint.Parse(split[3].Split("/")[0]) - 1,
                uint.Parse(split[4].Split("/")[0]) - 1
            };

            var triangleIndices = new uint[]
            {
                rectangleIndices[0],
                rectangleIndices[1],
                rectangleIndices[2],
                rectangleIndices[0],
                rectangleIndices[2],
                rectangleIndices[3]
            };

            return triangleIndices;
        }
        else
        {
            var triangleIndices = new uint[]
            {
                uint.Parse(split[1].Split("/")[0]) - 1,
                uint.Parse(split[2].Split("/")[0]) - 1,
                uint.Parse(split[3].Split("/")[0]) - 1
            };

            return triangleIndices;
        }
    }
}
