using Newtonsoft.Json;
using System.Collections.Generic;
using System.Numerics;

namespace CadRevealComposer;

[JsonObject]
public class CadInfoNode
{
    public ulong TreeIndex { get; set; }

    public string? Name { get; set; }

    public List<CadGeometry>? Geometries { get; set; }
}

[JsonObject]
public class CadGeometry
{
    public string? TypeName;
    public Vector3 Scale;
    public Quaternion Rotation;
    public Vector3 Location;
    public Dictionary<string, string?>? Properties { get; set; }
}