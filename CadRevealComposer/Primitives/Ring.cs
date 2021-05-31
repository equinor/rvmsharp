namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    public record Ring(
        ulong NodeId,
        ulong TreeIndex,
        int[] Color,
        float Diagonal,
        float CenterX,
        float CenterY,
        float CenterZ,
        [property: JsonProperty("inner_radius")]
        float InnerRadius,
        [property: JsonProperty("outer_radius")]
        float OuterRadius
    ) : APrimitive(NodeId, TreeIndex, Color, Diagonal, CenterX, CenterY, CenterZ);
}