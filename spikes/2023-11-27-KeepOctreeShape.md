# Keep the octree shape when sector splitting

## [Reference branch](https://github.com/equinor/rvmsharp/commits/Spike/KeepOctreeShape)

## Goal

Try to keep the octree shape when sector splitting instead of making tighter bounding sectors. 
This is done to avoid making unfavorable sector shapes such as oblong sectors.
We believe this will enhance user experience.

## Method
