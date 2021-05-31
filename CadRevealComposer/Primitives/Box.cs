namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    public record Box(
        ulong NodeId,
        ulong TreeIndex,
        int[] Color,
        float Diagonal,
        float CenterX,
        float CenterY,
        float CenterZ,
        [property: JsonProperty("normal")] float[] Normal,
        [property: JsonProperty("delta_x")] float DeltaX,
        [property: JsonProperty("delta_y")] float DeltaY,
        [property: JsonProperty("delta_z")] float DeltaZ,
        [property: JsonProperty("rotation_angle")]
        float RotationAngle
    ) : APrimitive(NodeId, TreeIndex, Color, Diagonal, CenterX, CenterY, CenterZ);
}