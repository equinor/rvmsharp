namespace HierarchyComposer.Model;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

[Index(nameof(ParentId), IsUnique = false)]
[Index(nameof(TopNodeId), IsUnique = false)]
public class Node
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public uint Id { get; init; }

    public uint EndId { get; init; }

    public int? RefNoDb { get; init; }
    public int? RefNoSequence { get; init; }

    public string? Name { get; init; }
    public bool HasMesh { get; init; }

    // Index (see class annotation)
    public uint? ParentId { get; init; }

    // Index (see class annotation)
    public uint TopNodeId { get; init; }

    public virtual ICollection<NodePDMSEntry>? NodePDMSEntry { get; init; } = null!;

    public AABB? AABB { get; init; }

    public string? DiagnosticInfo { get; init; }

    public void RawInsert(SqliteCommand command)
    {
        command.CommandText =
            "INSERT INTO Nodes (Id, EndId, RefNoDb, RefNoSequence, Name, HasMesh, ParentId, TopNodeId, AABBId, DiagnosticInfo) VALUES (@Id, @EndId, @RefNoDb, @RefNoSequence, @Name, @HasMesh, @ParentId, @TopNodeId, @AABBId, @DiagnosticInfo);";
        command.Parameters.Clear();
        command.Parameters.AddRange(new[]
        {
            new SqliteParameter("@Id", Id),
            new SqliteParameter("@EndId", EndId),
            new SqliteParameter("@RefNoDb", RefNoDb ?? (object)DBNull.Value),
            new SqliteParameter("@RefNoSequence", RefNoSequence ?? (object)DBNull.Value),
            new SqliteParameter("@Name", Name),
            new SqliteParameter("@HasMesh", HasMesh),
            new SqliteParameter("@ParentId", ParentId ?? (object) DBNull.Value),
            new SqliteParameter("@TopNodeId", TopNodeId),
            new SqliteParameter("@AABBId", AABB?.Id ?? (object) DBNull.Value),
            new SqliteParameter("@DiagnosticInfo", DiagnosticInfo ?? (object)DBNull.Value)
        });
        command.ExecuteNonQuery();
    }
}