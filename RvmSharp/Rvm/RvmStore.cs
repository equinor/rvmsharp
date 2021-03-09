namespace rvmsharp.Rvm
{
    using rvmsharp.Rvm.Primitives;
    using System;
    using System.Collections.Generic;

    public class RvmStore
    {
        public readonly List<RvmFile> RvmFiles = new List<RvmFile>();
        public readonly List<RvmConnection> Connections = new List<RvmConnection>();
    }
}
