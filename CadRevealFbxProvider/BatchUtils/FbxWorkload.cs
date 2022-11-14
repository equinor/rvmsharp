namespace CadRevealFbxProvider.BatchUtils;

using CadRevealFbxProvider;

using CadRevealComposer;
using CadRevealComposer.IdProviders;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;

using Commons;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;


public static class FbxWorkload
{
    public static (string fbxFilename, string? txtFilename)[] CollectWorkload(
        IReadOnlyCollection<string> filesAndFolders,
        string? filter = null)
    {
        var regexFilter = filter != null ? new Regex(filter) : null;
        var directories = filesAndFolders.Where(Directory.Exists).ToArray();
        var files = filesAndFolders.Where(File.Exists).ToArray();
        var missingInputs = filesAndFolders.Where(i => !files.Contains(i) && !directories.Contains(i)).ToArray();

        if (missingInputs.Any())
        {
            throw new FileNotFoundException(
                $"Missing file or folder: {Environment.NewLine}{string.Join(Environment.NewLine, missingInputs)}");
        }

        var inputFiles =
            directories.SelectMany(directory => Directory.GetFiles(directory, "*.fbx")) // Collect fbx files
                .Concat(directories.SelectMany(directory => Directory.GetFiles(directory, "*.txt"))) // Collect TXTs
                .Concat(files) // Append single files
                .Where(f => regexFilter == null || regexFilter.IsMatch(Path.GetFileName(f))) // Filter by regex
                .GroupBy(Path.GetFileNameWithoutExtension).ToArray(); // Group by filename (rvm, txt)

        var workload = (from filePair in inputFiles
            select filePair.ToArray()
            into filePairStatic
            let fbxFilename = filePairStatic.FirstOrDefault(f => f.ToLower().EndsWith(".fbx"))
            let txtFilename = filePairStatic.FirstOrDefault(f => f.ToLower().EndsWith(".txt"))
            select (fbxFilename, txtFilename)).ToArray();

        var result = new List<(string, string?)>();
        foreach ((string? fbxFilename, string? txtFilename) in workload)
        {
            if (fbxFilename == null)
            {
                Console.WriteLine(
                    $"No corresponding FBX file found for attributes: '{txtFilename}', the file will be skipped.");
            }
                
            else
                result.Add((fbxFilename, txtFilename));
        }

        return result.ToArray();
    }

    public static IReadOnlyList<CadRevealNode> ReadFbxData(
        IReadOnlyCollection<(string fbxFilename, string? txtFilename)> workload,
        TreeIndexGenerator treeIndexGenerator,
        IProgress<(string fileName, int progress, int total)>? progressReport = null,
        IStringInternPool? stringInternPool = null)
    {
        var progress = 0;
        var redundantPdmsAttributesToExclude = new[] { "Name", "Position" };

        IReadOnlyList<CadRevealNode> LoadFbxFile((string rvmFilename, string? txtFilename) filePair)
        {
            (string fbxFilename, string? infoTextFilename) = filePair;

            using var fbxImporter = new FbxImporter();

            var rootNodeOfModel = fbxImporter.LoadFile(fbxFilename);
            
            if (!string.IsNullOrEmpty(infoTextFilename))
            {
                // TODO
                // attach attributes if they exist !!
               // (...).AttachAttributes(txtFilename!, redundantPdmsAttributesToExclude, stringInternPool);
            }

            var lookupA = new Dictionary<IntPtr, (Mesh, int)>();
            List<APrimitive> geometriesToProcess = new List<APrimitive>();
            var nodesToProcess = IterateAndGenerate(rootNodeOfModel, treeIndexGenerator, fbxImporter, lookupA, geometriesToProcess).ToList();

            progressReport?.Report((Path.GetFileNameWithoutExtension(fbxFilename), ++progress, workload.Count));
            return nodesToProcess;
        }

        var fbxNodes = workload
            .AsParallel()
            .AsOrdered()
            .SelectMany(LoadFbxFile).ToArray();

        var fbxNodesFlat = fbxNodes.SelectMany(CadRevealNode.GetAllNodesFlat).ToArray();

        if (stringInternPool != null)
        {
            Console.WriteLine(
                $"{stringInternPool.Considered:N0} PDMS strings were deduped into {stringInternPool.Added:N0} string objects. Reduced string allocation by {(float)stringInternPool.Deduped / stringInternPool.Considered:P1}.");
        }

        //var rvmStore = new RvmStore();
        //rvmStore.RvmFiles.AddRange(fbxFiles);
        //progressReport?.Report(("Connecting geometry", 0, 2));
        //RvmConnect.Connect(rvmStore);
        //progressReport?.Report(("Aligning geometry", 1, 2));
        //RvmAlign.Align(rvmStore);
        //progressReport?.Report(("Import finished", 2, 2));
        return fbxNodesFlat;
    }

    public static IEnumerable<CadRevealNode> IterateAndGenerate(FbxImporter.FbxNode node, TreeIndexGenerator gen,
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
        var sca = new Vector3(trans.scaleX, trans.scaleY, trans.scaleZ);
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
                var aprim = new InstancedMesh(valueTuples[ptr].Item2, valueTuples[ptr].Item1, matrix, id, Color.Aqua, new BoundingBox(pos + Vector3.One * -100, pos + Vector3.One * 100));

                geometries.Add(aprim);
                meshdata.Add(aprim);
                //using ObjExporter exporter = new ObjExporter($"E:/{instanceId}.obj");
                //exporter.StartGroup(name);
                //exporter.WriteMesh(valueTuples[ptr].Item1);
                //exporter.Dispose();
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

}