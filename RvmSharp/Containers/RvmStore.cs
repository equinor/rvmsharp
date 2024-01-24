namespace RvmSharp.Containers;

using Primitives;
using System.Collections.Generic;

public class RvmStore
{
    public readonly List<RvmFile> RvmFiles = [];
    public readonly List<RvmConnection> Connections = [];
}
