namespace CadRevealComposer.IdProviders;

using System;

/// <summary>
/// Generates sequential IDs starting from a specified number.
/// </summary>
public class SequentialIdGenerator(uint firstIdReturned = 0)
{
    private protected const uint MaxSafeIdForReveal = 16_777_216; // 2^24 -- Max number of cells in a 4k texture (and max sequential numbers in a float32)

    private uint _internalIdCounter = firstIdReturned;

    private readonly object _idLock = new object();

    /// <summary>
    /// Gets the next sequential ID, and increments the internal counter.
    /// This is thread-safe and ensures that IDs are unique across calls.
    /// </summary>
    public uint GetNextId()
    {
        lock (_idLock)
        {
            var idToReturn = _internalIdCounter;
            _internalIdCounter++;
            if (idToReturn > MaxSafeIdForReveal)
                throw new Exception($"Id overflow. Ids are not safe for reveal anymore. Max is {MaxSafeIdForReveal}");
            return idToReturn;
        }
    }

    /// <summary>
    /// Look at the next id that will be generated (without incrementing the counter).
    /// Note: If you call this method in a multi-threaded environment, it will not guarantee that the ID will not change before you call GetNextId().
    /// </summary>
    public uint PeekNextId
    {
        get
        {
            lock (_idLock)
            {
                return _internalIdCounter;
            }
        }
    }
}
