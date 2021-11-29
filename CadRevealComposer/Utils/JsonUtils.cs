namespace CadRevealComposer.Utils
{
    using Newtonsoft.Json;
    using System.IO;
    using System.Numerics;

    public static class JsonUtils
    {
        public static void JsonSerializeToFile<T>(T obj, string filename, Formatting formatting = Formatting.None)
        {
            using var stream = File.Create(filename);
            using var writer = new StreamWriter(stream);
            using var jsonWriter = new JsonTextWriter(writer);
            var jsonSerializer = new JsonSerializer { Formatting = formatting };
            jsonSerializer.Serialize(jsonWriter, obj);
        }

        public static void JsonSerializeToFile(Vector3 cameraPosition, Vector3 cameraDirection, string filePath)
        {
            var json = JsonConvert.SerializeObject(new
            {
                cameraPosition = new
                {
                    x = cameraPosition.X,
                    y = cameraPosition.Y,
                    z = cameraPosition.Z
                },
                cameraDirection = new
                {
                    x = cameraDirection.X,
                    y = cameraDirection.Y,
                    z = cameraDirection.Z
                }
            }, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
    }
}