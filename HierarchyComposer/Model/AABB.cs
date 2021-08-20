namespace HierarchyComposer.Model
{
    using System;
    using System.Data.SQLite;
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
}