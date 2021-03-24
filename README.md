# RvmSharp

A library and utility for reading and converting RVM files into something else.

Library supports .NET 5.0 and .NET Standard 2.0 (Unity compatible)

## Dependencies

- [LibTessDotNet](https://github.com/speps/LibTessDotNet)
- [System.Numerics.Vectors](https://www.nuget.org/packages/System.Numerics.Vectors/)

## Usage

To read an RVM file:

```csharp
using var stream = File.OpenRead(rvmFilename);
RvmFile rvmFile = RvmParser.ReadRvm(stream);
rvmFile.AttachAttributes(txtFilename);
```

To tesselate:

```csharp
var rvmStore = new RvmStore();
rvmStore.RvmFiles.AddRange(rvmFiles);
RvmConnect.Connect(rvmStore);
RvmAlign.Align(rvmStore);
var leafs = rvmStore.RvmFiles.SelectMany(rvm => rvm.Model.Children.SelectMany(CollectGeometryNodes)).ToArray();

private static IEnumerable<RvmNode> CollectGeometryNodes(RvmNode root)
{
    if (root.Children.OfType<RvmPrimitive>().Any())
        yield return root;
    foreach (var geometryNode in root.Children.OfType<RvmNode>().SelectMany(CollectGeometryNodes))
        yield return geometryNode;
}
```

RvmSharp.Exe is a sample application using RvmSharp to process and export triangulated OBJ models.

## Contribution

## Credits

This project is based on [rvmparser](https://github.com/cdyk/rvmparser)
