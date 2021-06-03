namespace CadRevealComposer.Tests
{
    using CadRevealComposer.Utils;
    using Newtonsoft.Json;
    using NUnit.Framework;
    using System.IO;
    using System.Numerics;

    [TestFixture]
    public class RotationTests
    {
        private readonly DirectoryInfo TestSamplesDirectory = new DirectoryInfo(Path.GetFullPath(Path.Join(TestContext.CurrentContext.TestDirectory, "TestSamples")));
        
        public class RotationTestsJson
        {
            public RotationTestCase[] testCases;

            public class RotationTestCase
            {
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
        }
        [Test]
        public void CheckRotation1()
        {
            
            var tests = JsonConvert.DeserializeObject<RotationTestsJson>(File.ReadAllText(Path.Combine(TestSamplesDirectory.FullName, "TestData.json")));

            foreach (var test in tests.testCases)
            {
                var q = new Quaternion(test.quaternion.x,test.quaternion.y,test.quaternion.z,test.quaternion.w);
                var components = q.DecomposeQuaternion();
                Assert.AreEqual(components.rotationAngle, test.answer, 0.01f);
            }
        }
    }
}