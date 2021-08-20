using System;
using System.Data.SQLite;
using System.Diagnostics.CodeAnalysis;

namespace Mop.Hierarchy.Model
{
    public class AABB : IEquatable<AABB>
    {
        public int Id { get; set; }
        public Vector3f min { get; set; }
        public Vector3f max { get; set; }

        public bool Equals([AllowNull] AABB other)
        {
            //Check whether the compared object is null. 
            if (Object.ReferenceEquals(other, null)) return false;

            //Check whether the compared object references the same data. 
            if (Object.ReferenceEquals(this, other)) return true;

            //Check whether the products' properties are equal. 
            return Id == other.Id && min.Equals(other.min) && max.Equals(other.max);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() ^ min.GetHashCode() ^ max.GetHashCode();
        }

        public void RawInsert(SQLiteCommand command)
        {
            command.CommandText = "INSERT INTO AABBs (Id, min_x, min_y, min_z, max_x, max_y, max_z) VALUES (@Id, @min_x, @min_y, @min_z, @max_x, @max_y, @max_z)";
            command.Parameters.AddRange(new[] {
                    new SQLiteParameter("@Id", Id),
                    new SQLiteParameter("@min_x", min.x),
                    new SQLiteParameter("@min_y", min.y),
                    new SQLiteParameter("@min_z", min.z),
                    new SQLiteParameter("@max_x", max.x),
                    new SQLiteParameter("@max_y", max.y),
                    new SQLiteParameter("@max_z", max.z)
                    });
            command.ExecuteNonQuery();
        }
    }
}
