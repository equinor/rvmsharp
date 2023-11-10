namespace CadRevealComposer.Devtools;

using Primitives;
using Protobuf;
using System;
using System.IO;
using System.Linq;
using Utils;

public class DevPrimitiveCacheFolder
{
    private const string PrimitiveCacheExtension = ".primitivecache";
    private readonly DirectoryInfo _cacheFolder;

    public DevPrimitiveCacheFolder(DirectoryInfo cacheFolder)
    {
        _cacheFolder = cacheFolder;
    }

    public void PrintStatsToConsole()
    {
        var cache = _cacheFolder.EnumerateFiles("*" + PrimitiveCacheExtension).ToArray();

        var totalSizeBytes = cache.Sum(x => x.Length);
        var numCacheFiles = cache.Length;

        using (new TeamCityLogBlock("DevCache Stats"))
        {
            Console.WriteLine("Stats for DevDache folder at " + _cacheFolder.FullName);
            Console.WriteLine(
                $"Total Size of .devcache files: {totalSizeBytes / 1024 / 1024}MB. NumCacheFiles: {numCacheFiles}"
            );
        }
    }

    public FileInfo GetCacheFileForInputDirectory(DirectoryInfo inputDirectory)
    {
        var name = inputDirectory.Name;
        var expectedFilename = name + PrimitiveCacheExtension;
        var file = new FileInfo(Path.Combine(_cacheFolder.FullName, expectedFilename));
        return file;
    }

    /// <summary>
    /// Reads the best primitive-cache match.
    ///
    /// If there is none this will return null.
    /// </summary>
    public APrimitive[]? ReadPrimitiveCache(DirectoryInfo inputDirectory)
    {
        var file = GetCacheFileForInputDirectory(inputDirectory);
        if (!file.Exists)
            return null;

        try
        {
            using var readStream = file.OpenRead();
            return ProtobufStateSerializer.ReadAPrimitiveStateFromStream(readStream);
        }
        catch
        {
            Console.Error.WriteLine(
                $"Failed to deserialize the cache file at: \"{file.FullName}\" . The file is probably outdated. Delete the file manually to create a new one on next run."
            );
            throw; // Rethrows the caught exception
        }
    }

    /// <summary>
    /// Writes a new cache
    /// </summary>
    public void WriteToPrimitiveCache(APrimitive[] geometriesToProcessArray, DirectoryInfo inputDirectory)
    {
        var file = GetCacheFileForInputDirectory(inputDirectory);
        using var fileStream = file.Create(); // Create a new file -- replacing existing file if any
        ProtobufStateSerializer.WriteAPrimitiveStateToStream(fileStream, geometriesToProcessArray);
    }
}
