namespace CadRevealComposer.Primitives
{
    /// <summary>
    /// A Texture. This is currently Unused in the Reveal format, but it is required in the exported JSON.
    /// </summary>
    public record Texture(
         ulong FileId = default,
         ushort Width = default,
         ushort Height = default);
}