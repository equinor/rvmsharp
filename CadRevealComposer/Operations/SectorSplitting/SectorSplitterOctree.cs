namespace CadRevealComposer.Operations.SectorSplitting;

using IdProviders;
using Primitives;
using RvmSharp.Primitives;
using RvmSharp.Tessellation;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Numerics;
using Tessellation;
using Utils;

public class SectorSplitterOctree : ISectorSplitter
{
    private const long SectorEstimatedByteSizeBudget = 2_000_000; // bytes, Arbitrary value
    private const long SectorEstimatedPrimitiveBudget = 5_000; // count, Arbitrary value
    private const float DoNotChopSectorsSmallerThanMetersInDiameter = 17.4f; // Arbitrary value
    private const float MinDiagonalSizeAtDepth_1 = 7; // arbitrary value for min size at depth 1
    private const float MinDiagonalSizeAtDepth_2 = 4; // arbitrary value for min size at depth 2
    private const float MinDiagonalSizeAtDepth_3 = 1.5f; // arbitrary value for min size at depth 3
    private const float SplitDetailsThreshold = 0.1f; // arbitrary value for splitting out details from last nodes
    private const int MinimumNumberOfSmallPartsBeforeSplitting = 1000;

    // SPIKE
    private int NumberOfInstancesThreshold = 0;

    private int _totalExtraTriangles = 0;
    private int _totalNumberOfQuads = 0;
    private int _totalNumberOfBoxes = 0;
    private int _totalNumberOfCylinders = 0;
    private int _totalNumberOfCones = 0;
    private int _totalNumberOfEccentricCones = 0;
    private int _totalNumberOfRings = 0;
    private int _totalNumberOfCircles = 0;
    private int _totalNumberOfEllipsoidSegments = 0;
    private int _totalNumberOfTorusSegment = 0;

    //

    public IEnumerable<InternalSector> SplitIntoSectors(APrimitive[] allGeometries)
    {
        var sectorIdGenerator = new SequentialIdGenerator();

        var allNodes = SplittingUtils.ConvertPrimitivesToNodes(allGeometries);
        var (regularNodes, outlierNodes) = allNodes.SplitNodesIntoRegularAndOutlierNodes(0.995f);
        var boundingBoxEncapsulatingAllNodes = allNodes.CalculateBoundingBox();
        var boundingBoxEncapsulatingMostNodes = regularNodes.CalculateBoundingBox();

        var rootSectorId = (uint)sectorIdGenerator.GetNextId();
        var rootPath = "/0";

        yield return CreateRootSector(rootSectorId, rootPath, boundingBoxEncapsulatingAllNodes);

        //Order nodes by diagonal size
        var sortedNodes = regularNodes.OrderByDescending(n => n.Diagonal).ToArray();

        var sectors = SplitIntoSectorsRecursive(
                sortedNodes,
                1,
                rootPath,
                rootSectorId,
                sectorIdGenerator,
                CalculateStartSplittingDepth(boundingBoxEncapsulatingMostNodes)
            )
            .ToArray();

        foreach (var sector in sectors)
        {
            yield return sector;
        }

        // Add outliers to special outliers sector
        var excludedOutliersCount = outlierNodes.Length;
        if (excludedOutliersCount > 0)
        {
            var boundingBoxEncapsulatingOutlierNodes = outlierNodes.CalculateBoundingBox();

            Console.WriteLine($"Warning, adding {excludedOutliersCount} outliers to special sector(s).");
            var outlierSectors = SplitIntoSectorsRecursive(
                    outlierNodes.ToArray(),
                    20, // Arbitrary depth for outlier sectors, just to ensure separation from the rest
                    rootPath,
                    rootSectorId,
                    sectorIdGenerator,
                    CalculateStartSplittingDepth(boundingBoxEncapsulatingOutlierNodes)
                )
                .ToArray();

            foreach (var sector in outlierSectors)
            {
                Console.WriteLine(
                    $"Outlier-sector with id {sector.SectorId}, path {sector.Path}, {sector.Geometries.Length} geometries added at depth {sector.Depth}."
                );
                yield return sector;
            }
        }

        Console.WriteLine($"###### Instance number threshold: {NumberOfInstancesThreshold}");
        Console.WriteLine($"###### Extra triangles: {_totalExtraTriangles}");
        Console.WriteLine($"###### Total number of Quads: {_totalNumberOfQuads}");
        Console.WriteLine($"###### Total number of Boxes: {_totalNumberOfBoxes}");
        Console.WriteLine($"###### Total number of Cones: {_totalNumberOfCones}");
        Console.WriteLine($"###### Total number of Cylinders: {_totalNumberOfCylinders}");
        Console.WriteLine($"###### Total number of Rings: {_totalNumberOfRings}");
        Console.WriteLine($"###### Total number of Circles: {_totalNumberOfCircles}");
        Console.WriteLine($"###### Total number of Ellipsoid segments: {_totalNumberOfEllipsoidSegments}");
        Console.WriteLine($"###### Total number of Torus segments: {_totalNumberOfTorusSegment}");
        Console.WriteLine($"###### Total number of Eccentric cones: {_totalNumberOfEccentricCones}");
    }

