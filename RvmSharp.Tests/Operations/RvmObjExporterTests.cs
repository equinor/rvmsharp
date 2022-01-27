namespace RvmSharp.Tests.Operations;

using Containers;
using NUnit.Framework;
using RvmSharp.Operations;
using RvmSharp.Primitives;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

[TestFixture]
public class RvmObjExporterTests
{
    [Test]
    public void DoNotDuplicateMaterialsTest()
    {
        // create RvmStore
        var store = new RvmStore();
        var nodes = new List<RvmNode>
        {
            new RvmNode(1,"", Vector3.Zero, 1),
            new RvmNode(1,"", Vector3.Zero, 1),
        };
        nodes[0].Children.Add(new RvmBox(1, Matrix4x4.Identity, new RvmBoundingBox(-Vector3.One, Vector3.One), 2, 2, 2));
        nodes[1].Children.Add(new RvmBox(1, Matrix4x4.Identity, new RvmBoundingBox(-Vector3.One, Vector3.One), 2, 2, 2));
        store.RvmFiles.Add(new RvmFile(new RvmFile.RvmHeader(1, "", "", "", "", ""),
            new RvmModel(1, "", "", nodes, System.Array.Empty<RvmPrimitive>(), System.Array.Empty<RvmColor>())));
        // export
        var tempFileName = Path.GetTempFileName();
        RvmObjExporter.ExportToObj(store, 0.01f, tempFileName, null, null);
        var objContent = File.ReadLines(tempFileName);
        var materialRefCount = objContent.Count(s => s.StartsWith("usemtl "));
        File.Delete(tempFileName);
        File.Delete(Path.ChangeExtension(tempFileName, "mtl"));

        // check material count
        Assert.That(materialRefCount.Equals(1), "OBJ Exporter should not introduce unnecessary usemtl directives");
    }

}