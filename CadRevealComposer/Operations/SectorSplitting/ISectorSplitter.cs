namespace CadRevealComposer.Operations.SectorSplitting;

using CadRevealComposer.Primitives;
using System.Collections.Generic;
using static CadRevealComposer.Operations.SectorSplitting.SplittingUtils;

public interface ISectorSplitter
{
    public IEnumerable<ProtoSector> SplitIntoSectors(APrimitive[] allGeometries);
}