    private IEnumerable<InternalSector> SplitIntoSectorsRecursive(
        Node[] nodes,
        int recursiveDepth,
        string parentPath,
        uint? parentSectorId,
        SequentialIdGenerator sectorIdGenerator,
        int depthToStartSplittingGeometry
    )
    {
        /*
         * Recursively divides space into eight voxels of about equal size (each dimension X,Y,Z is divided in half).
         * Note: Voxels might have partial overlap, to place nodes that is between two sectors without duplicating the data.
         */

        if (nodes.Length == 0)
        {
            yield break;
        }

        var actualDepth = Math.Max(1, recursiveDepth - depthToStartSplittingGeometry + 1);

        var subtreeBoundingBox = nodes.CalculateBoundingBox();

        var mainVoxelNodes = Array.Empty<Node>();
        Node[] subVoxelNodes;

        if (recursiveDepth < depthToStartSplittingGeometry)
        {
            subVoxelNodes = nodes;
        }
        else
        {
            // fill main voxel according to budget
            var additionalMainVoxelNodesByBudget = GetNodesByBudget(
                    nodes.ToArray(),
                    SectorEstimatedByteSizeBudget,
                    actualDepth
                )
                .ToList();
            mainVoxelNodes = mainVoxelNodes.Concat(additionalMainVoxelNodesByBudget).ToArray();
            subVoxelNodes = nodes.Except(mainVoxelNodes).ToArray();
        }

        if (!subVoxelNodes.Any())
        {
            var lastSectors = HandleLastNodes(nodes, actualDepth, parentSectorId, parentPath, sectorIdGenerator);

            foreach (var sector in lastSectors)
            {
                yield return sector;
            }
        }
        else
        {
            string parentPathForChildren = parentPath;
            uint? parentSectorIdForChildren = parentSectorId;

            var geometries = mainVoxelNodes.SelectMany(n => n.Geometries).ToArray();

            // Should we keep empty sectors???? yes no?
            if (geometries.Any() || subVoxelNodes.Any())
            {
                var sectorId = (uint)sectorIdGenerator.GetNextId();

                yield return CreateSector(
                    mainVoxelNodes,
                    sectorId,
                    parentSectorId,
                    parentPath,
                    actualDepth,
                    subtreeBoundingBox
                );

                parentPathForChildren = $"{parentPath}/{sectorId}";
                parentSectorIdForChildren = sectorId;
            }

            var sizeOfSubVoxelNodes = subVoxelNodes.Sum(x => x.EstimatedByteSize);
            var subVoxelDiagonal = subVoxelNodes.CalculateBoundingBox().Diagonal;

            if (
                subVoxelDiagonal < DoNotChopSectorsSmallerThanMetersInDiameter
                || sizeOfSubVoxelNodes < SectorEstimatedByteSizeBudget
            )
            {
                var sectors = SplitIntoSectorsRecursive(
                    subVoxelNodes,
                    recursiveDepth + 1,
                    parentPathForChildren,
                    parentSectorIdForChildren,
                    sectorIdGenerator,
                    depthToStartSplittingGeometry
                );
                foreach (var sector in sectors)
                {
                    yield return sector;
                }

                yield break;
            }

            var voxels = subVoxelNodes
                .GroupBy(node => SplittingUtils.CalculateVoxelKeyForNode(node, subtreeBoundingBox))
                .OrderBy(x => x.Key)
                .ToImmutableList();

            foreach (var voxelGroup in voxels)
            {
                if (voxelGroup.Key == SplittingUtils.MainVoxel)
                {
                    throw new Exception(
                        "Main voxel should not appear here. Main voxel should be processed separately."
                    );
                }

                var sectors = SplitIntoSectorsRecursive(
                    voxelGroup.ToArray(),
                    recursiveDepth + 1,
                    parentPathForChildren,
                    parentSectorIdForChildren,
                    sectorIdGenerator,
                    depthToStartSplittingGeometry
                );
                foreach (var sector in sectors)
                {
                    yield return sector;
                }
            }
        }
    }

