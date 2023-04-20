// unset

namespace RvmSharp.Tests;

using NUnit.Framework;
using System.IO;

public static class TestFileHelpers
{
    private static readonly DirectoryInfo TestSamplesDirectory = new DirectoryInfo(
        Path.GetFullPath(Path.Join(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "TestSamples"))
    );

    public static readonly string BasicRvmTestFile = Path.Combine(
        TestSamplesDirectory.FullName,
        "Huldra",
        "WD1-PSUP.RVM"
    );
    public static readonly string BasicTxtAttTestFile = Path.Combine(
        TestSamplesDirectory.FullName,
        "Huldra",
        "WD1-PSUP.txt"
    );

    public static Stream GetTestfile(string testSamplesRelativePath)
    {
        return File.OpenRead(Path.Combine(TestSamplesDirectory.FullName, testSamplesRelativePath));
    }
}
