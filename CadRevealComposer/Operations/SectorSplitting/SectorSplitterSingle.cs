namespace CadRevealComposer.Operations.SectorSplitting;

using Primitives;
using System.Collections.Generic;
using System.Linq;
using Utils;

public class SectorSplitterSingle : ISectorSplitter
{
    public IEnumerable<InternalSector> SplitIntoSectors(APrimitive[] allGeometries)
    {
        yield return CreateRootSector(0, allGeometries);
    }

    private InternalSector CreateRootSector(uint sectorId, APrimitive[] geometries)
    {
        var bb = geometries.CalculateBoundingBox();
        return new InternalSector(
            sectorId,
            ParentSectorId: null,
            0,
            $"{sectorId}",
            geometries.Min(x => x.AxisAlignedBoundingBox.Diagonal),
            geometries.Max(x => x.AxisAlignedBoundingBox.Diagonal),
            geometries,
            bb,
            bb
        );
    }
}