    /*
     * This method is intended to avoid the problem that we always fill leaf sectors to the brim with content.
     * This means that we can have a sector with both large and tiny parts. If this is the case we sometimes want
     * to avoid loading all the tiny parts until we are closer to the sector.
     */
    private IEnumerable<InternalSector> HandleLastNodes(
        Node[] nodes,
        int depth,
        uint? parentSectorId,
        string parentPath,
        SequentialIdGenerator sectorIdGenerator
    )
    {
        var sectorId = (uint)sectorIdGenerator.GetNextId();

        var smallNodes = nodes.Where(n => n.Diagonal < SplitDetailsThreshold).ToArray();
        var largeNodes = nodes.Where(n => n.Diagonal >= SplitDetailsThreshold).ToArray();

        var subtreeBoundingBox = nodes.CalculateBoundingBox();

        if (
            largeNodes.Length > 0
            && smallNodes.Length > MinimumNumberOfSmallPartsBeforeSplitting
            && smallNodes.Any(n => n.Diagonal > 0) // There can be nodes with diagonal = 0, no point in splitting if they're all 0
        )
        {
            yield return CreateSector(largeNodes, sectorId, parentSectorId, parentPath, depth, subtreeBoundingBox);

            var smallNodesSectorId = (uint)sectorIdGenerator.GetNextId();
            var smallNodesParentPath = $"{parentPath}/{sectorId}";

            yield return CreateSector(
                smallNodes,
                smallNodesSectorId,
                sectorId,
                smallNodesParentPath,
                depth + 1,
                smallNodes.CalculateBoundingBox()
            );
        }
        else
        {
            yield return CreateSector(nodes, sectorId, parentSectorId, parentPath, depth, subtreeBoundingBox);
        }
    }

