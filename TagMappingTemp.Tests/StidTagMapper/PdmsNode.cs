namespace CadRevealComposer.Operations;

using System.Text.Json.Serialization;

public class PdmsNode
{
    public required string NodeName { get; set; }
    public string? PdmsTag { get; set; }
    public string? Type { get; set; }
}
