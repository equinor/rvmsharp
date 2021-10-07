namespace CadRevealComposer
{
    using Configuration;
    using HierarchyComposer.Functions;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Newtonsoft.Json;
    using Operations;
    using Primitives;
    using Primitives.Reflection;
    using RvmSharp.Primitives;
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using Utils;
    using Utils.Comparers;
    using Writers;

    public static class SceneCreator
    {
        private const int I3DFMagicBytes = 1178874697; // I3DF chars as bytes.

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
            RvmBoundingBox BoundingBox
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

        public static void WriteSceneFile(ImmutableArray<SectorInfo> sectors, ModelParameters parameters, DirectoryInfo outputDirectory, ulong maxTreeIndex)
        {
            static Sector FromSector(SectorInfo sector)
            {
                return new Sector
                {
                    Id = sector.SectorId,
                    ParentId = sector.ParentSectorId.HasValue
                        ? sector.ParentSectorId.Value
                        : -1,
                    BoundingBox =
                        new BoundingBox(
                            Min: new BbVector3(sector.BoundingBox.Min.X, sector.BoundingBox.Min.Y, sector.BoundingBox.Min.Z),
                            Max: new BbVector3(sector.BoundingBox.Max.X, sector.BoundingBox.Max.Y, sector.BoundingBox.Max.Z)
                        ),
                    Depth = sector.Depth,
                    Path = sector.Path,
                    IndexFile = new IndexFile(
                        FileName: sector.Filename,
                        DownloadSize: sector.DownloadSize,
                        PeripheralFiles: sector.PeripheralFiles),
                    FacesFile = null, // Not implemented
                    EstimatedTriangleCount = sector.EstimatedTriangleCount,
                    EstimatedDrawCallCount = sector.EstimatedDrawCallCount
                };
            }

            var scene = new Scene
            {
                Version = 8,
                ProjectId = parameters.ProjectId,
                ModelId = parameters.ModelId,
                RevisionId = parameters.RevisionId,
                SubRevisionId = -1,
                MaxTreeIndex = maxTreeIndex,
                Unit = "Meters",
                Sectors = sectors.Select(FromSector).ToArray()
            };

            var scenePath = Path.Join(outputDirectory.FullName, "scene.json");
            JsonUtils.JsonSerializeToFile(scene, scenePath, Formatting.Indented);
        }

        public static void ExportSector(SectorInfo sector, string outputDirectory)
        {
            var geometries = sector.Geometries;
            var groupAttributesTimer = Stopwatch.StartNew();
            Console.WriteLine("Start Group Attributes and create i3d file structure");

            var attributesTimer = Stopwatch.StartNew();
            Console.WriteLine($"Attribute Grouping: {attributesTimer.Elapsed}");

            var colors = ImmutableSortedSet<Color>.Empty;
            var diagonals = ImmutableSortedSet<float>.Empty;
            var centerX = ImmutableSortedSet<float>.Empty;
            var centerY = ImmutableSortedSet<float>.Empty;
            var centerZ = ImmutableSortedSet<float>.Empty;
            var normals = ImmutableSortedSet<Vector3>.Empty;
            var deltas = ImmutableSortedSet<float>.Empty;
            var heights = ImmutableSortedSet<float>.Empty;
            var radii = ImmutableSortedSet<float>.Empty;
            var angles = ImmutableSortedSet<float>.Empty;
            var translationsX = ImmutableSortedSet<float>.Empty;
            var translationsY = ImmutableSortedSet<float>.Empty;
            var translationsZ = ImmutableSortedSet<float>.Empty;
            var scalesX = ImmutableSortedSet<float>.Empty;
            var scalesY = ImmutableSortedSet<float>.Empty;
            var scalesZ = ImmutableSortedSet<float>.Empty;
            var fileIds = Array.Empty<ulong>();
            var textures = Array.Empty<Texture>();

            var getAttributeValuesTimer = Stopwatch.StartNew();
            foreach (var attributeKind in Enum.GetValues<I3dfAttribute.AttributeType>())
            {
                switch (attributeKind)
                {
                    case I3dfAttribute.AttributeType.Null:
                        // Intentionally ignored
                        break;
                    case I3dfAttribute.AttributeType.Color:
                        // ReSharper disable once RedundantTypeArgumentsOfMethod
                        colors = APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<Color>(
                            geometries, attributeKind, new RgbaColorComparer());
                        break;
                    case I3dfAttribute.AttributeType.Diagonal:
                        diagonals =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.CenterX:
                        centerX =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.CenterY:
                        centerY =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.CenterZ:
                        centerZ =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.Normal:
                        // ReSharper disable once RedundantTypeArgumentsOfMethod
                        normals =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<Vector3>(
                                geometries, attributeKind, new XyzVector3Comparer());
                        break;
                    case I3dfAttribute.AttributeType.Delta:
                        deltas =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.Height:
                        heights =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.Radius:
                        radii = APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                            geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.Angle:
                        angles = APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                            geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.TranslationX:
                        translationsX =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.TranslationY:
                        translationsY =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.TranslationZ:
                        translationsZ =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.ScaleX:
                        scalesX =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.ScaleY:
                        scalesY =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.ScaleZ:
                        scalesZ =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.FileId:
                        fileIds =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<ulong>(
                                geometries, attributeKind).ToArray();
                        break;
                    case I3dfAttribute.AttributeType.Texture:
                        textures = Array.Empty<Texture>();
                        break;
                    case I3dfAttribute.AttributeType.Ignore:
                        // AttributeType.Ignore are intentionally ignored, and not exported.
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(attributeKind), attributeKind, "Unexpected i3df attribute.");
                }
            }

            Console.WriteLine($"Retrieved all distinct attributes in: {getAttributeValuesTimer.Elapsed}");

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

            var file = new FileI3D
            {
                FileSector = new FileSector
                {
                    Header = new Header
                    {
                        // Constants
                        MagicBytes = I3DFMagicBytes,
                        FormatVersion = 8,
                        OptimizerVersion = 1,

                        // Arbitrary selected numbers
                        SectorId = sector.SectorId,
                        ParentSectorId = sector.ParentSectorId,
                        BboxMax = sector.BoundingBox.Max,
                        BboxMin = sector.BoundingBox.Min,
                        Attributes = new Attributes
                        {
                            Angle = angles,
                            CenterX = centerX,
                            CenterY = centerY,
                            CenterZ = centerZ,
                            Color = colors,
                            Normal = normals,
                            Delta = deltas,
                            Diagonal = diagonals,
                            ScaleX = scalesX,
                            ScaleY = scalesY,
                            ScaleZ = scalesZ,
                            TranslationX = translationsX,
                            TranslationY = translationsY,
                            TranslationZ = translationsZ,
                            Radius = radii,
                            FileId = fileIds,
                            Height = heights,
                            Texture = textures
                        }
                    },
                    PrimitiveCollections = primitiveCollections
                }
            };
            Console.WriteLine($"Group Attributes and create i3d file structure: {groupAttributesTimer.Elapsed}");

            var i3dTimer = Stopwatch.StartNew();
            var filepath = Path.Join(outputDirectory, $"sector_{file.FileSector.Header.SectorId}.i3d");
            using var i3dSectorFile = File.Create(filepath);
            I3dWriter.WriteSector(file.FileSector, i3dSectorFile);
            Console.WriteLine($"Finished writing i3d Sectors in {i3dTimer.Elapsed}");

            Console.WriteLine($"Total primitives {geometries.Count}/{PrimitiveCounter.pc}");
            Console.WriteLine($"Missing: {PrimitiveCounter.ToString()}");
        }
    }
}