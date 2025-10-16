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
    const string ViewName = "PDMSEntries";
    private const string TableNameKeys = "PDMSEntries_Keys";

    private const string TableNameValues = "PDMSEntries_Values";

    public static void CreateTable(SqliteCommand command)
    {
        // We create two tables, one for keys and one for values.
        // This allows us to store keys only once, and reference them by id in the values table.
        // This is a form of normalization, and helps reduce the size of the database significantly
        // when there are many repeated keys (which is common in PDMS data).

        command.CommandText = $"""
            CREATE TABLE {TableNameKeys} (
                Id INTEGER PRIMARY KEY,
                Key TEXT NOT NULL UNIQUE -- UNIQUE will auto-create an index, so we dont have to.
            ) STRICT, WITHOUT ROWID;
            """;
        command.ExecuteNonQuery();

        command.CommandText = $"""
            CREATE TABLE IF NOT EXISTS {TableNameValues} (
                Id INTEGER PRIMARY KEY,
                KeyId INTEGER NOT NULL,
                Value TEXT NOT NULL COLLATE NOCASE
            ) STRICT; -- WITHOUT ROWID is intentionally not used here. See https://stackoverflow.com/a/79376535 for explanation
            """;
        command.ExecuteNonQuery();

        // Create a view table for Key Value text instead of having to use joins for the PdmsKeys table
        command.CommandText = $"""
            CREATE VIEW {ViewName} AS
            SELECT {TableNameValues}.Id, {TableNameKeys}.Key, {TableNameValues}.Value
            FROM {TableNameValues}
            JOIN {TableNameKeys} ON {TableNameValues}.KeyId = {TableNameKeys}.Id;
            """;
        command.ExecuteNonQuery();
    }

    public static void CreateIndexes(SqliteCommand command)
    {
        // Index on Key/Value for fast lookup of PDMS entries by key/value pair
        // This index also covers queries that filter only by Key, as Key is the first column in the index
        command.CommandText = $"CREATE INDEX IX_{TableNameValues}_KeyValue ON {TableNameValues} (KeyId, Value);";
        command.ExecuteNonQuery();
    }

    public static void RawInsertBatch(SqliteCommand command, IReadOnlyList<PdmsEntry> pdmsEntries)
    {
        var keys = pdmsEntries.Select(x => x.Key).Distinct();
        // First insert keys into the keys table, and get a mapping of key to id
        Dictionary<string, int> keyToIdMap = AddKeysToKeysTable(command, keys);

        // Then insert values into the values table, using the key id from the mapping
        command.CommandText = $"INSERT INTO {TableNameValues} (Id, KeyId, Value) VALUES ($Id, $KeyId, $Value);";

        var idParameter = command.CreateParameter();
        idParameter.ParameterName = "$Id";
        var keyIdParameter = command.CreateParameter();
        keyIdParameter.ParameterName = "$KeyId";
        var valueParameter = command.CreateParameter();
        valueParameter.ParameterName = "$Value";

        command.Parameters.AddRange([idParameter, keyIdParameter, valueParameter]);

        foreach (PdmsEntry pdmsEntry in pdmsEntries)
        {
            idParameter.Value = pdmsEntry.Id;
            keyIdParameter.Value = keyToIdMap[pdmsEntry.Key];
            valueParameter.Value = pdmsEntry.Value;
            command.ExecuteNonQuery();
        }
    }

    private static Dictionary<string, int> AddKeysToKeysTable(SqliteCommand command, IEnumerable<string> keys)
    {
        var keyToId = new Dictionary<string, int>();
        command.CommandText = $"INSERT INTO {TableNameKeys} (Id, Key) VALUES ($Id, $Key);";
        var keyParameter = command.CreateParameter();
        keyParameter.ParameterName = "$Key";
        command.Parameters.Add(keyParameter);
        var keyIdParameter = command.CreateParameter();
        keyIdParameter.ParameterName = "$Id";
        command.Parameters.Add(keyIdParameter);

        var idCounter = 1; // Start IDs from 1 which is common in databases
        foreach (var key in keys)
        {
            var id = idCounter++;
            keyParameter.Value = key;
            keyIdParameter.Value = id;
            command.ExecuteNonQuery();
            keyToId[key] = id;
        }
        command.Parameters.Clear();
        return keyToId;
    }
}
