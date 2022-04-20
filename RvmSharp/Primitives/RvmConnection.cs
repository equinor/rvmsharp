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
        HasRectangularSide = 2
        // NextFlag = 4
    }

    public RvmConnection(RvmPrimitive primitive1, RvmPrimitive primitive2, uint offsetX, uint offsetY, Vector3 position, Vector3 direction, ConnectionType connectionTypeFlags)
    {
        Primitive1 = primitive1;
        Primitive2 = primitive2;
        OffsetX = offsetX;
        OffsetY = offsetY;
        Position = position;
        Direction = direction;
        ConnectionTypeFlags = connectionTypeFlags;
    }

    public RvmPrimitive Primitive1 { get; }
    public RvmPrimitive Primitive2 { get; }
    public uint OffsetX { get; }
    public uint OffsetY { get; }
    public Vector3 Position { get; }
    public Vector3 Direction { get; }
    public ConnectionType ConnectionTypeFlags { get; }

    public bool IsEnqueued { get; set; }

    public bool HasConnectionType(ConnectionType f)
    {
        return ConnectionTypeFlags.HasFlag(f);
    }
}