namespace CadRevealComposer.Operations.SectorSplitting;

using CadRevealComposer.IdProviders;
using System.Collections.Generic;

public interface ISectorSplitter
{
    public IEnumerable<InternalSector> SplitIntoSectors(
        Node[] nodes,
        uint parentId,
        string parentPath,
        SequentialIdGenerator sectorIdGenerator
    );
}
