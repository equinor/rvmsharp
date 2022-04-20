namespace CadRevealComposer.Tests.Utils;

using CadRevealComposer.Utils;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Linq;
using System.Numerics;

[TestFixture]
public class AlgebraUtilsDecomposeQuaternionTests
{
    [Serializable]
    public record RotationTestCase
    {
        [JsonProperty("name")]
        public string Name;
        [JsonProperty("quaternionIn")]
        public QuaternionSerializable QuaternionIn;
        [JsonProperty("normalExpected")]
        public Vector3Serializable NormalExpected;
        [JsonProperty("rotationAngleExpected")]
        public float RotationAngleExpected;

        public record QuaternionSerializable(
            [JsonProperty("x")] float X,
            [JsonProperty("y")] float Y,
            [JsonProperty("z")] float Z,
            [JsonProperty("w")] float W);

        public record Vector3Serializable(
            [JsonProperty("x")] float X,
            [JsonProperty("y")] float Y,
            [JsonProperty("z")] float Z);
    }

    private static TestCaseData[] ReadTestCases()
    {
        var tests = TestSampleLoader.LoadTestJson<RotationTestCase[]>("QuaternionDecomposition.json");
        return tests?.Select(x => new TestCaseData(x).SetName(x.Name)).ToArray();
    }

    private static TestCaseData[] DivideCases => ReadTestCases();

    [Test]
    [TestCaseSource(nameof(DivideCases))]
    public void TestQuaternionDecomposition(RotationTestCase test)
    {
        var quaternionInput = new Quaternion(test.QuaternionIn.X,test.QuaternionIn.Y,test.QuaternionIn.Z,test.QuaternionIn.W);
        var normalExpected = new Vector3(test.NormalExpected.X, test.NormalExpected.Y, test.NormalExpected.Z);
        (Vector3 normalCalculated, float rotationAngleCalculated) = quaternionInput.DecomposeQuaternion();
        Assert.That(normalExpected.EqualsWithinTolerance(normalCalculated, 0.00001f));
        Assert.AreEqual(rotationAngleCalculated, test.RotationAngleExpected, 0.01f);
    }
}