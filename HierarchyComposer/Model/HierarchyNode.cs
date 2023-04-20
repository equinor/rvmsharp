namespace HierarchyComposer.Model;

using System.Collections.Generic;

public class HierarchyNode
{
    public uint NodeId { get; set; }
    public uint EndId { get; set; }

    public string? RefNoPrefix { get; set; }
    public int? RefNoDb { get; set; }
    public int? RefNoSequence { get; set; }
    public string Name { get; set; } = "";
    public uint TopNodeId { get; set; }
    public uint? ParentId { get; set; }
    public Dictionary<string, string> PDMSData { get; init; } = new Dictionary<string, string>();
    public bool HasMesh { get; set; }
    public AABB? AABB { get; set; }
    public string? OptionalDiagnosticInfo { get; set; } = null;
}