    private InternalSector CreateSector(
        Node[] nodes,
        uint sectorId,
        uint? parentSectorId,
        string parentPath,
        int depth,
        BoundingBox subtreeBoundingBox
    )
    {
        var path = $"{parentPath}/{sectorId}";

        var minDiagonal = nodes.Any() ? nodes.Min(n => n.Diagonal) : 0;
        var maxDiagonal = nodes.Any() ? nodes.Max(n => n.Diagonal) : 0;
        var geometries = nodes.SelectMany(n => n.Geometries).ToArray();
        var geometryBoundingBox = geometries.CalculateBoundingBox();

        int primitivesThreshold = 100;
        float tessellatingTolerance = 0.05f;

        var numberOfCones = geometries.Count(g => g is Cone);
        var numberOfEccentricCone = geometries.Count(g => g is EccentricCone);
        var numberOfEllipsoidSegment = geometries.Count(g => g is EllipsoidSegment);
        var numberOfGeneralCylinder = geometries.Count(g => g is GeneralCylinder);
        var numberOfTorusSegment = geometries.Count(g => g is TorusSegment);
        var numberOfBoxes = geometries.Count(g => g is Box);
        var numberOfQuads = geometries.Count(g => g is Quad);
        var numberOfRings = geometries.Count(g => g is GeneralRing);
        var numberOfCircles = geometries.Count(g => g is Circle);

        _totalNumberOfCones += numberOfCones;
        _totalNumberOfEccentricCones += numberOfEccentricCone;
        _totalNumberOfEllipsoidSegments += numberOfEllipsoidSegment;
        _totalNumberOfCylinders += numberOfGeneralCylinder;
        _totalNumberOfTorusSegment += numberOfTorusSegment;
        _totalNumberOfBoxes += numberOfBoxes;
        _totalNumberOfQuads += numberOfQuads;
        _totalNumberOfRings += numberOfRings;
        _totalNumberOfCircles += numberOfCircles;

        if (numberOfCones > 0 && numberOfCones < primitivesThreshold)
        {
            Console.WriteLine($"######### Converting cones to mesh, found {numberOfCones} cones in the sector");
            geometries = geometries
                .Select(g => g is Cone cone ? ConvertToTriangleMesh(cone, tessellatingTolerance) : g)
                .ToArray();
        }

        if (numberOfEccentricCone > 0 && numberOfEccentricCone < primitivesThreshold)
        {
            Console.WriteLine(
                $"######### Converting eccentric cones to mesh, found {numberOfEccentricCone} eccentric cones in the sector"
            );
            geometries = geometries
                .Select(g => g is EccentricCone cone ? ConvertToTriangleMesh(cone, tessellatingTolerance) : g)
                .ToArray();
        }

        if (numberOfEllipsoidSegment > 0 && numberOfEllipsoidSegment < primitivesThreshold)
        {
            Console.WriteLine(
                $"######### Converting ellipsoid segments to mesh, found {numberOfEllipsoidSegment} ellipsoid segments in the sector"
            );
            geometries = geometries
                .Select(
                    g =>
                        g is EllipsoidSegment ellipsoidSegment
                            ? ConvertToTriangleMesh(ellipsoidSegment, tessellatingTolerance)
                            : g
                )
                .ToArray();
        }

        if (numberOfGeneralCylinder > 0 && numberOfGeneralCylinder < primitivesThreshold)
        {
            Console.WriteLine(
                $"######### Converting general cylinders to mesh, found {numberOfGeneralCylinder} general cylinders in the sector"
            );
            geometries = geometries
                .Select(g => g is GeneralCylinder cylinder ? ConvertToTriangleMesh(cylinder, tessellatingTolerance) : g)
                .ToArray();
        }

        // Boxes
        if (numberOfBoxes > 0 && numberOfGeneralCylinder < primitivesThreshold)
        {
            geometries = geometries
                .Select(g => g is Box box ? ConvertToTriangleMesh(box, tessellatingTolerance) : g)
                .ToArray();
        }

        // GeneralRings
        if (numberOfRings > 0 && numberOfRings < primitivesThreshold)
        {
            geometries = geometries
                .Select(g => g is GeneralRing ring ? ConvertToTriangleMesh(ring, tessellatingTolerance) : g)
                .ToArray();
        }

        // Circle
        if (numberOfCircles > 0 && numberOfCircles < primitivesThreshold)
        {
            geometries = geometries
                .Select(g => g is Circle circle ? ConvertToTriangleMesh(circle, tessellatingTolerance) : g)
                .ToArray();
        }

        // Quads
        if (numberOfQuads > 0 && numberOfQuads < primitivesThreshold)
        {
            geometries = geometries
                .Select(g => g is Quad quad ? ConvertToTriangleMesh(quad, tessellatingTolerance) : g)
                .ToArray();
        }

        if (numberOfTorusSegment > 0 && numberOfTorusSegment < primitivesThreshold)
        {
            Console.WriteLine(
                $"######### Converting torus segments to mesh, found {numberOfTorusSegment} torus segments in the sector"
            );
            geometries = geometries
                .Select(
                    g => g is TorusSegment torusSegment ? ConvertToTriangleMesh(torusSegment, tessellatingTolerance) : g
                )
                .ToArray();
        }

        // var instances = geometries.Where(g => g is InstancedMesh).GroupBy(i => ((InstancedMesh)i).InstanceId);
        //
        // var instanceKeyListToDrop = new List<ulong>();

        // int extraTriangles = 0;
        //
        // foreach (var instanceGroup in instances)
        // {
        //     if (instanceGroup.Count() < NumberOfInstancesThreshold)
        //     {
        //         // Extra triangles = the number of the triangles in the instance times number of converted minus the original template
        //         extraTriangles +=
        //             ((InstancedMesh)instanceGroup.First()).TemplateMesh.TriangleCount * (instanceGroup.Count() - 1);
        //         instanceKeyListToDrop.Add(instanceGroup.Key);
        //     }
        // }
        //
        // _totalExtraTriangles += extraTriangles;
        //
        // geometries = geometries
        //     .Select(g =>
        //     {
        //         if (g is InstancedMesh instanceMesh && instanceKeyListToDrop.Contains(instanceMesh.InstanceId))
        //         {
        //             return new TriangleMesh(
        //                 instanceMesh.TemplateMesh,
        //                 instanceMesh.TreeIndex,
        //                 instanceMesh.Color,
        //                 instanceMesh.AxisAlignedBoundingBox
        //             );
        //         }
        //
        //         return g;
        //     })
        //     .ToArray();

        return new InternalSector(
            sectorId,
            parentSectorId,
            depth,
            path,
            minDiagonal,
            maxDiagonal,
            geometries,
            subtreeBoundingBox,
            geometryBoundingBox
        );
    }

