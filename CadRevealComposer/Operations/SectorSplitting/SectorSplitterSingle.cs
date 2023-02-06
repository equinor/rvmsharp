namespace CadRevealComposer.Operations;

using CadRevealComposer.Primitives;
using CadRevealComposer.Utils;
using System.Collections.Generic;
using static CadRevealComposer.Operations.SectorSplitter;

public static class SectorSplitterSingle
{
    public static IEnumerable<ProtoSector> CreateSingleSector(APrimitive[] allGeometries)
    {
        yield return CreateRootSector(0, allGeometries);
    }

    private static ProtoSector CreateRootSector(uint sectorId, APrimitive[] geometries)
    {
        var bbMin = geometries.GetBoundingBoxMin();
        var bbMax = geometries.GetBoundingBoxMax();
        return new ProtoSector(
            sectorId,
            ParentSectorId: null,
            0,
            $"{sectorId}",
            geometries,
            bbMin,
            bbMax,
            bbMin,
            bbMax
        );
    }

}
