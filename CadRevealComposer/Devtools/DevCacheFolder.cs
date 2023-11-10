namespace CadRevealComposer.Devtools;

using Primitives;
using Protobuf;
using System;
using System.IO;
using System.Linq;
using Utils;

public class DevCacheFolder
{
    private const string DevcacheExtension = ".devcache";
    private readonly DirectoryInfo _cacheFolder;

    public DevCacheFolder(DirectoryInfo cacheFolder)
    {
        _cacheFolder = cacheFolder;
    }

    public void PrintStatsToConsole()
    {
        var cache = _cacheFolder.EnumerateFiles("*" + DevcacheExtension).ToArray();

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
        var expectedFilename = name + DevcacheExtension;
        var file = new FileInfo(Path.Combine(_cacheFolder.FullName, expectedFilename));
        return file;
    }

    public APrimitive[]? ReadDevCacheIfExists(DirectoryInfo inputDirectory)
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

    public void WriteToDevCacheIfExists(APrimitive[] geometriesToProcessArray, DirectoryInfo inputDirectory)
    {
        var file = GetCacheFileForInputDirectory(inputDirectory);
        using var writeStream = file.OpenWrite();
        ProtobufStateSerializer.WriteAPrimitiveStateToStream(writeStream, geometriesToProcessArray);
    }
}
