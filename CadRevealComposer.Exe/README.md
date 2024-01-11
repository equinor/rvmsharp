# Cad Reveal Composer

The Cad Reveal Composer project uses the rvmsharp parser to read RVM data, and converts the data into the Reveal v8
format - ready for use in the Reveal 3D viewer.

#### Abbreviations

- Cognite Reveal: open source graphics engine
- PDMS: Plant Design Management System
- RVM: AVEVA Plant Design Management System Model project file
- OBJ file: contains meshes, text format

## Input

Geometry data is read from RVM files and PDMS metadata from TXT files (tag data and node hierarchy).

## Output

| File               | Note                                                                     |
|--------------------|--------------------------------------------------------------------------|
| hierarchy.db       | SQLite database, contains node metadata. Used by hierarchy REST service. |
| initialCamera.json | Initial camera position for the 3D model. Automatically calculated.      |
| sector.json        | Describes all sectors (Cognite Reveal format).                           |

- .glb files together constitutes the 3D model, including the primitives using gltf extensions.

## Dev Tools

### DevPrimitiveCache

To facilitate faster iteration times, a serialization and deserialization of primitives has been implemented.
This helps avoid repetitive processing of stuff that is not needed for configuring splitting for instance.

NOTE: Using the DevPrimitiveCache may introduce bugs in your code, and is not valid for production use!
If you edit anything in the primitives such as names or add fields they MAY deserialize correctly, but give wrong data.
Also any changes before the APrimitives has been made will not be ran, and there is no smart cache invalidation
techniques implemented.

How to:

Add the `--DevPrimitiveCacheFolder` parameter. Either with a full path, or a relative path.

When this is added the first next run will check if there is a cache for the input folder you supplied. For instance: "hda_rvm", and load that if it exists.
If it does NOT exist, we will process the files as normal but save a cache when the primitives are read. Then the next time you start the primitives should use the cache.

#### Troubleshooting PrimitiveCache:

**The cache file fails to load:** Delete the cache file and build a new one. Its probably outdated.

## Processing pipeline

#### Sector splitting

Cognite Reveal has a concept of sectors as a means to control loading and unloading of memory and GPU resources. Sectors
can be loaded and unloaded as Cognite Reveal sees fit.

> NOTE: Sectors may or may not overlap.

As of now Cad Reveal Composer recursively splits space into eight subvoxels using X/Y/Z midpoint.

#### Geometry: tesselation

`RvmPyramid` and `RvmFacetGroup` are tessellated into meshes.

#### Geometry: instance matching

Both `RvmPyramid` and `RvmFacetGroup` are processed to find equal geometries. Equal geometries meaning they have the
same shape, but may be offset with regards to translation, rotation and scale within the model.

The corresponding transformation matrix is calculated and applied at runtime. Instancing of geometries is a generic
feature of graphics APIs (DirectX, OpenGL, WebGL etc.).

## Cognite Reveal graphics performance: a note on instancing

Instancing can be used to reduce the amount of memory needed. Instead of having the same identical mesh X number of
times in memory, it can be reduced to a single mesh in memory.
But this comes at a cost in draw performance. Every instanced mesh gives a corresponding draw call. This is in contrast
to meshes which are NOT instanced. They are all combined and drawn in a single draw call.
