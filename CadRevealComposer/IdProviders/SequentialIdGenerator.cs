namespace CadRevealComposer.IdProviders;

using System;

public class SequentialIdGenerator(uint firstIdReturned = 0)
{
    private static readonly uint MaxSafeIdForReveal = (uint)Math.Pow(2, 24); // Max number of cells in a 4k texture (in reveal)

    private uint _internalIdCounter = firstIdReturned; // It increments before selecting the id, hence -1

    public uint GetNextId()
    {
        var idToReturn = _internalIdCounter;
        _internalIdCounter++;
        if (idToReturn > MaxSafeIdForReveal)
            throw new Exception("Too many ids generated");
        return idToReturn;
    }

    /// <summary>
    /// Look at the next id that will be generated
    /// </summary>
    public uint PeekNextId => _internalIdCounter;
}
