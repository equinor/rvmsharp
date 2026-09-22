namespace CadRevealRvmProvider.Tests.Utils;

using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using RvmSharp.Primitives;

public static class TestSampleLoader
{
    private static readonly string TestSamplesDirectory = Path.GetFullPath(
        Path.Join(TestContext.CurrentContext.TestDirectory, "TestSamples")
    );
    public static readonly string GlobalTestSamplesDirectory = Path.GetFullPath(
        Path.Join(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "TestSamples")
    );

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
        var options = new JsonSerializerOptions();
        options.IncludeFields = true;
        options.Converters.Add(new ArraySegmentConverter<RvmFacetGroup.RvmContour>());
        options.Converters.Add(new ArraySegmentConverter<(Vector3 Vertex, Vector3 Normal)>());
        return JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(TestSamplesDirectory, filename)), options)!;
    }

    // System.Text.Json can serialize ArraySegment<T>, but cannot instantiate and populate it when reading.
    // Keep the existing nested-array sample format by wrapping deserialized arrays in segment views.
    // This test-only path does not pack shared buffers like the RVM parser does, and keeps JSON dependencies
    // out of the core geometry library.
    private sealed class ArraySegmentConverter<T> : JsonConverter<ArraySegment<T>>
    {
        public override ArraySegment<T> Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            return JsonSerializer.Deserialize<T[]>(ref reader, options)
                ?? throw new JsonException("Expected an array.");
        }

        public override void Write(Utf8JsonWriter writer, ArraySegment<T> value, JsonSerializerOptions options)
        {
            // Write only the view: the backing array may also contain vertices or contours of other polygons.
            writer.WriteStartArray();
            foreach (var item in value)
                JsonSerializer.Serialize(writer, item, options);
            writer.WriteEndArray();
        }
    }
}
