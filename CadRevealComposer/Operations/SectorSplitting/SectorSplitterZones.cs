namespace CadRevealComposer.Operations;

using CadRevealComposer.IdProviders;
using CadRevealComposer.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static CadRevealComposer.Operations.SectorSplitter;

public static class SectorSplitterZones
{
    public static IEnumerable<ProtoSector> SplitIntoSectors(ZoneSplitter.Zone[] zones)
    {
        var sectorIdGenerator = new SequentialIdGenerator();

        foreach (var zone in zones)
        {
            var nodes = zone.Primitives
                .GroupBy(p => p.TreeIndex)
                .Select(g =>
                {
                    var geometries = g.ToArray();
                    var boundingBoxMin = geometries.GetBoundingBoxMin();
                    var boundingBoxMax = geometries.GetBoundingBoxMax();
                    return new Node(
                        g.Key,
                        geometries,
                        geometries.Sum(DrawCallEstimator.EstimateByteSize),
                        boundingBoxMin,
                        boundingBoxMax,
                        Vector3.Distance(boundingBoxMin, boundingBoxMax));
                })
                .ToArray();

            var sectors = SectorSplitter.SplitIntoSectorsRecursive(
                nodes,
                0,
                "",
                null,
                sectorIdGenerator).ToArray();

            foreach (var sector in sectors)
            {
                yield return sector;
            }
        }
    }
}
