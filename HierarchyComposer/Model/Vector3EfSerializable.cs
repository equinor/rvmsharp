namespace HierarchyComposer.Model;

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
    public float X { get; }
    public float Y { get; }
    public float Z { get; }

    // ReSharper disable once UnusedMember.Global
    public Vector3EfSerializable() { }

    public Vector3EfSerializable(Vector3 v)
    {
        X = v.X;
        Y = v.Y;
        Z = v.Z;
    }

    public static implicit operator Vector3EfSerializable(Vector3 v)
    {
        return new Vector3EfSerializable(v);
    }

    public static implicit operator Vector3(Vector3EfSerializable v)
    {
        return new Vector3(v.X, v.Y, v.Z);
    }

    public bool Equals(Vector3EfSerializable? other)
    {
        //Check whether the compared object is null.
        if (ReferenceEquals(other, null))
            return false;

        //Check whether the compared object references the same data.
        if (ReferenceEquals(this, other))
            return true;

        //Check whether the products' properties are equal.
        const float tolerance = 0.001f;
        return Math.Abs(X - other.X) < tolerance
            && Math.Abs(Y - other.Y) < tolerance
            && Math.Abs(Z - other.Z) < tolerance;
    }

    public override int GetHashCode()
    {
        return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Vector3EfSerializable);
    }
}
