namespace RvmSharp.Exe;

using BatchUtils;
using Ben.Collections.Specialized;
using System;

public class BenPoolWrapper : ISharedInternPool
{
    private readonly IInternPool _internPool;
    public long Considered => _internPool.Considered;
    public long Added => _internPool.Added;
    public long Deduped => _internPool.Deduped;

    public BenPoolWrapper(IInternPool internPool)
    {
        _internPool = internPool;
    }

    public string Intern(ReadOnlySpan<char> key)
    {
        return _internPool.Intern(key);
    }
}