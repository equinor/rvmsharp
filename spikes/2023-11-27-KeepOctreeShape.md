# Keep the octree shape when sector splitting

## [Reference branch](https://github.com/equinor/rvmsharp/commits/Spike/KeepOctreeShape)

## Goal

Try to keep the octree shape when sector splitting instead of making tighter bounding sectors. 
This is done to avoid making unfavorable sector shapes such as oblong sectors.
We believe this will enhance user experience.

## Method
Calculate the bounding box encapsulating all nodes as usual. Make this bounding box cubed (which we call cubify) by increasing/decreasing the x, y, and z values based on the
largest value. This implies that the size of the cubed bounding box is always greater than or equal the size of the original bounding box.

For instance:

Bounding box data:

- $X_{min}$ = 1, &emsp; $X_{max}$ = 3
- $Y_{min}$ = 4, &emsp; $Y_{max}$ = 8
- $Z_{min}$ = 0, &emsp; $Z_{max}$ = 6

Implies cubed bounding box data:

- $X_{min}$ = -1, &emsp; $X_{max}$ = 5
- $Y_{min}$ = 2,  &emsp; $Y_{max}$ = 6
- $Z_{min}$ = 0,  &emsp; $Z_{max}$ = 6

## Results
The following images show the difference between main (tight bounding boxes) and the KeepOctreeShape branch.

#### Aasta Hansteen
Main:
![main_AHA](https://github.com/equinor/rvmsharp/assets/141636529/829a618d-d181-4c5f-a76a-f67f7f516393)

Keep Octree Shape branch:
![spike_AHA](https://github.com/equinor/rvmsharp/assets/141636529/9fdd2ee9-3d02-4640-9ce1-c8cf3af81a42)


#### Kårstø
Main:
![main_KAA](https://github.com/equinor/rvmsharp/assets/141636529/79e3b73e-bb71-4d1e-99fb-d36544508be9)

Keep Octree Shape branch:
![spike_KAA](https://github.com/equinor/rvmsharp/assets/141636529/3262ef52-c3f0-4c70-b0ba-f67069cf9ee6)

As evident from the figures, many sectors have increased significantly in size.
However, the user experience of moving around in these two models remain unchanged after testing on a local development computer.
A possible implimentation of a smarter algorithm can be investigated.

Examples to a smart cubification algorithm:
- Smart cubification of oblong sectors
  - If the original sector has lengths in any directions that are substantially longer than the others, do not cubify,
  - Or implement a partial cubification of those sectors
- Only cubify if the percentage volume increase between the original and cubified sectors are below a certain threshold
- Not cubifying at higher depths

## Conclusion
The gain is currently unknown, however there were no apparent downsides when testing the model on a local development computer.
Since there are currently no apparent upside nor downside we recommend to not merge this change.

## Next steps
- Implement a smart cubification algorithm


