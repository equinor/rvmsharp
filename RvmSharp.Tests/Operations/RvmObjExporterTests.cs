namespace RvmSharp.Tests.Operations;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Exporters;
using NUnit.Framework;
using RvmSharp.Containers;
using RvmSharp.Operations;
using RvmSharp.Primitives;
using Tessellation;

[TestFixture]
public class RvmObjExporterTests
{
    [Test]
    public void DoNotDuplicateMaterialsTest()
    {
        // create RvmStore
        var store = new RvmStore();
        var nodes = new List<RvmNode> { new RvmNode(1, "", Vector3.Zero, 1), new RvmNode(1, "", Vector3.Zero, 1) };
        nodes[0]
            .Children.Add(new RvmBox(1, Matrix4x4.Identity, new RvmBoundingBox(-Vector3.One, Vector3.One), 2, 2, 2));
        nodes[1]
            .Children.Add(new RvmBox(1, Matrix4x4.Identity, new RvmBoundingBox(-Vector3.One, Vector3.One), 2, 2, 2));
        store.RvmFiles.Add(
            new RvmFile(
                new RvmFile.RvmHeader(1, "", "", "", "", ""),
                new RvmModel(1, "", "", nodes, System.Array.Empty<RvmPrimitive>(), System.Array.Empty<RvmColor>())
            )
        );
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

    [Test]
    public void DoNotCreateMtlIfNoMaterial()
    {
        // create RvmStore
        var primitive = new RvmBox(1, Matrix4x4.Identity, new RvmBoundingBox(-Vector3.One, Vector3.One), 2, 2, 2);

        var mesh = TessellatorBridge.Tessellate(primitive, 0.01f);
        // export
        var tempFileName = Path.GetTempFileName();
        var matFileName = Path.ChangeExtension(tempFileName, "mtl");
        using (var objExporter = new ObjExporter(tempFileName))
        {
            objExporter.StartGroup("mesh");
            objExporter.WriteMesh(mesh);
        }

        var objContent = File.ReadLines(tempFileName);
        var materialRefCount = objContent.Count(s => s.StartsWith("usemtl "));
        var materialFileExists = File.Exists(matFileName);
        File.Delete(tempFileName);

        // check that mtl file was not created
        Assert.That(materialFileExists.Equals(false), "MTL file should not be created if no material is specified");
        // check material count
        Assert.That(materialRefCount.Equals(0), "OBJ Exporter should not reference non-existing material file");
    }

    [Test]
    public void TagNaming_UsesTagAndStrippedNodeName()
    {
        var store = new RvmStore();
        var parent = new RvmNode(1, "/3S_EL38820_DS-DRAIN", Vector3.Zero, 1);
        parent.Attributes["Tag"] = "3S EL38820 DS-DRAIN";

        var child = new RvmNode(1, "PANEL 2 of FRMWORK /3S_EL38820_DRAIN", Vector3.Zero, 1);
        child.Children.Add(
            new RvmBox(1, Matrix4x4.Identity, new RvmBoundingBox(-Vector3.One, Vector3.One), 2, 2, 2));
        parent.Children.Add(child);

        store.RvmFiles.Add(
            new RvmFile(
                new RvmFile.RvmHeader(1, "", "", "", "", ""),
                new RvmModel(1, "", "", new List<RvmNode> { parent },
                    System.Array.Empty<RvmPrimitive>(), System.Array.Empty<RvmColor>())));

        var tempFileName = Path.GetTempFileName();
        RvmObjExporter.ExportToObj(store, 0.01f, tempFileName, null, null, null, useTagNaming: true);
        var objContent = File.ReadAllLines(tempFileName);
        File.Delete(tempFileName);
        var mtlFile = Path.ChangeExtension(tempFileName, "mtl");
        if (File.Exists(mtlFile)) File.Delete(mtlFile);

        var objectLine = objContent.FirstOrDefault(s => s.StartsWith("o "));
        Assert.That(objectLine, Is.EqualTo("o 3S-EL38820-DS-DRAIN/PANEL-2-of-FRMWORK"));
    }

    [Test]
    public void TagNaming_FallsBackToNodeName_WhenNoTag()
    {
        var store = new RvmStore();
        var node = new RvmNode(1, "SomeNodeName", Vector3.Zero, 1);
        node.Children.Add(
            new RvmBox(1, Matrix4x4.Identity, new RvmBoundingBox(-Vector3.One, Vector3.One), 2, 2, 2));

        store.RvmFiles.Add(
            new RvmFile(
                new RvmFile.RvmHeader(1, "", "", "", "", ""),
                new RvmModel(1, "", "", new List<RvmNode> { node },
                    System.Array.Empty<RvmPrimitive>(), System.Array.Empty<RvmColor>())));

        var tempFileName = Path.GetTempFileName();
        RvmObjExporter.ExportToObj(store, 0.01f, tempFileName, null, null, null, useTagNaming: true);
        var objContent = File.ReadAllLines(tempFileName);
        File.Delete(tempFileName);
        var mtlFile = Path.ChangeExtension(tempFileName, "mtl");
        if (File.Exists(mtlFile)) File.Delete(mtlFile);

        var objectLine = objContent.FirstOrDefault(s => s.StartsWith("o "));
        Assert.That(objectLine, Is.EqualTo("o SomeNodeName"));
    }

    [Test]
    public void TagNaming_DeduplicatesWithRefNo()
    {
        var store = new RvmStore();
        var parent = new RvmNode(1, "/Parent", Vector3.Zero, 1);
        parent.Attributes["Tag"] = "MyTag";

        var child1 = new RvmNode(1, "PANEL 1 of FRMWORK /Parent", Vector3.Zero, 1);
        child1.Attributes["RefNo"] = "=100/200";
        child1.Children.Add(
            new RvmBox(1, Matrix4x4.Identity, new RvmBoundingBox(-Vector3.One, Vector3.One), 2, 2, 2));

        var child2 = new RvmNode(1, "PANEL 1 of FRMWORK /Parent", Vector3.Zero, 1);
        child2.Attributes["RefNo"] = "=100/300";
        child2.Children.Add(
            new RvmBox(1, Matrix4x4.Identity, new RvmBoundingBox(-Vector3.One, Vector3.One), 2, 2, 2));

        parent.Children.Add(child1);
        parent.Children.Add(child2);

        store.RvmFiles.Add(
            new RvmFile(
                new RvmFile.RvmHeader(1, "", "", "", "", ""),
                new RvmModel(1, "", "", new List<RvmNode> { parent },
                    System.Array.Empty<RvmPrimitive>(), System.Array.Empty<RvmColor>())));

        var tempFileName = Path.GetTempFileName();
        RvmObjExporter.ExportToObj(store, 0.01f, tempFileName, null, null, null, useTagNaming: true);
        var objContent = File.ReadAllLines(tempFileName);
        File.Delete(tempFileName);
        var mtlFile = Path.ChangeExtension(tempFileName, "mtl");
        if (File.Exists(mtlFile)) File.Delete(mtlFile);

        var objectLines = objContent.Where(s => s.StartsWith("o ")).ToArray();
        Assert.That(objectLines, Has.Length.EqualTo(2));
        Assert.That(objectLines[0], Is.EqualTo("o MyTag/PANEL-1-of-FRMWORK_100-200"));
        Assert.That(objectLines[1], Is.EqualTo("o MyTag/PANEL-1-of-FRMWORK_100-300"));
    }
}
