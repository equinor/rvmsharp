namespace CadRevealComposer.IdProviders;

using System;
using System.Threading;

public class SequentialIdGenerator(uint firstIdReturned = 0)
{
    public static readonly uint MaxSafeInteger = (uint)Math.Pow(2, 24) - 1; // Max sequential whole integer in a 32-bit float as used in reveal shaders.

    private long _internalIdCounter = ((long)firstIdReturned) - 1; // It increments before selecting the id, hence -1

    public uint GetNextId()
    {
        var candidate = Interlocked.Increment(ref _internalIdCounter);
        if (candidate > MaxSafeInteger)
            throw new Exception("Too many ids generated");
        return (uint)candidate;
    }

    public uint CurrentMaxGeneratedIndex => (uint)Interlocked.Read(ref _internalIdCounter);
}
