namespace CadRevealComposer.Operations;

using System.Text.Json.Serialization;

public class PdmsNode
{
    public required string NodeName { get; set; }    [JsonPropertyName("PDMS Tag")]
    public string? PdmsTag { get; set; }
}
