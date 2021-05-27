namespace CadRevealComposer
{
    using Newtonsoft.Json;
    using Primitives;

    public class Box : APrimitive
    {
        [JsonProperty("color")] public int[] Color { get; set; }

        [JsonProperty("diagonal")] public float Diagonal { get; set; }

        [JsonProperty("center_x")] public float CenterX { get; set; }

        [JsonProperty("center_y")] public float CenterY { get; set; }

        [JsonProperty("center_z")] public float CenterZ { get; set; }

        [JsonProperty("normal")] public float[] Normal { get; set; }

        [JsonProperty("delta_x")] public float DeltaX { get; set; }

        [JsonProperty("delta_y")] public float DeltaY { get; set; }

        [JsonProperty("delta_z")] public float DeltaZ { get; set; }

        [JsonProperty("rotation_angle")] public float RotationAngle { get; set; }
    }
}