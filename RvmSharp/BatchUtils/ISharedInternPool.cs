namespace RvmSharp.BatchUtils
{
    using System;

    public interface ISharedInternPool
    {
        long Considered { get; }
        long Added { get; }
        long Deduped { get; }
        string Intern(ReadOnlySpan<char> key);
    }
}