namespace CadRevealComposer.Tests.Utils
{
    using Newtonsoft.Json;
    using NUnit.Framework;
    using System.IO;

    public class DataLoader
    {
        public static readonly string TestSamplesDirectory = Path.GetFullPath(Path.Join(TestContext.CurrentContext.TestDirectory, "TestSamples"));

        public static T LoadTestJson<T>(string filename)
        {
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(Path.Combine(TestSamplesDirectory, filename)));
        }
    }
}