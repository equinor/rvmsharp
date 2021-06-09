namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;


    public record Sphere(
        CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: JsonProperty("radius")] float Radius
    ) : APrimitive(CommonPrimitiveProperties);
}