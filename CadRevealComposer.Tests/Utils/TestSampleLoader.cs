namespace CadRevealComposer.Tests.Utils
{
    using Newtonsoft.Json;
    using NUnit.Framework;
    using System.IO;

    public static class TestSampleLoader
    {
        public static readonly string TestSamplesDirectory = Path.GetFullPath(Path.Join(TestContext.CurrentContext.TestDirectory, "TestSamples"));

        /// <summary>
        /// Loads test sample from TestSamples folder.
        /// To add a new test sample, create a file in TestSamples folder of this project, and specify in Test project
        /// that this file must be copied in output folder
        /// NOTE: This method will read the whole file into string
        /// </summary>
        /// <param name="filename">JSON filename relative to TestSamples folder</param>
        /// <typeparam name="T">Type to use for deserialization</typeparam>
        /// <returns>Deserialized object</returns>
        public static T LoadTestJson<T>(string filename)
        {
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(Path.Combine(TestSamplesDirectory, filename)));
        }
    }
}