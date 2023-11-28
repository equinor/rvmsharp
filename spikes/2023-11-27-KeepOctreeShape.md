# Keep the octree shape when sector splitting

## [Reference branch](https://github.com/equinor/rvmsharp/commits/Spike/KeepOctreeShape)

## Goal

Try to keep the octree shape when sector splitting instead of making tighter bounding sectors. 
This is done to avoid making unfavorable sector shapes such as oblong sectors.
We believe this will enhance user experience.

## Method
Calculate the bounding box encapsulating all nodes as usual. Make this bounding box cubed by increasing/decreasing the x, y, and z values based on the
largest value. This implies that the size of the cubed bounding box is always greater than or equal the size of the original bounding box.

For instance:

Bounding box data:

- X_min = 1, X_max = 3
- Y_min = 4, Y_max = 8
- Z_min = 0, Z = 6

Implies cubed bounding box data:

- X_min = -1, X_max = 5
- Y_min = 2, Y_max = 6
- Z_min = 0, Z = 6

## Results
The following images show the difference between main (tight bounding boxes) and the KeepOctreeShape branch.



