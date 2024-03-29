﻿namespace CadRevealComposer.IdProviders;

using System;
using System.Threading;

public class SequentialIdGenerator
{
    public const ulong MaxSafeInteger = (1L << 53) - 1;

    private ulong _internalIdCounter = ulong.MaxValue;

    public ulong GetNextId()
    {
        var candidate = Interlocked.Increment(ref _internalIdCounter);
        if (candidate > MaxSafeInteger)
            throw new Exception("Too many ids generated");
        return candidate;
    }

    public ulong CurrentMaxGeneratedIndex => Interlocked.Read(ref _internalIdCounter);
}
