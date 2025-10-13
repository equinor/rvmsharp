namespace HierarchyComposer.Model;

using System.Collections.Generic;
using System.Numerics;
using Microsoft.Data.Sqlite;

public record AabbItem(Vector3 Min, Vector3 Max, int Id = 0);

public static class AabbTable
{
    public static void CreateTable(SqliteCommand command)
    {
        command.CommandText =
            "CREATE VIRTUAL TABLE AABBs USING rtree_i32(Id, min_x, max_x, min_y, max_y, min_z, max_z)";
        command.ExecuteNonQuery();
    }

    public static void RawInsertBatch(SqliteCommand command, IEnumerable<AabbItem> aabbs)
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
                maxZParameter,
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
}
