namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    public record OpenCylinder(
        CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: JsonProperty("center_axis")] float[] CenterAxis,
        [property: JsonProperty("height")] float Height,
        [property: JsonProperty("radius")] float Radius
    ) : APrimitive(CommonPrimitiveProperties);
}