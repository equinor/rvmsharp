namespace HierarchyComposer.Model;

using System.Collections.Generic;

public class HierarchyNode
{
    public uint NodeId { get; init; }
    public uint EndId { get; init; }

    public string? RefNoPrefix { get; init; }
    public int? RefNoDb { get; init; }
    public int? RefNoSequence { get; init; }
    public string Name { get; init; } = "";
    public uint TopNodeId { get; init; }
    public uint? ParentId { get; init; }
    public Dictionary<string, string> PDMSData { get; init; } = new();
    public bool HasMesh { get; init; }
    public AABB? AABB { get; init; }
    public string? OptionalDiagnosticInfo { get; init; }
}
