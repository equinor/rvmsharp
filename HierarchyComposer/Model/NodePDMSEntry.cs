namespace HierarchyComposer.Model;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Data.Sqlite;

public struct NodePdmsEntryMapItem
{
    public uint NodeId;

    public int PDMSEntryId;
}

public static class NodePDMSEntry
{
    public static void CreateTable(SqliteCommand command)
    {
        command.CommandText = """
                        CREATE TABLE NodeToPdmsEntry (
                            NodeId INTEGER NOT NULL,
                            PDMSEntryId INTEGER NOT NULL,
                            PRIMARY KEY (NodeId, PDMSEntryId)) WITHOUT ROWID
                            ;
            """;
        command.ExecuteNonQuery();
    }

    public static void RawInsertBatch(SqliteConnection connection, IEnumerable<NodePdmsEntryMapItem> nodePdmsEntries)
    {
        const int chunkSize = 1_000_000;

        foreach (var chunk in nodePdmsEntries.OrderBy(x => x.NodeId).ThenBy(x => x.PDMSEntryId).Chunk(chunkSize))
        {
            var stopwatch = Stopwatch.StartNew();
            InsertChunk(connection, chunk);
            stopwatch.Stop();
            Console.WriteLine($"Inserted chunk in {stopwatch.ElapsedMilliseconds} ms");
        }
    }

    private static void InsertChunk(SqliteConnection connection, IEnumerable<NodePdmsEntryMapItem> chunk)
    {
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();

        command.CommandText = "INSERT INTO NodeToPDMSEntry (NodeId, PDMSEntryId) VALUES ($NodeId, $PDMSEntryId);";

        var nodeIdParameter = command.CreateParameter();
        nodeIdParameter.ParameterName = "$NodeId";
        var pdmsEntryIdParameter = command.CreateParameter();
        pdmsEntryIdParameter.ParameterName = "$PDMSEntryId";

        command.Parameters.AddRange(new[] { nodeIdParameter, pdmsEntryIdParameter });

        foreach (var pdmsEntry in chunk)
        {
            nodeIdParameter.Value = pdmsEntry.NodeId;
            pdmsEntryIdParameter.Value = pdmsEntry.PDMSEntryId;
            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public static void CreateIndexes(SqliteCommand cmd)
    {
        // Indexes are most efficient when the most selective columns come first.
        // The primary key (NodeId, PDMSEntryId) already covers queries for all PDMSEntryIds of a given NodeId.
        // To efficiently query all NodeIds for a given PDMSEntryId, a reverse index on (PDMSEntryId, NodeId) is created.
        cmd.CommandText =
            "CREATE INDEX IX_NodePDMSEntries_PdmsEntryIdToNodeId ON NodeToPdmsEntry (PDMSEntryId, NodeId)";
        cmd.ExecuteNonQuery();
    }
}
