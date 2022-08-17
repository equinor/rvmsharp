# RvmSharp

[![Build Status](https://dev.azure.com/EquinorASA/DT%20%E2%80%93%20Digital%20Twin/_apis/build/status/equinor.rvmsharp?branchName=master)](https://dev.azure.com/EquinorASA/DT%20%E2%80%93%20Digital%20Twin/_build/latest?definitionId=86&branchName=master)

A library and utility for reading and converting RVM files into something else.

Library supports .NET 6.0 and .NET Standard 2.1

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

To tessellate:

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

```ps1
# Run the command below to see the required arguments.
dotnet.exe run --configuration Release --project "RvmSharp.Exe/RvmSharp.Exe.csproj" -- --help # Replace '--help' with your arguments.
```

## Cad Reveal Composer

The CAD Reveal Composer is a fast converter for RVM files into the Reveal formats used by the [cognite/reveal](https://github.com/cognitedata/reveal) 3D Viewer.

It can be invoked using the script below:

```ps1
.\scripts\Convert-RvmToI3df.ps1 -InputDirectory $AFolderWithRvmAndTxtFiles -ProjectId 1 -ModelId 2 -RevisionId 1 -ArtifactDirectory $OutputDirectory 
```

Invoke from IDE

```ide
-i $AFolderWithRvmAndTxtFiles -o $OutputDirectory -ProjectId 1 -ModelId 2 -RevisionId 1 
```

Invoke from CLI
```cli
dotnet run -c Release --project "..\rvmsharp\CadRevealComposer.exe" -i $AFolderWithRvmAndTxtFiles -o $OutputDirectory --ProjectId 1 --ModelId 1 --RevisionId 1
```

## Example Data

Equinor has published the Huldra dataset, including RVM files to the public.

<https://data.equinor.com/dataset/Huldra>

Visit this link and create a user to download a complete source dataset of an oil platform, including RVM files that can be used with `rvmsharp`.

## Contribution

If you find some nice optimizations or tricks, do not hesitate to create an issue discussing if it should be contributed to the `rvmsharp` project.

While any contributions are appreciated please make an issue to discuss the approach before coding a major change. This helps both us and you set expectations :)

## Credits

This project is based on [rvmparser](https://github.com/cdyk/rvmparser) by @cdyk

This repository contains sample data from the Equinor ASA - Huldra Dataset <https://data.equinor.com/dataset/Huldra>. A copy of the license can be found [here](./TestSamples/Huldra/Equinor_open_data_sharing_license_-_Huldra.pdf).
