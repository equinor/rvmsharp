namespace HierarchyComposer.Model;

using Microsoft.Data.Sqlite;
using System;
using System.Numerics;

public class AABB : IEquatable<AABB>
{
    public int Id { get; init; }
    public Vector3EfSerializable min { get; init; } = new Vector3EfSerializable(Vector3.Zero);
    public Vector3EfSerializable max { get; init; } = new Vector3EfSerializable(Vector3.Zero);

    public AABB CopyWithNewId(int id)
    {
        return new AABB() { Id = id, min = this.min, max = this.max };
    }

    public void RawInsert(SqliteCommand command)
    {
        command.CommandText = "INSERT INTO AABBs (Id, min_x, min_y, min_z, max_x, max_y, max_z) VALUES (@Id, @min_x, @min_y, @min_z, @max_x, @max_y, @max_z)";
        command.Parameters.Clear();
        command.Parameters.AddRange(new[] {
            new SqliteParameter("@Id", Id),
            new SqliteParameter("@min_x", min.x),
            new SqliteParameter("@min_y", min.y),
            new SqliteParameter("@min_z", min.z),
            new SqliteParameter("@max_x", max.x),
            new SqliteParameter("@max_y", max.y),
            new SqliteParameter("@max_z", max.z)
        });
        command.ExecuteNonQuery();
    }

    public bool Equals(AABB? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Equals(min, other.min) && Equals(max, other.max);
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

        return Equals((AABB)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(min, max);
    }
}