namespace HierarchyComposer.Model;

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Numerics;

public class AABB : IEquatable<AABB>
{
    public int Id { get; private init; }
    public Vector3EfSerializable Min { get; init; } = new(Vector3.Zero);
    public Vector3EfSerializable Max { get; init; } = new(Vector3.Zero);

    public AABB CopyWithNewId(int id)
    {
        return new AABB()
        {
            Id = id,
            Min = this.Min,
            Max = this.Max
        };
    }

    public static void RawInsertBatch(SQLiteCommand command, IEnumerable<AABB> aabbs)
    {
        command.CommandText =
            "INSERT INTO AABBs (Id, min_x, min_y, min_z, max_x, max_y, max_z) VALUES ($Id, $min_x, $min_y, $min_z, $max_x, $max_y, $max_z)";

        var aabbIdParameter = command.CreateParameter();
        aabbIdParameter.ParameterName = "$Id";
        var minXParameter = command.CreateParameter();
        minXParameter.ParameterName = "$min_x";
        var minYParameter = command.CreateParameter();
        minYParameter.ParameterName = "$min_y";
        var minZParameter = command.CreateParameter();
        minZParameter.ParameterName = "$min_z";
        var maxXParameter = command.CreateParameter();
        maxXParameter.ParameterName = "$max_x";
        var maxYParameter = command.CreateParameter();
        maxYParameter.ParameterName = "$max_y";
        var maxZParameter = command.CreateParameter();
        maxZParameter.ParameterName = "$max_z";

        command.Parameters.AddRange(
            new[]
            {
                aabbIdParameter,
                minXParameter,
                minYParameter,
                minZParameter,
                maxXParameter,
                maxYParameter,
                maxZParameter
            }
        );

        foreach (var aabb in aabbs)
        {
            aabbIdParameter.Value = aabb.Id;
            minXParameter.Value = aabb.Min.X;
            minYParameter.Value = aabb.Min.Y;
            minZParameter.Value = aabb.Min.Z;
            maxXParameter.Value = aabb.Max.X;
            maxYParameter.Value = aabb.Max.Y;
            maxZParameter.Value = aabb.Max.Z;
            command.ExecuteNonQuery();
        }
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

        return Equals(Min, other.Min) && Equals(Max, other.Max);
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
        return HashCode.Combine(Min, Max);
    }
}
