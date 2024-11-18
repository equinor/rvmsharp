namespace CadRevealComposer.Operations.SectorSplitting;

using System;
using System.Collections.Generic;
using System.Linq;
using IdProviders;
using Primitives;
using Utils;

public class SectorSplitterSingle : ISectorSplitter
{
    public IEnumerable<InternalSector> SplitIntoSectors(
        APrimitive[] allGeometries,
        SequentialIdGenerator sectorIdGenerator
    )
    {
        yield return CreateRootSector(sectorIdGenerator.GetNextId(), allGeometries);
    }

    private static InternalSector CreateRootSector(uint sectorId, APrimitive[] geometries)
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
