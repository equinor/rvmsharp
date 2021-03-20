namespace RvmSharp.Primitives
{
    using System;
    using System.Numerics;

    public class RvmConnection
    {
        [Flags]
        public enum ConnectionType
        {
            None = 0,
            HasCircularSide = 1 << 0, // = 1
            HasRectangularSide = 1 << 1 // = 2
            // Next = 1 << 2 // = 4
        }

        // public RvmConnection Next;
        public RvmConnection(RvmPrimitive primitive1, RvmPrimitive primitive2, uint offsetX, uint offsetY, Vector3 p, Vector3 d, ConnectionType connectionTypeFlags)
        {
            Primitive1 = primitive1;
            Primitive2 = primitive2;
            OffsetX = offsetX;
            OffsetY = offsetY;
            P = p;
            D = d;
            ConnectionTypeFlags = connectionTypeFlags;
        }

        public RvmPrimitive Primitive1 { get; init; }
        public RvmPrimitive Primitive2 { get; init; }
        public uint OffsetX { get; init; }
        public uint OffsetY { get; init; }
        public Vector3 P { get; init; }
        public Vector3 D { get; init; }
        public ConnectionType ConnectionTypeFlags { get; init; }
        
        public bool IsEnqueued { get; set; }

        public bool HasConnectionType(ConnectionType f)
        {
            return ConnectionTypeFlags.HasFlag(f);
        }
    }
}
