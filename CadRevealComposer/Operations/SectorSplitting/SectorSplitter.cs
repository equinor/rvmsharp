namespace CadRevealComposer.Operations.SectorSplitting;

using CadRevealComposer.IdProviders;
using CadRevealComposer.Primitives;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Linq;

public class SectorSplitter
{
    private SequentialIdGenerator _sectorIdGenerator;

    public SectorSplitter()
    {
        _sectorIdGenerator = new SequentialIdGenerator();
    }

    public InternalSector[] SplitIntoSectors(APrimitive[] allPrimitives)
    {
        var allSectors = new List<InternalSector>();

        var nodes = SplittingUtils.ConvertPrimitivesToNodes(allPrimitives);

        var rootId = (uint)_sectorIdGenerator.GetNextId();
        var rootPath = "/0";
        var boundingBoxEncapsulatingAllNodes = nodes.CalculateBoundingBox();

        var rootSector = CreateRootSector(rootId, rootPath, boundingBoxEncapsulatingAllNodes);
        allSectors.Add(rootSector);

        var (regularNodes, outlierNodes) = nodes.SplitNodesIntoRegularAndOutlierNodes(0.995f);
        var nodeGroupsByPriority = regularNodes.GroupBy(x => x.Priority).ToDictionary(g => g.Key, g => g.ToArray());

        if (outlierNodes.Any())
        {
            var outlierSectors = HandleOutliers(outlierNodes, rootId, rootPath);
            allSectors.AddRange(outlierSectors);
        }

        var octreeSplitter = new SectorSplitterOctree();
        var regularSectors = octreeSplitter.SplitIntoSectors(
            nodeGroupsByPriority[NodePriority.Default],
            rootId,
            rootPath,
            _sectorIdGenerator
        );
        allSectors.AddRange(regularSectors);

        var linearSplitter = new SectorSplitterLinear();
        var prioritizedSectors = linearSplitter.SplitIntoSectors(
            nodeGroupsByPriority[NodePriority.High],
            rootId,
            rootPath,
            _sectorIdGenerator
        );
        var prioritizedSectorsWithFlagSet = prioritizedSectors.Select(sector => sector with { Prioritized = true });

        allSectors.AddRange(prioritizedSectorsWithFlagSet);

        return allSectors.ToArray();
    }

    private InternalSector[] HandleOutliers(Node[] outlierNodes, uint parentId, string parentPath)
    {
        Console.WriteLine($"Warning, adding {outlierNodes.Length} outliers to special sector(s).");

        var outlierSectors = SplitWithOctree(outlierNodes, parentId, parentPath); // TODO: StartDepth? 20

        foreach (var sector in outlierSectors)
        {
            Console.WriteLine(
                $"Outlier-sector with id {sector.SectorId}, path {sector.Path}, {sector.Geometries.Length} geometries added at depth {sector.Depth}."
            );
        }

        return outlierSectors.ToArray();
    }

    private InternalSector[] SplitWithOctree(Node[] nodes, uint parentId, string parentPath)
    {
        var splitter = new SectorSplitterOctree();
        var sectors = splitter.SplitIntoSectors(nodes, parentId, parentPath, _sectorIdGenerator);
        return sectors.ToArray();
    }

    private InternalSector[] SplitWithLinear(APrimitive[] primitives, uint parentId, string parentPath)
    {
        throw new NotImplementedException();
    }

    private InternalSector CreateRootSector(uint sectorId, string path, BoundingBox subtreeBoundingBox)
    {
        return new InternalSector(sectorId, null, 0, path, 0, 0, Array.Empty<APrimitive>(), subtreeBoundingBox, null);
    }
}