    private static InternalSector CreateRootSector(uint sectorId, string path, BoundingBox subtreeBoundingBox)
    {
        return new InternalSector(sectorId, null, 0, path, 0, 0, Array.Empty<APrimitive>(), subtreeBoundingBox, null);
    }

    private TriangleMesh ConvertToTriangleMesh(Cone cone, float tolerance)
    {
        var matrix =
            Matrix4x4.CreateScale(1f)
            * Matrix4x4.CreateFromQuaternion(cone.Rotation)
            * Matrix4x4.CreateTranslation(cone.AxisAlignedBoundingBox.Center);

        var bb = new RvmBoundingBox(cone.AxisAlignedBoundingBox.Min, cone.AxisAlignedBoundingBox.Max);

        var rvmCylinder = new RvmCylinder(1, matrix, bb, cone.RadiusA, Vector3.Distance(cone.CenterB, cone.CenterA));

        var result = TessellatorBridge.Tessellate(rvmCylinder, tolerance);

        if (result == null)
            throw new Exception();

        return ConvertRvmMeshToTriangleMesh(result, cone);
    }

    private TriangleMesh ConvertToTriangleMesh(EccentricCone cone, float tolerance)
    {
        var matrix =
            Matrix4x4.CreateScale(1f)
            * Matrix4x4.CreateFromQuaternion(Quaternion.Identity)
            * Matrix4x4.CreateTranslation(cone.AxisAlignedBoundingBox.Center);

        var bb = new RvmBoundingBox(cone.AxisAlignedBoundingBox.Min, cone.AxisAlignedBoundingBox.Max);

        var rvmCylinder = new RvmCylinder(1, matrix, bb, cone.RadiusA, Vector3.Distance(cone.CenterA, cone.CenterB));
        var result = TessellatorBridge.Tessellate(rvmCylinder, tolerance);

        if (result == null)
            throw new Exception();

        return ConvertRvmMeshToTriangleMesh(result, cone);
    }

    private TriangleMesh ConvertToTriangleMesh(EllipsoidSegment ellipsoidSegment, float tolerance)
    {
        var matrix =
            Matrix4x4.CreateScale(1f)
            * Matrix4x4.CreateFromQuaternion(Quaternion.Identity)
            * Matrix4x4.CreateTranslation(ellipsoidSegment.Center);

        var bb = new RvmBoundingBox(
            ellipsoidSegment.AxisAlignedBoundingBox.Min,
            ellipsoidSegment.AxisAlignedBoundingBox.Max
        );

        var rvmSphere = new RvmSphere(1, matrix, bb, ellipsoidSegment.HorizontalRadius);
        var result = TessellatorBridge.Tessellate(rvmSphere, tolerance);

        if (result == null)
            throw new Exception();

        return ConvertRvmMeshToTriangleMesh(result, ellipsoidSegment);
    }

