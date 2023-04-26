namespace CadRevealComposer.Operations.SectorSplitting;

using Primitives;
using System;
using System.Collections.Generic;

public interface ISectorSplitter
{
    public IEnumerable<InternalSector> SplitIntoSectors((APrimitive, int)[] allGeometries);
}
