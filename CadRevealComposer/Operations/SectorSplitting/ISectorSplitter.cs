namespace CadRevealComposer.Operations.SectorSplitting;

using Primitives;
using System.Collections.Generic;

public interface ISectorSplitter
{
    public IEnumerable<InternalSector> SplitIntoSectors(APrimitive[] allGeometries);
}
