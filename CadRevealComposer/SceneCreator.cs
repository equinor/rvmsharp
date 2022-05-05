namespace CadRevealComposer;

using Configuration;
using HierarchyComposer.Functions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Operations;
using Primitives;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Numerics;
using Utils;
using Writers;

public static class SceneCreator
{
    public record SectorInfo(
        uint SectorId,
        uint? ParentSectorId,
        long Depth,
        string Path,
        string Filename,
        string[] PeripheralFiles,
        long EstimatedTriangleCount,
        long EstimatedDrawCallCount,
        IReadOnlyList<APrimitive> Geometries,
        Vector3 BoundingBoxMin,
        Vector3 BoundingBoxMax
    )
    {
        public long DownloadSize { get; init; }
    };

    public static void ExportHierarchyDatabase(string databasePath, CadRevealNode[] allNodes)
    {
        var nodes = HierarchyComposerConverter.ConvertToHierarchyNodes(allNodes);

        ILogger<DatabaseComposer> databaseLogger = NullLogger<DatabaseComposer>.Instance;
        var exporter = new DatabaseComposer(databaseLogger);
        exporter.ComposeDatabase(nodes.ToList(), Path.GetFullPath(databasePath));
    }

    public static void WriteSceneFile(
        ImmutableArray<SectorInfo> sectors,
        ModelParameters parameters,
        DirectoryInfo outputDirectory,
        ulong maxTreeIndex,
        CameraPositioning.CameraPosition cameraPosition)
    {
        static Sector FromSector(SectorInfo sector, DirectoryInfo outputDirectory)
        {

            return new Sector
            {
                Id = sector.SectorId,
                ParentId = sector.ParentSectorId.HasValue // FIXME: not needed anymore?
                    ? sector.ParentSectorId.Value
                    : -1,
                BoundingBox =
                    new BoundingBox(
                        Min: new BbVector3(sector.BoundingBoxMin.X, sector.BoundingBoxMin.Y, sector.BoundingBoxMin.Z),
                        Max: new BbVector3(sector.BoundingBoxMax.X, sector.BoundingBoxMax.Y, sector.BoundingBoxMax.Z)
                    ),
                Depth = sector.Depth,
                Path = sector.Path,
                SectorFileName = $"{sector.SectorId}.glb",
                EstimatedTriangleCount = sector.EstimatedTriangleCount,
                EstimatedDrawCallCount = sector.EstimatedDrawCallCount,
                // FIXME diagonal data
                DownloadSize = 1000, //FIXME: right size
            };
        }

        var scene = new Scene
        {
            Version = 9,
            ProjectId = parameters.ProjectId,
            ModelId = parameters.ModelId,
            RevisionId = parameters.RevisionId,
            SubRevisionId = -1,
            MaxTreeIndex = maxTreeIndex,
            Unit = "Meters",
            Sectors = sectors.Select(s => FromSector(s, outputDirectory)).ToArray()
        };

        var cameraPath = Path.Join(outputDirectory.FullName, "initialCamera.json");
        var scenePath = Path.Join(outputDirectory.FullName, "scene.json");
        JsonUtils.JsonSerializeToFile(cameraPosition, cameraPath);
        JsonUtils.JsonSerializeToFile(scene, scenePath, Formatting.Indented);
    }

    public static void ExportSector(SectorInfo sector, string outputDirectory)
    {
        var geometries = sector.Geometries;

        var primitiveCollections = new PrimitiveCollections();
        foreach (var geometriesByType in geometries.GroupBy(g => g.GetType()))
        {
            var elementType = geometriesByType.Key;
            if (elementType == typeof(ProtoMesh) || elementType  == typeof(ProtoMeshFromFacetGroup)  || elementType == typeof(ProtoMeshFromPyramid))
                continue; // ProtoMesh is a temporary primitive, and should not be exported.
            var elements = geometriesByType.ToArray();

            var fieldInfo = primitiveCollections.GetType().GetFields()
                .First(pc => pc.FieldType.GetElementType() == elementType);
            var typedArray = Array.CreateInstance(elementType, elements.Length);
            Array.Copy(elements, typedArray, elements.Length);
            fieldInfo.SetValue(primitiveCollections, typedArray);
        }

        var filepath2 = Path.Join(outputDirectory, $"{sector.SectorId}.glb");
        using var gltfSectorFile = File.Create(filepath2);
        GltfWriter.WriteSector(geometries.ToArray(), gltfSectorFile);
    }
}