namespace HierarchyComposer.Model;

using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;

public class NodePDMSEntry
{
    public uint NodeId { get; set; }
    public Node Node { get; set; } = null!;

    public long PDMSEntryId { get; set; }
    public PDMSEntry PDMSEntry { get; set; } = null!;

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
        int transactionSize = 0;
        command.Transaction = command.Connection.BeginTransaction(IsolationLevel.Serializable);
        foreach (NodePDMSEntry pdmsEntry in nodePdmsEntries)
        {
            transactionSize++;

            nodeIdParameter.Value = pdmsEntry.NodeId;
            pdmsEntryIdParameter.Value = pdmsEntry.PDMSEntryId;
            command.ExecuteNonQuery();
            if (transactionSize > 1_000_000)
            {
                // Split transactions for potentially more speed, and less memory use (not actually verified).
                transactionSize = 0;
                command.Transaction.Commit();
                command.Transaction.Dispose();
                command.Transaction = command.Connection.BeginTransaction(IsolationLevel.Serializable);
            }
        }
        command.Transaction.Commit();
        command.Transaction.Dispose();
    }
}
