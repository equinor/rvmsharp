# Cad Reveal Composer

The Cad Reveal Composer project uses the rvmsharp parser to read RVM data, and converts the data into the Reveal v8 format - ready for use in the Reveal 3D viewer.

#### Abbreviations
- Cognite Reveal: open source graphics engine
- PDMS: Plant Design Management System
- RVM: AVEVA Plant Design Management System Model project file
- OBJ file: contains meshes, text format
- CTM file: contains meshes, compressed binary format
- I3D file: contains primitives and pointers to meshes in CTM file, compressed binary format
- F3D file: contains level of detail


## Input

Geometry data is read from RVM files and PDMS metadata from TXT files (tag data and node hierarchy).

## Output

| File               | Note                                                                     |
| ------------------ | ------------------------------------------------------------------------ |
| debug_sectors.json | Sector information.                                                      |
| debug_zones.png    | Image showing how the model is divided into zones in the XY plane.       |
| hierarchy.db       | SQLite database, contains node metadata. Used by hierarchy REST service. |
| initialCamera.json | Initial camera position for the 3D model. Automatically calculated.      |
| sector.json        | Describes all sectors (Cognite Reveal format).                           |

- OBJ files are temporary files and are converted to CTM files.
- CTM files and I3D files together constitutes the 3D model.
- F3D files (or faces files) are level of detail files.
- I3D files contains all geometries, and pointers to meshes. Meshes are stored in CTM files.

## Processing pipeline

#### Sector splitting

Cognite Reveal has a concept of sectors as a means to control loading and unloading of memory and GPU resources. Sectors can be loaded and unloaded as Cognite Reveal sees fit.

> NOTE: Sectors may or may not overlap.

As of now Cad Reveal Composer recursively splits space into eight subvoxels using X/Y/Z midpoint.

#### Zone splitting

Applicable for some installations spread over a large area, like Melkøya and Tjeldbergodden. Zone splitting detects several processing plants spread apart within the model.

Each zone is used as a basis for sector splitting.

#### Exterior splitting

Given a set of geometries separate them into exterior and interior. The exterior is found using ray tracing from the outside hitting the exterior geometries. The rest is considered interior geometries.

The exterior geometries are prioritized in sector loading.

#### Geometry: tesselation

`RvmPyramid` and `RvmFacetGroup` are tesselated into meshes.

#### Geometry: instance matching

Both `RvmPyramid` and `RvmFacetGroup` are processed to find equal geometries. Equal geometries meaning they have the same shape, but may be offset with regards to translation, rotation and scale within the model.

The corresponding transformation matrix is calculated and applied at runtime. Instancing of geometries is a generic feature of graphics APIs (DirectX, OpenGL, WebGL etc.).

## Cognite Reveal graphics performance: a note on instancing

Instancing can be used to reduce the amount of memory needed. Instead of having the same identical mesh X number of times in memory, it can be reduced to a single mesh in memory.
But this comes at a cost in draw performance. Every instanced mesh gives a corresponding draw call. This is in contrast to meshes which are NOT instanced. They are all combined and drawn in a single draw call.
