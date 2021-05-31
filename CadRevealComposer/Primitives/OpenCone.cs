namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    public record OpenCone(
        ulong NodeId,
        ulong TreeIndex,
        int[] Color,
        float Diagonal,
        float CenterX,
        float CenterY,
        float CenterZ,
        [property: JsonProperty("center_axis")]
        float[] CenterAxis,
        [property: JsonProperty("height")] float Height,
        [property: JsonProperty("radius_a")] float RadiusA,
        [property: JsonProperty("radius_b")] float RadiusB
    ) : APrimitive(NodeId, TreeIndex, Color, Diagonal, CenterX, CenterY, CenterZ);
}