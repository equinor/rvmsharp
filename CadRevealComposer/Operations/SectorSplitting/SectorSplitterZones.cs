namespace CadRevealComposer.Operations.SectorSplitting;

using IdProviders;
using Primitives;
using Utils;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class SectorSplitterZones : ISectorSplitter
{
    public IEnumerable<ProtoSector> SplitIntoSectors(APrimitive[] allGeometries)
    {

        var zones = ZoneSplitter.SplitIntoZones(allGeometries);

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


            var octreeSplitter = new SectorSplitterOctree();
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
