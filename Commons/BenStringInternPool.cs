namespace Commons;

using Ben.Collections.Specialized;
using System;

/// <summary>
/// String interning is used to avoid allocating new strings for each unique string in a metadata-set
/// </summary>
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
