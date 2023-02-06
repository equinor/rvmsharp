namespace CadRevealComposer.Operations.SectorSplitting;

using IdProviders;
using Primitives;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        foreach (var zone in zones)
        {
            var nodes = SplittingUtils.ConvertPrimitivesToNodes(zone.Primitives);

            var sectors = octreeSplitter.SplitIntoSectorsRecursive(
                nodes,
                0,
                "",
                null,
                sectorIdGenerator,
                0).ToArray();

            foreach (var sector in sectors)
            {
                yield return sector;
            }
        }
    }
}