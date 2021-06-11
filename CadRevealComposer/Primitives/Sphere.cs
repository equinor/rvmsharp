namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;


    public record Sphere(
        CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: I3df(I3dfAttribute.AttributeType.Radius)]
        [property: JsonProperty("radius")] float Radius
    ) : APrimitive(CommonPrimitiveProperties);
}