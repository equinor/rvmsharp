namespace HierarchyComposer.Model;

using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

public static class NodeTable
{
    public const string TableName = "Nodes";

    public static void CreateTable(SqliteCommand command)
    {
        command.CommandText = $"""
            CREATE TABLE {TableName} (
                            Id INTEGER PRIMARY KEY NOT NULL,
                            EndId INTEGER NOT NULL,
                            RefNoPrefix TEXT NULL COLLATE NOCASE,
                            RefNoDb INTEGER NULL,
                            RefNoSequence INTEGER NULL,
                            Name TEXT NULL COLLATE NOCASE,
                            HasMesh INTEGER NOT NULL,
                            ParentId INTEGER NULL,
                            TopNodeId INTEGER NOT NULL,
                            AABBId INTEGER NULL,
                            DiagnosticInfo TEXT NULL,
                            FOREIGN KEY (ParentId) REFERENCES Nodes(Id),
                            FOREIGN KEY (AABBId) REFERENCES AABBs(Id)
                        ) STRICT, WITHOUT ROWID;
            """;
        command.ExecuteNonQuery();
    }

    public static void CreateIndexes(SqliteCommand cmd)
    {
        // We create indexes AFTER inserting all data, as it allows for faster inserts.
        cmd.CommandText = $"CREATE INDEX IX_{TableName}_Name ON {TableName} (Name)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = $"CREATE INDEX IX_{TableName}_RefNo ON {TableName} (RefNoPrefix, RefNoDb, RefNoSequence)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = $"CREATE INDEX IX_{TableName}_TopNodeId ON {TableName} (TopNodeId)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = $"CREATE INDEX IX_{TableName}_ParentId ON {TableName} (ParentId)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = $"CREATE INDEX IX_{TableName}_AABBId ON {TableName} (AABBId)";
        cmd.ExecuteNonQuery();
    }

    public static void RawInsertBatch(SqliteCommand command, IEnumerable<Node> nodes)
    {
        command.CommandText =
            $"INSERT INTO {TableName} (Id, EndId, RefNoPrefix, RefNoDb, RefNoSequence, Name, HasMesh, ParentId, TopNodeId, AABBId, DiagnosticInfo) VALUES ($Id, $EndId, $RefNoPrefix, $RefNoDb, $RefNoSequence, $Name, $HasMesh, $ParentId, $TopNodeId, $AABBId, $DiagnosticInfo)";
        var nodeIdParameter = command.CreateParameter();
        nodeIdParameter.ParameterName = "$Id";
        var nodeEndIdParameter = command.CreateParameter();
        nodeEndIdParameter.ParameterName = "$EndId";
        var refNoPrefixParameter = command.CreateParameter();
        refNoPrefixParameter.ParameterName = "$RefNoPrefix";
        var refNoDbParameter = command.CreateParameter();
        refNoDbParameter.ParameterName = "$RefNoDb";
        var refNoSequenceParameter = command.CreateParameter();
        refNoSequenceParameter.ParameterName = "$RefNoSequence";
        var nameParameter = command.CreateParameter();
        nameParameter.ParameterName = "$Name";
        var hasMeshParameter = command.CreateParameter();
        hasMeshParameter.ParameterName = "$HasMesh";
        var parentIdParameter = command.CreateParameter();
        parentIdParameter.ParameterName = "$ParentId";
        var topNodeIdParameter = command.CreateParameter();
        topNodeIdParameter.ParameterName = "$TopNodeId";
        var aabbIdParameter = command.CreateParameter();
        aabbIdParameter.ParameterName = "$AABBId";
        var diagnosticInfoParameter = command.CreateParameter();
        diagnosticInfoParameter.ParameterName = "$DiagnosticInfo";

        command.Parameters.AddRange(
            new[]
            {
                nodeIdParameter,
                nodeEndIdParameter,
                refNoPrefixParameter,
                refNoDbParameter,
                refNoSequenceParameter,
                nameParameter,
                hasMeshParameter,
                parentIdParameter,
                topNodeIdParameter,
                aabbIdParameter,
                diagnosticInfoParameter,
            }
        );

        foreach (Node node in nodes)
        {
            nodeIdParameter.Value = node.Id;
            nodeEndIdParameter.Value = node.EndId;
            refNoPrefixParameter.Value = node.RefNoPrefix ?? (object)DBNull.Value;
            refNoDbParameter.Value = node.RefNoDb ?? (object)DBNull.Value;
            refNoSequenceParameter.Value = node.RefNoSequence ?? (object)DBNull.Value;
            nameParameter.Value = node.Name;
            hasMeshParameter.Value = node.HasMesh;
            parentIdParameter.Value = node.ParentId ?? (object)DBNull.Value;
            topNodeIdParameter.Value = node.TopNodeId;
            aabbIdParameter.Value = node.AABBId ?? (object)DBNull.Value;
            diagnosticInfoParameter.Value = node.DiagnosticInfo ?? (object)DBNull.Value;

            command.ExecuteNonQuery();
        }
    }
}

public class Node
{
    public uint Id { get; init; }
    public uint EndId { get; init; }
    public string? RefNoPrefix { get; init; }
    public int? RefNoDb { get; init; }
    public int? RefNoSequence { get; init; }

    public string? Name { get; init; }
    public bool HasMesh { get; init; }

    public uint? ParentId { get; init; }

    public uint TopNodeId { get; init; }

    public int? AABBId { get; init; }

    public string? DiagnosticInfo { get; init; }
}
