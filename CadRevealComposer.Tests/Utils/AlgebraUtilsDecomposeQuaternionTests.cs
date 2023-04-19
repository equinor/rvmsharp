namespace CadRevealComposer.Tests.Utils;

using CadRevealComposer.Utils;
using System.Text.Json.Serialization;
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
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("quaternionIn")]
        public QuaternionSerializable QuaternionIn { get; set; }
        [JsonPropertyName("normalExpected")]
        public Vector3Serializable NormalExpected { get; set; }
        [JsonPropertyName("rotationAngleExpected")]
        public float RotationAngleExpected { get; set; }

        public record QuaternionSerializable(
            [property: JsonPropertyName("x")] float X,
            [property: JsonPropertyName("y")] float Y,
            [property: JsonPropertyName("z")] float Z,
            [property: JsonPropertyName("w")] float W);

        public record Vector3Serializable(
            [property: JsonPropertyName("x")] float X,
            [property: JsonPropertyName("y")] float Y,
            [property: JsonPropertyName("z")] float Z);
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