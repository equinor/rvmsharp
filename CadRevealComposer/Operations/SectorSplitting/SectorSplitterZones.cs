namespace CadRevealComposer.Operations.SectorSplitting;

using IdProviders;
using Primitives;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

public class SectorSplitterZones : ISectorSplitter
{
    private readonly DirectoryInfo _outputDirectory;

    public SectorSplitterZones(DirectoryInfo outputDirectory)
    {
        _outputDirectory = outputDirectory;
    }

    public IEnumerable<ProtoSector> SplitIntoSectors(APrimitive[] allGeometries)
    {
        var zones = ZoneSplitter.SplitIntoZones(allGeometries, _outputDirectory);

        var sectorIdGenerator = new SequentialIdGenerator();
        var octreeSplitter = new SectorSplitterOctree();

        var rootSectorId = (uint)sectorIdGenerator.GetNextId();
        var rootPath = "/0";

        var allnodes = SplittingUtils.ConvertPrimitivesToNodes(allGeometries);
        var boundingBox = allnodes.CalculateBoundingBox();
        yield return SplittingUtils.CreateRootSector(rootSectorId, rootPath, boundingBox, new BoundingBox(Vector3.Zero,Vector3.Zero));


        foreach (var zone in zones)
        {
            var nodes = SplittingUtils.ConvertPrimitivesToNodes(zone.Primitives);

            var sectors = octreeSplitter.SplitIntoSectorsRecursive(
                nodes,
                1,
                rootPath,
                rootSectorId,
                sectorIdGenerator,
                1).ToArray();

            foreach (var sector in sectors)
            {
                yield return sector;
            }
        }
    }
}