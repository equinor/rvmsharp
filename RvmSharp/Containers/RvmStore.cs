namespace RvmSharp.Containers;

using System.Collections.Generic;
using Primitives;

public class RvmStore
{
    public readonly List<RvmFile> RvmFiles = new List<RvmFile>();
    public readonly List<RvmConnection> Connections = new List<RvmConnection>();
}
