namespace CadRevealFbxProvider.BatchUtils;

using CadRevealFbxProvider.Utils;

using CadRevealComposer;
using CadRevealComposer.IdProviders;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;

using Commons;

using System;
using System.Collections.Generic;
using System.Drawing;
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
        InstanceIdGenerator instanceIdGenerator,
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

            var lookupA = new Dictionary<IntPtr, (Mesh, ulong)>();
            List<APrimitive> geometriesToProcess = new List<APrimitive>();
            var nodesToProcess = ConvertFbxNodesToCadRevealRecursive(
                rootNodeOfModel,
                treeIndexGenerator,
                instanceIdGenerator,
                fbxImporter,
                lookupA).ToList();

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

    

    public static IEnumerable<CadRevealNode> ConvertFbxNodesToCadRevealRecursive(FbxImporter.FbxNode node,
        TreeIndexGenerator treeIndexGenerator,
        InstanceIdGenerator instanceIdGenerator,
        FbxImporter fbxSdk,
        Dictionary<IntPtr, (Mesh templateMesh, ulong instanceId)> meshInstanceLookup)
    {
        var id = treeIndexGenerator.GetNextId();
        List<APrimitive> geometries = new List<APrimitive>();
        var name = fbxSdk.GetNodeName(node);
        var nodeGeometryPtr = fbxSdk.GetMeshGeometryPtr(node);
        var fbxTransform = fbxSdk.GetTransform(node);
        var transform = FbxTransformConverter.ToMatrix4x4(fbxTransform);

        if (nodeGeometryPtr != IntPtr.Zero)
        {
            if (meshInstanceLookup.TryGetValue(nodeGeometryPtr, out var instanceData))
            {
                var bb = instanceData.templateMesh.CalculateBoundingBox(transform);
                var instancedMeshCopy = new InstancedMesh(instanceData.instanceId, instanceData.templateMesh,
                    transform, id, Color.Aqua,
                    bb);
                geometries.Add(instancedMeshCopy);
            }
            else
            {
                var meshData = fbxSdk.GetGeometricData(node);
                if (meshData.HasValue)
                {
                    var mesh = meshData.Value.Mesh;
                    var meshPtr = meshData.Value.MeshPtr;
                    ulong instanceId = instanceIdGenerator.GetNextId();

                    var bb = mesh.CalculateBoundingBox(transform);

                    meshInstanceLookup.Add(meshPtr, (mesh, instanceId));
                    var instancedMesh = new InstancedMesh(instanceId, mesh,
                        transform,
                        id,
                        Color.Magenta, // Temp debug color to distinguish first Instance
                        bb);

                    geometries.Add(instancedMesh);
                    //using ObjExporter exporter = new ObjExporter($"E:/{instanceId}.obj");
                    //exporter.StartGroup(name);
                    //exporter.WriteMesh(valueTuples[ptr].Item1);
                    //exporter.Dispose();
                }
            }
        }

        yield return new CadRevealNode { TreeIndex = id, Name = name, Geometries = geometries.ToArray() };

        var childCount = fbxSdk.GetChildCount(node);
        for (var i = 0; i < childCount; i++)
        {
            var child = fbxSdk.GetChild(i, node);
            var childCadRevealNodes = ConvertFbxNodesToCadRevealRecursive(
                child,
                treeIndexGenerator,
                instanceIdGenerator,
                fbxSdk,
                meshInstanceLookup);
            foreach (CadRevealNode cadRevealNode in childCadRevealNodes)
            {
                yield return cadRevealNode;
            }
        }
    }
}