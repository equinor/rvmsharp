namespace CadRevealComposer.Tests
{
    using CadRevealComposer.Utils;
    using Newtonsoft.Json;
    using NUnit.Framework;
    using System.IO;
    using System.Linq;
    using System.Numerics;

    [TestFixture]
    public class RotationTests
    {
        private static readonly DirectoryInfo TestSamplesDirectory = new(Path.GetFullPath(Path.Join(TestContext.CurrentContext.TestDirectory, "TestSamples")));
        
        // ReSharper disable InconsistentNaming
        // ReSharper disable UnassignedField.Global
        // ReSharper disable ClassNeverInstantiated.Global
        public class RotationTestCase
        {
            public string name;
            public QuaternionSerializable quaternion;
            public float answer;

            public class QuaternionSerializable
            {
                public float x;
                public float y;
                public float z;
                public float w;
            }
        }

        private static TestCaseData[] ReadTestCases()
        {
            var tests = JsonConvert.DeserializeObject<RotationTestCase[]>(File.ReadAllText(Path.Combine(TestSamplesDirectory.FullName, "TestData.json")));
            return tests?.Select(x => new TestCaseData(x).SetName(x.name)).ToArray();
        }

        private static TestCaseData[] DivideCases => ReadTestCases();

        [Test]
        [TestCaseSource(nameof(DivideCases))]
        public void CheckRotation1(RotationTestCase test)
        {
            var q = new Quaternion(test.quaternion.x,test.quaternion.y,test.quaternion.z,test.quaternion.w);
            var components = q.DecomposeQuaternion();
            Assert.AreEqual(components.rotationAngle, test.answer, 0.01f);
        }
    }
}