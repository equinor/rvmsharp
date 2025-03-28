namespace RvmSharp.Primitives;

using System;
using System.Numerics;

public class RvmConnection
{
    [Flags]
    public enum ConnectionType
    {
        None = 0,
        HasCircularSide = 1,
        HasRectangularSide = 2,
        // NextFlag = 4
    }

    public RvmConnection(
        RvmPrimitive primitive1,
        RvmPrimitive primitive2,
        uint connectionIndex1,
        uint connectionIndex2,
        Vector3 position,
        Vector3 direction,
        ConnectionType connectionTypeFlags
    )
    {
        Primitive1 = primitive1;
        Primitive2 = primitive2;
        ConnectionIndex1 = connectionIndex1;
        ConnectionIndex2 = connectionIndex2;
        Position = position;
        Direction = direction;
        ConnectionTypeFlags = connectionTypeFlags;
    }

    public RvmPrimitive Primitive1 { get; }
    public RvmPrimitive Primitive2 { get; }
    public uint ConnectionIndex1 { get; }
    public uint ConnectionIndex2 { get; }
    public Vector3 Position { get; }
    public Vector3 Direction { get; }
    public ConnectionType ConnectionTypeFlags { get; }

    public bool IsEnqueued { get; set; }

    public bool HasConnectionType(ConnectionType f)
    {
        return ConnectionTypeFlags.HasFlag(f);
    }
}
