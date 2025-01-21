namespace HierarchyComposer.Model;

using Microsoft.Data.Sqlite;
using System.Collections.Generic;

public class NodePDMSEntry
{
    public uint NodeId { get; set; }
    public Node Node { get; set; } = null!;

    public long PDMSEntryId { get; set; }
    public PDMSEntry PDMSEntry { get; set; } = null!;

    public void RawInsert(SqliteCommand command)
    {
        command.CommandText = "INSERT INTO NodeToPDMSEntry (NodeId, PDMSEntryId) VALUES (@NodeId, @PDMSEntryId);";
        command.Parameters.AddRange(
            new[] { new SqliteParameter("@NodeId", NodeId), new SqliteParameter("@PDMSEntryId", PDMSEntryId) }
        );
        command.ExecuteNonQuery();
    }

    public static void RawInsertBatch(SqliteCommand command, IEnumerable<NodePDMSEntry> nodePdmsEntries)
    {
        command.CommandText = "INSERT INTO NodeToPDMSEntry (NodeId, PDMSEntryId) VALUES ($NodeId, $PDMSEntryId);";

        var nodeIdParameter = command.CreateParameter();
        nodeIdParameter.ParameterName = "$NodeId";
        var pdmsEntryIdParameter = command.CreateParameter();
        pdmsEntryIdParameter.ParameterName = "$PDMSEntryId";

        command.Parameters.AddRange(new[] { nodeIdParameter, pdmsEntryIdParameter });

        foreach (NodePDMSEntry pdmsEntry in nodePdmsEntries)
        {
            nodeIdParameter.Value = pdmsEntry.NodeId;
            pdmsEntryIdParameter.Value = pdmsEntry.PDMSEntryId;
            command.ExecuteNonQuery();
        }
    }
}
