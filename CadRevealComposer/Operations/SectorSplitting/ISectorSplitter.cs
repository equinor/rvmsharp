namespace CadRevealComposer.Operations.SectorSplitting;

using System.Collections.Generic;
using IdProviders;
using Primitives;

public interface ISectorSplitter
{
    public IEnumerable<InternalSector> SplitIntoSectors(
        APrimitive[] allGeometries,
        SequentialIdGenerator sectorIdGenerator
    );
}
