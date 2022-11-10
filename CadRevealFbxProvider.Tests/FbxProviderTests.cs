namespace CadRevealFbxProvider.Tests;

using CadRevealComposer;
using CadRevealComposer.AlgebraExtensions;
using CadRevealComposer.Configuration;
using CadRevealComposer.IdProviders;
using CadRevealComposer.ModelFormatProvider;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;

[TestFixture]
public class FbxProviderTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void FbxImporterSdkInitTest()
    {
        var treeIndexGenerator = new TreeIndexGenerator();
        using var test = new FbxImporter();
    }

    [Test]
    public void FbxImporterLoadFileTest()
    {
        var treeIndexGenerator = new TreeIndexGenerator();
        using var test = new FbxImporter();
        var RootNode = test.LoadFile(@"E:\tmp\AQ110South-3DView.FBX");
        Iterate(RootNode, test);
    }

    private void Iterate(FbxImporter.FbxNode root, FbxImporter test)
    {
        Console.WriteLine(test.GetNodeName(root));
        var childCount = test.GetChildCount(root);
        var t = test.GetTransform(root);
        Console.WriteLine($"Pos: {t.posX}, {t.posY}, {t.posZ}");
        test.GetGeometricData(root);
        for (var i = 0; i < childCount; i++)
        {
            var child = test.GetChild(i, root);
            Iterate(child, test);
        }
    }

    private IEnumerable<CadRevealNode> IterateAndGenerate(FbxImporter.FbxNode node, TreeIndexGenerator gen,
        FbxImporter sdk,
        Dictionary<IntPtr, (Mesh, int)> valueTuples,
        List<APrimitive> meshdata)
    {

        var id = gen.GetNextId();
        List<APrimitive> geometries = new List<APrimitive>();
        var name = sdk.GetNodeName(node);
        var geom = sdk.GetGeometricData(node);
        var trans = sdk.GetTransform(node);
        var pos = new Vector3(trans.posX, trans.posY, trans.posZ);
        var rot = new Quaternion(trans.rotX, trans.rotY, trans.rotZ, trans.rotW);
        var sca = Vector3.One;//new Vector3(trans.scaleX, trans.scaleY, trans.scaleZ);
        Console.WriteLine($"Pos {pos}");
        Console.WriteLine($"Rot {rot}");
        Console.WriteLine($"Sca {sca}");
        var matrix = Matrix4x4.CreateScale(sca)
            * Matrix4x4.CreateFromQuaternion(rot)
            * Matrix4x4.CreateTranslation(pos);
        //var matrix =

        if (geom != null)
        {
            var ptr = geom.Value.Item2;
            if (valueTuples.ContainsKey(ptr))
            {
                var aprim = new InstancedMesh(valueTuples[ptr].Item2, valueTuples[ptr].Item1, matrix, id, Color.Aqua, new BoundingBox(pos + Vector3.One * -100, pos + Vector3.One * 100));
                geometries.Add(aprim);
                meshdata.Add(aprim);
            }
            else
            {

                var instanceId = valueTuples.Count;

                valueTuples.Add(ptr, (geom.Value.Item1, instanceId));
                var aprim = new InstancedMesh(valueTuples[ptr].Item2, valueTuples[ptr].Item1, matrix, id, Color.Aqua, new BoundingBox(pos +Vector3.One * -100, pos +Vector3.One * 100));

                geometries.Add(aprim);
                meshdata.Add(aprim);
                using ObjExporter exporter = new ObjExporter($"E:/{instanceId}.obj");
                exporter.StartGroup(name);
                exporter.WriteMesh(valueTuples[ptr].Item1);
                exporter.Dispose();
            }
        }

        yield return new CadRevealNode
        {
            TreeIndex = gen.GetNextId(),
            Name = name,
            Geometries = geometries.ToArray()
        };

        var childCount = sdk.GetChildCount(node);
        for (var i = 0; i < childCount; i++)
        {
            foreach (CadRevealNode cadRevealNode in IterateAndGenerate(sdk.GetChild(i, node), gen, sdk, valueTuples, meshdata))
            {
                yield return cadRevealNode;
            }
        }
    }

    [Test]
    public void SampleModel()
    {
        var treeIndexGenerator = new TreeIndexGenerator();
        using var test = new FbxImporter();
        var RootNode = test.LoadFile(@"E:\tmp\A6001-20A06.fbx");
        var lookupA = new Dictionary<IntPtr, (Mesh, int)>();
        List<APrimitive> geometriesToProcess = new List<APrimitive>();
        var nodesToProcess = IterateAndGenerate(RootNode, treeIndexGenerator, test, lookupA, geometriesToProcess).ToList();



        var outputDirectory = new DirectoryInfo(@"E:\tmp\lol");
        var modelParameters = new ModelParameters(new ProjectId(1), new ModelId(1), new RevisionId(1), new InstancingThreshold(1));
        var composerParameters = new ComposerParameters("", false, true, false);

        CadRevealComposerRunner.ProcessPrimitives(geometriesToProcess.ToArray(), outputDirectory, modelParameters, composerParameters, treeIndexGenerator);

        Console.WriteLine($"Export Finished. Wrote output files to \"{Path.GetFullPath(outputDirectory.FullName)}\"");
    }
}