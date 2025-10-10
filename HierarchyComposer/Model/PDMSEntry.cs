namespace HierarchyComposer.Model;

using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

public struct PdmsEntry : IEquatable<PdmsEntry>
{
    public int Id;
    public string Key;
    public string Value;

    public bool Equals(PdmsEntry other)
    {
        return Key == other.Key && Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is PdmsEntry other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Key, Value);
    }
}

public class PDMSEntryTable
{
    public static void CreateTable(SqliteCommand command)
    {
        command.CommandText = """
                        CREATE TABLE IF NOT EXISTS PDMSEntries (
                            Id INTEGER PRIMARY KEY,
                            Key TEXT NOT NULL,
                            Value TEXT NOT NULL
                        ) STRICT, WITHOUT ROWID;
            """;
        command.ExecuteNonQuery();
    }

    public static void RawInsertBatch(SqliteCommand command, IEnumerable<PdmsEntry> pdmsEntries)
    {
        command.CommandText = "INSERT INTO PDMSEntries (Id, Key, Value) VALUES ($Id, $Key, $Value);";

        var idParameter = command.CreateParameter();
        idParameter.ParameterName = "$Id";
        var keyParameter = command.CreateParameter();
        keyParameter.ParameterName = "$Key";
        var valueParameter = command.CreateParameter();
        valueParameter.ParameterName = "$Value";

        command.Parameters.AddRange(new[] { idParameter, keyParameter, valueParameter });

        foreach (PdmsEntry pdmsEntry in pdmsEntries)
        {
            idParameter.Value = pdmsEntry.Id;
            keyParameter.Value = pdmsEntry.Key;
            valueParameter.Value = pdmsEntry.Value;
            command.ExecuteNonQuery();
        }
    }
}