    private TriangleMesh ConvertToTriangleMesh(GeneralCylinder cylinder, float tolerance)
    {
        var matrix =
            Matrix4x4.CreateScale(1f)
            * Matrix4x4.CreateFromQuaternion(Quaternion.Identity)
            * Matrix4x4.CreateTranslation(cylinder.AxisAlignedBoundingBox.Center);

        var bb = new RvmBoundingBox(cylinder.AxisAlignedBoundingBox.Min, cylinder.AxisAlignedBoundingBox.Max);

        var rvmCylinder = new RvmCylinder(
            1,
            matrix,
            bb,
            cylinder.Radius,
            Vector3.Distance(cylinder.CenterA, cylinder.CenterB)
        );
        var result = TessellatorBridge.Tessellate(rvmCylinder, tolerance);

        if (result == null)
            throw new Exception();

        return ConvertRvmMeshToTriangleMesh(result, cylinder);
    }

    private TriangleMesh ConvertToTriangleMesh(TorusSegment torus, float tolerance)
    {
        var matrix =
            Matrix4x4.CreateScale(1f)
            * Matrix4x4.CreateFromQuaternion(Quaternion.Identity)
            * Matrix4x4.CreateTranslation(torus.AxisAlignedBoundingBox.Center);

        var bb = new RvmBoundingBox(torus.AxisAlignedBoundingBox.Min, torus.AxisAlignedBoundingBox.Max);

        // Gets stuck
        // var rvmCircularTorus = new RvmCircularTorus(1, matrix, bb, 0, torus.Radius, torus.ArcAngle);
        // var result = TessellatorBridge.Tessellate(rvmCircularTorus, tolerance);

        var rvmBox = new RvmBox(1, matrix, bb, 1, 1, 1);
        var result = TessellatorBridge.Tessellate(rvmBox, tolerance);

        if (result == null)
            throw new Exception();

        return ConvertRvmMeshToTriangleMesh(result, torus);
    }

    private TriangleMesh ConvertToTriangleMesh(Box box, float tolerance)
    {
        var matrix =
            Matrix4x4.CreateScale(1f)
            * Matrix4x4.CreateFromQuaternion(Quaternion.Identity)
            * Matrix4x4.CreateTranslation(box.AxisAlignedBoundingBox.Center);

        var bb = new RvmBoundingBox(box.AxisAlignedBoundingBox.Min, box.AxisAlignedBoundingBox.Max);

        var rvmBox = new RvmBox(1, matrix, bb, 1, 1, 1);

        var result = TessellatorBridge.Tessellate(rvmBox, tolerance);

        if (result == null)
            throw new Exception();

        return ConvertRvmMeshToTriangleMesh(result, box);
    }

    private TriangleMesh ConvertToTriangleMesh(GeneralRing ring, float tolerance)
    {
        var matrix =
            Matrix4x4.CreateScale(1f)
            * Matrix4x4.CreateFromQuaternion(Quaternion.Identity)
            * Matrix4x4.CreateTranslation(ring.AxisAlignedBoundingBox.Center);

        var bb = new RvmBoundingBox(ring.AxisAlignedBoundingBox.Min, ring.AxisAlignedBoundingBox.Max);

        var rvmBox = new RvmBox(1, matrix, bb, 1, 1, 1);

        var result = TessellatorBridge.Tessellate(rvmBox, tolerance);

        if (result == null)
            throw new Exception();

        return ConvertRvmMeshToTriangleMesh(result, ring);
    }

