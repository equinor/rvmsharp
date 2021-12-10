namespace CadRevealComposer.Utils
{
    using Newtonsoft.Json;
    using Operations;
    using System.IO;

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

        public static void JsonSerializeToFile(CameraPositioning.CameraPosition cameraPosition, string filePath)
        {
            var json = JsonConvert.SerializeObject(new
            {
                cameraPosition = new
                {
                    x = cameraPosition.Position.X,
                    y = cameraPosition.Position.Y,
                    z = cameraPosition.Position.Z
                },
                cameraDirection = new
                {
                    x = cameraPosition.Direction.X,
                    y = cameraPosition.Direction.Y,
                    z = cameraPosition.Direction.Z
                },
                targetPosition = new
                {
                    x = cameraPosition.Target.X,
                    y = cameraPosition.Target.Y,
                    z = cameraPosition.Target.Z
                }
            }, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
    }
}