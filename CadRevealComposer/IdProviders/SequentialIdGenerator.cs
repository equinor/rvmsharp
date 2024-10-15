namespace CadRevealComposer.IdProviders;

using System;
using System.Threading;

public class SequentialIdGenerator(uint firstIdReturned = 0)
{
    private static readonly uint MaxSafeIdForFloats = (uint)Math.Pow(2, 24) - 1; // Max sequential whole integer in a 32-bit float as used in reveal shaders.

    private uint _internalIdCounter = firstIdReturned; // It increments before selecting the id, hence -1

    public uint GetNextId()
    {
        var idToReturn = _internalIdCounter;
        _internalIdCounter++;
        if (idToReturn > MaxSafeIdForFloats)
            throw new Exception("Too many ids generated");
        return idToReturn;
    }

    /// <summary>
    /// Look at the next id that will be generated
    /// </summary>
    public uint PeekNextId => _internalIdCounter;
}
