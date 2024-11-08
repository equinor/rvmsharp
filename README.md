# RvmSharp

[![SCM Compliance](https://scm-compliance-api.radix.equinor.com/repos/equinor/rvmsharp/badge)](https://scm-compliance-api.radix.equinor.com/repos/equinor/rvmsharp/badge)

<https://www.nuget.org/packages/Equinor.RvmSharp>

A library and utility for reading and converting RVM files.

Library supports .NET 8.0+ and .NET Standard 2.1

This repo also contains a pipeline for converting from Rvm to the Reveal file format. See [CadRevealComposer](#cad-reveal-composer)

## RvmSharp Dependencies

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

The CAD Reveal Composer is a fast converter for RVM files into the Reveal formats used by
the [cognite/reveal](https://github.com/cognitedata/reveal) 3D Viewer.

Invoke from CLI

```cli
dotnet run -c Release --project "..\rvmsharp\CadRevealComposer.exe" -i $AFolderWithRvmAndTxtFiles -o pathto/reveal/examples/public/primitives --ProjectId 1 --ModelId 1 --RevisionId 1
```

## Example Data

Equinor has published the Huldra dataset, including RVM files to the public.

<https://data.equinor.com/dataset/Huldra>

Visit this link and create a user to download a complete source dataset of an oil platform, including RVM files that can
be used with `rvmsharp`.

## Contribution

If you find some nice optimizations or tricks, do not hesitate to create an issue discussing if it should be contributed
to the `rvmsharp` project.

While any contributions are appreciated please make an issue to discuss the approach before coding a major change. This
helps both us and you set expectations :)

### Formatting

We use csharpier for formatting.

You can run it manually by running the following in your command line.

```ps
dotnet tool restore
dotnet csharpier .
```

Install the addon for your IDE, and enable formatting when saving files to avoid spending time on formatting!

> Remark:<br>
> You need to run `dotnet tool restore` to install and update csharpier for the IDE addons to work as expected.

## Releasing NuGet

To update the RvmSharp package on nuget check the internal docs: <https://github.com/equinor/Echo/tree/master/docs/rvmsharp-sop-nuget.md>

## Credits

This project is based on [rvmparser](https://github.com/cdyk/rvmparser) by @cdyk

This repository contains sample data from the Equinor ASA - Huldra Dataset <https://data.equinor.com/dataset/Huldra>. A
copy of the Huldra dataset license can be found [here](./TestSamples/Huldra/Equinor_open_data_sharing_license_-_Huldra.pdf).
