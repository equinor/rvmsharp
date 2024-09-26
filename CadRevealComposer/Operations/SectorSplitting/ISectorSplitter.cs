namespace CadRevealComposer.Operations.SectorSplitting;

using System.Collections.Generic;
using Primitives;

public interface ISectorSplitter
{
    public IEnumerable<InternalSector> SplitIntoSectors(APrimitive[] allGeometries, ulong nextSectorId);
}
