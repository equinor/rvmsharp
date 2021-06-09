namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    public record ClosedEllipsoidSegment(
            ulong NodeId,
            ulong TreeIndex,
            int[] Color,
            float Diagonal,
            float CenterX,
            float CenterY,
            float CenterZ,
            [property: JsonProperty("normal")]
            float[] Normal,
            [property: JsonProperty("height")] float Height,
            [property: JsonProperty("horizontal_radius")] float HorizontalRadius,
            [property: JsonProperty("vertical_radius")] float VerticalRadius)
        : APrimitive(NodeId, TreeIndex, Color, Diagonal, CenterX, CenterY, CenterZ);
}