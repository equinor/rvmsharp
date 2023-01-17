namespace HierarchyComposer.Model;

using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

public class PDMSEntry : IEquatable<PDMSEntry>
{
    public long Id { get; init; } = -1;
    public string? Key { get; init; }
    public string? Value { get; init; }
    public virtual ICollection<NodePDMSEntry> NodePDMSEntry { get; set; } = null!; // Navigation property

    public void RawInsert(SqliteCommand command)
    {
        command.CommandText = "INSERT INTO PDMSEntries (Id, Key, Value) VALUES (@Id, @Key, @Value);";
        command.Parameters.Clear();
        command.Parameters.AddRange(new[] {
            new SqliteParameter("@Id", Id),
            new SqliteParameter("@Key", Key),
            new SqliteParameter("@Value", Value)});
        command.ExecuteNonQuery();
    }

    public bool Equals(PDMSEntry? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Key == other.Key && Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != this.GetType())
        {
            return false;
        }

        return Equals((PDMSEntry)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Key, Value);
    }
}