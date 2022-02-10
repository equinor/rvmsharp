namespace RvmSharp.BatchUtils
{
    using System;

    /// <summary>
    /// Intern / reuse strings instead of allocating the same string multiple times.
    /// </summary>
    public interface IStringInternPool
    {
        long Considered { get; }
        long Added { get; }
        long Deduped { get; }
        string Intern(ReadOnlySpan<char> key);
    }
}