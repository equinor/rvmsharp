namespace CadRevealComposer.Operations.SectorSplitting;

using Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using Utils;

public class SectorSplitterSingle : ISectorSplitter
{
    public IEnumerable<InternalSector> SplitIntoSectors(
        APrimitive[] allGeometries,
        ulong sectorStartId = 0,
        long budgetDivider = 1
    )
    {
        yield return CreateRootSector(0, allGeometries);
    }

    private InternalSector CreateRootSector(uint sectorId, APrimitive[] geometries)
    {
        var bb = geometries.CalculateBoundingBox();
        if (bb == null)
        {
            throw new Exception("The bounding box of the root sector should never be null");
        }
        return new InternalSector(
            sectorId,
            ParentSectorId: null,
            0,
            $"{sectorId}",
            geometries.Min(x => x.AxisAlignedBoundingBox.Diagonal),
            geometries.Max(x => x.AxisAlignedBoundingBox.Diagonal),
            geometries,
            bb,
            null
        );
    }
}
