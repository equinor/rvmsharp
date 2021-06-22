namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    /// <summary>
    /// A Texture. This is currently Unused in the Reveal format, but it is required in the exported JSON.
    /// </summary>
    public record Texture(
        [property: JsonProperty("file_id")] ulong FileId = default,
        [property: JsonProperty("width")] ushort Width = default,
        [property: JsonProperty("height")] ushort Height = default);
}