namespace HierarchyComposer.Model;

using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;

public struct PdmsEntry
{
    public required int Id;
    public required string Key;
    public required string Value;
}

public class PDMSEntryTable
{
    const string TableName = "PDMSEntries";

    public static void CreateTable(SqliteCommand command)
    {
        command.CommandText = $"""
            CREATE TABLE {TableName}_Keys (
                Id INTEGER PRIMARY KEY,
                Key TEXT NOT NULL UNIQUE
            ) STRICT, WITHOUT ROWID;
            """;
        command.ExecuteNonQuery();

        command.CommandText = $"""
                        CREATE TABLE IF NOT EXISTS {TableName}_Values (
                            Id INTEGER PRIMARY KEY,
                            Key INTEGER NOT NULL,
                            Value TEXT NOT NULL COLLATE NOCASE
                        ) STRICT, WITHOUT ROWID;
            """;
        command.ExecuteNonQuery();

        // Create a view table for Key Value text instead of having to use joins for the PdmsKeys table
        command.CommandText = $"""
            CREATE VIEW IF NOT EXISTS {TableName} AS
            SELECT e.Id, k.Key, e.Value
            FROM {TableName}_Values e
            JOIN PdmsKeys k ON e.Key = k.Id;
            """;
        command.ExecuteNonQuery();
    }

    public static void CreateIndexes(SqliteCommand command)
    {
        // Index on Key/Value for fast lookup of PDMS entries by key/value pair
        // This index also covers queries that filter only by Key, as Key is the first column in the index
        command.CommandText = $"CREATE INDEX IX_{TableName}_KeyValue ON PDMSEntries_Values (Key,Value COlLATE NOCASE);";
        command.ExecuteNonQuery();
    }

    public static void RawInsertBatch(SqliteCommand command, IReadOnlyList<PdmsEntry> pdmsEntries)
    {
        var keys = pdmsEntries.Select(x => x.Key).Distinct();
        var keyToId = new Dictionary<string, int>();
        command.CommandText = $"INSERT INTO {TableName}_Keys (Id, Key) VALUES ($Id, $Key);";
        var keyParameter = command.CreateParameter();
        keyParameter.ParameterName = "$Key";
        command.Parameters.Add(keyParameter);
        var keyIdParameter = command.CreateParameter();
        keyIdParameter.ParameterName = "$Id";
        command.Parameters.Add(keyIdParameter);

        var idCounter = 1;
        foreach (var key in keys)
        {
            var id = idCounter++;
            keyParameter.Value = key;
            keyIdParameter.Value = id;
            command.ExecuteNonQuery();
            keyToId[key] = id;
        }

        command.CommandText = $"INSERT INTO {TableName}_Values (Id, Key, Value) VALUES ($Id, $Key, $Value);";

        var idParameter = command.CreateParameter();
        idParameter.ParameterName = "$Id";
        var numKeyParameter = command.CreateParameter();
        numKeyParameter.ParameterName = "$Key";
        var valueParameter = command.CreateParameter();
        valueParameter.ParameterName = "$Value";

        command.Parameters.AddRange(new[] { idParameter, numKeyParameter, valueParameter });

        foreach (PdmsEntry pdmsEntry in pdmsEntries)
        {
            idParameter.Value = pdmsEntry.Id;
            numKeyParameter.Value = keyToId[pdmsEntry.Key];
            valueParameter.Value = pdmsEntry.Value;
            command.ExecuteNonQuery();
        }
    }
}
