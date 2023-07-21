namespace CadRevealComposer.Operations.SectorSplitting;

using CadRevealComposer.IdProviders;
using Primitives;
using System.Collections.Generic;

public interface ISectorSplitter
{
    public IEnumerable<InternalSector> SplitIntoSectors(
        APrimitive[] allGeometries,
        SequentialIdGenerator sectorIdGenerator,
        bool generateRootSector
    );
}
