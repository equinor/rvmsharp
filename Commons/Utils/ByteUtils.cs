namespace Commons.Utils;

public static class ByteUtils
{
    /// <summary>
    /// Calculate megabytes from a given number of bytes
    /// </summary>
    /// <param name="bytes">number of bytes</param>
    /// <returns>Megabytes (MB)</returns>
    public static double BytesToMegabytes(long bytes)
    {
        return (bytes / 1024f) / 1024f;
    }
}
