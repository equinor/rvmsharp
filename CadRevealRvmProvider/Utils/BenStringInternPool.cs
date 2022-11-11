namespace CadRevealRvmProvider.Utils;

using Ben.Collections.Specialized;
using RvmSharp.BatchUtils;
using System;

public class BenStringInternPool : IStringInternPool
{
    private readonly IInternPool _internPool;
    public long Considered => _internPool.Considered;
    public long Added => _internPool.Added;
    public long Deduped => _internPool.Deduped;

    public BenStringInternPool(IInternPool internPool)
    {
        _internPool = internPool;
    }

    public string Intern(ReadOnlySpan<char> key)
    {
        return _internPool.Intern(key);
    }
}