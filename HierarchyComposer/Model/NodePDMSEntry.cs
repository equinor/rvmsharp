namespace HierarchyComposer.Model;

using System.Collections.Generic;
using System.Data.SQLite;

public class NodePDMSEntry
{
    public uint NodeId { get; init; }
    public Node Node { get; init; } = null!;

    public long PDMSEntryId { get; init; }
    public PDMSEntry PDMSEntry { get; init; } = null!;

    public void RawInsert(SQLiteCommand command)
    {
        command.CommandText = "INSERT INTO NodeToPDMSEntry (NodeId, PDMSEntryId) VALUES (@NodeId, @PDMSEntryId);";
        command.Parameters.AddRange(
            new[] { new SQLiteParameter("@NodeId", NodeId), new SQLiteParameter("@PDMSEntryId", PDMSEntryId) }
        );
        command.ExecuteNonQuery();
    }

    public static void RawInsertBatch(SQLiteCommand command, IEnumerable<NodePDMSEntry> nodePdmsEntries)
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
