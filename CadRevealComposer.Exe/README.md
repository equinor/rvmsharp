# Cad Reveal Composer

## Input

3D data is read from RVM-files and metadata from TXT-files.

## Output

| File               | Note                                                                     |
| ------------------ | ------------------------------------------------------------------------ |
| debug_sectors.json | Sector information.                                                      |
| debug_zones.png    | Image showing how the model is divided into zones in the XY plane.       |
| hierarchy.db       | SQLite database, contains node metadata. Used by hierarchy REST service. |
| initialCamera.json | Initial camera position for the 3D model. Automatically calculated.      |
| sector.json        | Cognite Reveal format. Describes all sectors.                            |

OBJ-files are temporary files and are converted to CTM-files. CTM-files and I3D-files together constitutes the 3D model. F3D-files (or faces files) are level of detail files.

I3D-files contains all geometries, and pointers to meshes. Meshes are stored in CTM-files.

## Processing pipeline

#### Sector splitting

Cognite Reveal has a concept of sectors as a means to control memory and GPU resources. Sectors can be loaded and unloaded as Cognite Reveal sees fit.

Sectors may or may not overlap. As of now Cad Reveal Composer recursively splits space/voxel into eight subvoxels.

#### Zone splitting

Applicable for some installations spread over a large area, like Melkøya and Tjeldbergodden. Detects several process plants spread apart. This aids the sector splitting.

#### Exterior splitting

Sector splitting prioritizes the exterior of a platform higher up in the sector tree. This ensures that one sees the exterior of a platform first, and then load the interior.

#### Faces generation

Faces are filtered.

#### Geometry: conversion RVM to internal

#### Geometry: tesselation

`RvmPyramid` and `RvmFacetGroup` are tesselated into meshes.

#### Geometry: instance matching

`RvmPyramid` and `RvmFacetGroup` are processed to find equal geometries. Equal geometries may be offset with regards to translation, rotation and scale. The corresponding translation matrix is calculated.

## Cognite Reveal graphics performance: a note on instancing

Instancing can be used to reduce the amount of memory needed. Instead of having the same identical mesh X number of times in memory, it can be reduced to a single mesh.
But this comes at a cost in draw performance. Every instanced mesh gives a corresponding draw call. This is in contrast to meshes which are NOT instanced. They are all combined and drawn in a single draw call.
