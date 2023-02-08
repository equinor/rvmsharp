namespace CadRevealComposer.Operations.SectorSplitting;

using Primitives;
using Utils;
using System.Collections.Generic;

public class SectorSplitterSingle : ISectorSplitter
{
    public IEnumerable<ProtoSector> SplitIntoSectors(APrimitive[] allGeometries)
    {
        yield return CreateRootSector(0, allGeometries);
    }

    private ProtoSector CreateRootSector(uint sectorId, APrimitive[] geometries)
    {
        var bb = geometries.CalculateBoundingBox();
        return new ProtoSector(
            sectorId,
            ParentSectorId: null,
            0,
            $"{sectorId}",
            geometries,
            bb,
            bb
        );
    }
}