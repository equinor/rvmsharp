namespace CadRevealComposer.Utils;

using Operations;
using System.IO;
using System.Text.Json;

public static class JsonUtils
{
    public static void JsonSerializeToFile<T>(T obj, string filename, JsonSerializerOptions? options = null)
    {
        var jsonData = JsonSerializer.Serialize(obj, options);
        File.WriteAllText(filename, jsonData);
    }

    public static void JsonSerializeToFile(CameraPositioning.CameraPosition cameraPosition, string filePath)
    {
        var json = JsonSerializer.Serialize(
            new
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
            }
        );
        File.WriteAllText(filePath, json);
    }
}
