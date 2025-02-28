namespace CadRevealComposer.IdProviders;

using System;

public class SequentialIdGenerator(uint firstIdReturned = 0)
{
    private protected const uint MaxSafeIdForReveal = 16_777_216; // 2^24 -- Max number of cells in a 4k texture (and max sequential numbers in a float32)

    private uint _internalIdCounter = firstIdReturned;

    public uint GetNextId()
    {
        var idToReturn = _internalIdCounter;
        _internalIdCounter++;
        if (idToReturn > MaxSafeIdForReveal)
            throw new Exception("Id overflow. Ids are not safe for reveal anymore. " + "Max is " + MaxSafeIdForReveal);
        return idToReturn;
    }

    /// <summary>
    /// Look at the next id that will be generated
    /// </summary>
    public uint PeekNextId => _internalIdCounter;
}
