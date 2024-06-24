namespace HierarchyComposer.Model;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Data.Sqlite;

public class Node
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public uint Id { get; init; }
    public uint EndId { get; init; }
    public string? RefNoPrefix { get; init; }
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

    public static void RawInsertBatch(SqliteCommand command, IEnumerable<Node> nodes)
    {
        command.CommandText =
            "INSERT INTO Nodes (Id, EndId, RefNoPrefix, RefNoDb, RefNoSequence, Name, HasMesh, ParentId, TopNodeId, AABBId, DiagnosticInfo) VALUES ($Id, $EndId, $RefNoPrefix, $RefNoDb, $RefNoSequence, $Name, $HasMesh, $ParentId, $TopNodeId, $AABBId, $DiagnosticInfo)";
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
                diagnosticInfoParameter
            }
        );

        foreach (Node node in nodes)
        {
            nodeIdParameter.Value = node.Id;
            nodeEndIdParameter.Value = node.EndId;
            refNoPrefixParameter.Value = node.RefNoPrefix;
            refNoDbParameter.Value = node.RefNoDb;
            refNoSequenceParameter.Value = node.RefNoSequence;
            nameParameter.Value = node.Name;
            hasMeshParameter.Value = node.HasMesh;
            parentIdParameter.Value = node.Parent?.Id ?? 0;
            topNodeIdParameter.Value = node.TopNode?.Id ?? 0;
            aabbIdParameter.Value = node.AABB?.Id ?? 0;
            diagnosticInfoParameter.Value = node.DiagnosticInfo;

            command.ExecuteNonQuery();
        }
    }
}
