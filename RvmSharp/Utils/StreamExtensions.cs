namespace RvmSharp.Utils;

using System;
using System.IO;

internal static class StreamExtensions
{
#if DOTNET7_0_OR_GREATER
    // 'ReadExactly' is implemented in Stream in .NET 7.0 and later
#else
    internal static void ReadExactly(this Stream stream, Span<byte> target)
    {
        if (stream.Read(target) != target.Length)
            throw new EndOfStreamException("Unexpected end of stream");
    }
#endif
}
