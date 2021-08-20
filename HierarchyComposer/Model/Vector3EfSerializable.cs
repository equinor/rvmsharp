namespace HierarchyComposer.Model
{
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Numerics;

    /// <summary>
    /// Vector3EfSerializable is ONLY used for serializing to Entity Framework, as EF does not (As of EFCore 6.0) support serializing Vector3
    /// See https://github.com/dotnet/efcore/issues/9906 for any updates.
    /// </summary>
    [Owned]
    public class Vector3EfSerializable : IEquatable<Vector3EfSerializable>
    {
        public float x { get; init; }
        public float y { get; init; }
        public float z { get; init; }

        // ReSharper disable once UnusedMember.Global
        public Vector3EfSerializable() { }

        public Vector3EfSerializable(Vector3 v)
        {
            x = v.X;
            y = v.Y;
            z = v.Z;
        }

        public static implicit operator Vector3EfSerializable(Vector3 v)
        {
            return new Vector3EfSerializable(v);
        }

        public static implicit operator Vector3(Vector3EfSerializable v)
        {
            return new Vector3(v.x, v.y, v.z);
        }

        public bool Equals(Vector3EfSerializable? other)
        {
            //Check whether the compared object is null.
            if (ReferenceEquals(other, null)) return false;

            //Check whether the compared object references the same data.
            if (ReferenceEquals(this, other)) return true;

            //Check whether the products' properties are equal.
            float tolerance = 0.001f;
            return Math.Abs(x - other.x) < tolerance && Math.Abs(y - other.y) < tolerance &&
                   Math.Abs(z - other.z) < tolerance;
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode();
        }
    }
}