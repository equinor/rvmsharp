namespace rvmsharp.Rvm.Primitives
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using System.Text;
    using System.Threading.Tasks;

    public class RvmConnection
    {
        [Flags]
        public enum Flags
        {
            None,
            HasCircularSide,
            HasRectangularSide
        }

        public RvmConnection Next;
        public RvmPrimitive p1;
        public RvmPrimitive p2;
        public uint OffsetX;
        public uint OffsetY;
        public Vector3 p;
        public Vector3 d;
        public uint temp; // TODO: better name
        public Flags flags;
        public void AddFlag(Flags f)
        {
            flags |= f;
        }
        public bool HasFlag(Flags f)
        {
            return (flags & f) != 0;
        }

    }
}
