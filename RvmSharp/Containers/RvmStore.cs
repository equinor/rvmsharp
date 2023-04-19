namespace RvmSharp.Containers;

using Primitives;
using System.Collections.Generic;

public class RvmStore
{
    public readonly List<RvmFile> RvmFiles = new List<RvmFile>();
    public readonly List<RvmConnection> Connections = new List<RvmConnection>();
}
