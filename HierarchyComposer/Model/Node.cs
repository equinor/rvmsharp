namespace HierarchyComposer.Model;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SQLite;

public class Node
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public uint Id { get; init; }

    public uint EndId { get; init; }

    public int? RefNoDb { get; init; }
    public int? RefNoSequence { get; init; }

    public string? Name { get; init; }
    public bool HasMesh { get; init; }

    [ForeignKey("ParentId")]
    public virtual Node? Parent { get; set; }

    [ForeignKey("TopNodeId")]
    public virtual Node? TopNode { get; set; }

    public virtual ICollection<NodePDMSEntry>? NodePDMSEntry { get; init; } = null!;

    public AABB? AABB { get; init; }

    public string? DiagnosticInfo { get; init; }

    public void RawInsert(SQLiteCommand command)
    {
        command.CommandText = "INSERT INTO Nodes (Id, EndId, RefNoDb, RefNoSequence, Name, HasMesh, ParentId, TopNodeId, AABBId, DiagnosticInfo) VALUES (@Id, @EndId, @RefNoDb, @RefNoSequence, @Name, @HasMesh, @ParentId, @TopNodeId, @AABBId, @DiagnosticInfo);";
        command.Parameters.AddRange(new[] {
            new SQLiteParameter("@Id", Id),
            new SQLiteParameter("@EndId", EndId),
            new SQLiteParameter("@RefNoDb", RefNoDb),
            new SQLiteParameter("@RefNoSequence", RefNoSequence),
            new SQLiteParameter("@Name", Name),
            new SQLiteParameter("@HasMesh", HasMesh),
            new SQLiteParameter("@ParentId", Parent?.Id ?? 0),
            new SQLiteParameter("@TopNodeId", TopNode?.Id ?? 0),
            new SQLiteParameter("@AABBId", AABB?.Id ?? 0),
            new SQLiteParameter("@DiagnosticInfo", DiagnosticInfo)
        });
        command.ExecuteNonQuery();
    }
}