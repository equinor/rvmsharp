namespace CadRevealComposer.Operations.SectorSplitting;

using CadRevealComposer.IdProviders;
using System.Collections.Generic;

public class SectorSplitterSingle : ISectorSplitter
{
    public IEnumerable<InternalSector> SplitIntoSectors(
        Node[] nodes,
        uint parentId,
        string parentPath,
        SequentialIdGenerator sectorIdGenerator
    )
    {
        var sectorId = (uint)sectorIdGenerator.GetNextId();
        var boundingBox = nodes.CalculateBoundingBox();
        yield return SplittingUtils.CreateSector(nodes, sectorId, parentId, parentPath, 1, boundingBox);
    }
}
