namespace CadRevealComposer.Operations.MeshSwitch;

using Primitives;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Tessellation;

public static class MeshSwitch
{
    private static string folderName = "Switches";
    private static string[] candidates =
    {
        "Standard Aluhak FS 0,50",
        "Standard Aluhak FS 2,00",
        "Standard Aluhak FS 3,00",
        "Ledger Beam Aluhak TB 1,20",
        "Ledger Beam Aluhak TB 1,60",
        "Plank Aluhak 0.23 PLU 0,23 X 3,00"
    };

    public static IReadOnlyCollection<CadRevealNode> SwitchMesh(
        IReadOnlyList<CadRevealNode> nodes,
        DirectoryInfo inputFolder
    )
    {
        var switchDirectory = new DirectoryInfo(inputFolder.FullName + $"/{folderName}");

        foreach (var candidate in candidates)
        {
            var searchPattern = string.Concat(candidate, "*.obj");
            var switchFiles = switchDirectory.GetFiles(searchPattern);
            var switchMesh = CreateMeshFromObj(switchFiles[0]);

            nodes = nodes
                .Select(node =>
                {
                    if (node.Name.StartsWith(candidate))
                    {
                        var geometry = node.Geometries[0];
                        if (geometry is InstancedMesh instance)
                        {
                            node.Geometries[0] = instance with { TemplateMesh = switchMesh };
                            return node;
                        }
                    }
                    return node;
                })
                .ToArray();
        }

        return nodes;
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