    private TriangleMesh ConvertToTriangleMesh(Circle circle, float tolerance)
    {
        var matrix =
            Matrix4x4.CreateScale(1f)
            * Matrix4x4.CreateFromQuaternion(Quaternion.Identity)
            * Matrix4x4.CreateTranslation(circle.AxisAlignedBoundingBox.Center);

        var bb = new RvmBoundingBox(circle.AxisAlignedBoundingBox.Min, circle.AxisAlignedBoundingBox.Max);

        var rvmBox = new RvmBox(1, matrix, bb, 1, 1, 1);

        var result = TessellatorBridge.Tessellate(rvmBox, tolerance);

        if (result == null)
            throw new Exception();

        return ConvertRvmMeshToTriangleMesh(result, circle);
    }

    private TriangleMesh ConvertToTriangleMesh(Quad quad, float tolerance)
    {
        var matrix =
            Matrix4x4.CreateScale(1f)
            * Matrix4x4.CreateFromQuaternion(Quaternion.Identity)
            * Matrix4x4.CreateTranslation(quad.AxisAlignedBoundingBox.Center);

        var bb = new RvmBoundingBox(quad.AxisAlignedBoundingBox.Min, quad.AxisAlignedBoundingBox.Max);

        var rvmBox = new RvmBox(1, matrix, bb, 1, 1, 1);

        var result = TessellatorBridge.Tessellate(rvmBox, tolerance);

        if (result == null)
            throw new Exception();

        return ConvertRvmMeshToTriangleMesh(result, quad);
    }

    private TriangleMesh ConvertRvmMeshToTriangleMesh(RvmMesh rvmMesh, APrimitive primitive)
    {
        var mesh = new Mesh(rvmMesh.Vertices, rvmMesh.Triangles, rvmMesh.Error);
        var triangleMesh = new TriangleMesh(
            mesh,
            primitive.TreeIndex,
            primitive.Color,
            primitive.AxisAlignedBoundingBox
        );

        return triangleMesh;
    }

    private static int CalculateStartSplittingDepth(BoundingBox boundingBox)
    {
        // If we start splitting too low in the octree, we might end up with way too many sectors
        // If we start splitting too high, we might get some large sectors with a lot of data, which always will be prioritized

        var diagonalAtDepth = boundingBox.Diagonal;
        int depth = 1;
        // Todo: Arbitrary numbers in this method based on gut feeling.
        // Assumes 3 levels of "LOD Splitting":
        // 300x300 for Very large parts
        // 150x150 for large parts
        // 75x75 for > 1 meter parts
        // 37,5 etc by budget
        const float level1SectorsMaxDiagonal = 500;
        while (diagonalAtDepth > level1SectorsMaxDiagonal)
        {
            diagonalAtDepth /= 2;
            depth++;
        }

        Console.WriteLine(
            $"Diagonal was: {boundingBox.Diagonal:F2}m. Starting splitting at depth {depth}. Expecting a diagonal of maximum {diagonalAtDepth:F2}m"
        );
        return depth;
    }

    private static IEnumerable<Node> GetNodesByBudget(IReadOnlyList<Node> nodes, long budget, int actualDepth)
    {
        var selectedNodes = actualDepth switch
        {
            1 => nodes.Where(x => x.Diagonal >= MinDiagonalSizeAtDepth_1).ToArray(),
            2 => nodes.Where(x => x.Diagonal >= MinDiagonalSizeAtDepth_2).ToArray(),
            3 => nodes.Where(x => x.Diagonal >= MinDiagonalSizeAtDepth_3).ToArray(),
            _ => nodes.ToArray(),
        };

        var nodesInPrioritizedOrder = selectedNodes.OrderByDescending(x => x.Diagonal);

        var budgetLeft = budget;
        var nodeArray = nodesInPrioritizedOrder.ToArray();
        var primitiveBudget = SectorEstimatedPrimitiveBudget;
        for (int i = 0; i < nodeArray.Length; i++)
        {
            if (budgetLeft < 0 || primitiveBudget <= 0 && nodeArray.Length - i > 10)
            {
                yield break;
            }

            var node = nodeArray[i];
            budgetLeft -= node.EstimatedByteSize;
            primitiveBudget -= node.Geometries.Count(x => x is not (InstancedMesh or TriangleMesh));
            yield return node;
        }
    }
}
