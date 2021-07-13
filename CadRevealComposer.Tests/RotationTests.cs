namespace CadRevealComposer.Tests
{
    using CadRevealComposer.Utils;
    using Newtonsoft.Json;
    using NUnit.Framework;
    using System;
    using System.IO;
    using System.Linq;
    using System.Numerics;

    [TestFixture]
    public class RotationTests
    {
        private static readonly string TestSamplesDirectory = Path.GetFullPath(Path.Join(TestContext.CurrentContext.TestDirectory, "TestSamples"));

        [Serializable]
        public class RotationTestCase
        {
            [JsonProperty("name")]
            public string Name;
            [JsonProperty("quaternionIn")]
            public QuaternionSerializable QuaternionIn;
            [JsonProperty("rotationAngleOut")]
            public float RotationAngleOut;

            public class QuaternionSerializable
            {
                [JsonProperty("x")]
                public float X;
                [JsonProperty("y")]
                public float Y;
                [JsonProperty("z")]
                public float Z;
                [JsonProperty("w")]
                public float W;
            }
        }

        private static TestCaseData[] ReadTestCases()
        {
            var tests = JsonConvert.DeserializeObject<RotationTestCase[]>(File.ReadAllText(Path.Combine(TestSamplesDirectory, "RotationTestsTestData.json")));
            return tests?.Select(x => new TestCaseData(x).SetName(x.Name)).ToArray();
        }

        private static TestCaseData[] DivideCases => ReadTestCases();

        [Test]
        [TestCaseSource(nameof(DivideCases))]
        public void TestQuaternionDecomposition(RotationTestCase test)
        {
            var q = new Quaternion(test.QuaternionIn.X,test.QuaternionIn.Y,test.QuaternionIn.Z,test.QuaternionIn.W);
            var components = q.DecomposeQuaternion();
            Assert.AreEqual(components.rotationAngle, test.RotationAngleOut, 0.01f);
        }
    }
}