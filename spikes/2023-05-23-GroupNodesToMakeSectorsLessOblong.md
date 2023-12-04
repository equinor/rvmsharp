# Group nodes to make sectors less oblong

## [Reference branch](https://github.com/equinor/rvmsharp/tree/Spike/GroupNodesToReduceOblongity)

## Goal

Try to make sectors less oblong to achieve a better user experience. Group nodes so that the initial splitting point is a cube, instead of a large and flat rectangular box.

## Method

To avoid sectors being to oblong, the nodes are being grouped in cubes. If the bounding isn't "cubish" enough, the shortest side is used as a base to create cubes. The hope is that a more cubish base will create less oblong sectors.

For instance:

Bounding box lengths:

- X = 1
- Y = 2
- Z = 3

Will give six cubes 1 x 2 x 3. The nodes will then be placed based on center of node in the right cube.

## Evidence

The following screenshots are made at different depths, but at a point where the sectors are approximately equal in size. The difference in depths is just a result of the splitting being done differently.

Screenshot of TrollA before grouping:
![Troll A before grouping](https://statoilsrm.sharepoint.com/:i:/r/sites/Echo3DWeb/Shared%20Documents/RvmSharp/Spikes/Resources/GroupNodesToMakeSectorsLessOblong/trolla.png?csf=1&web=1&e=nmki3q)

Screenshot of TrollA after grouping:
![Troll A after grouping](https://statoilsrm.sharepoint.com/:i:/r/sites/Echo3DWeb/Shared%20Documents/RvmSharp/Spikes/Resources/GroupNodesToMakeSectorsLessOblong/trolla_group.png?csf=1&web=1&e=Y3bL4m)

Screenshot of Melkøya before grouping:
![Melkøya before grouping](https://statoilsrm.sharepoint.com/:i:/r/sites/Echo3DWeb/Shared%20Documents/RvmSharp/Spikes/Resources/GroupNodesToMakeSectorsLessOblong/melkoya.png?csf=1&web=1&e=pcx8jP)

Screenshot of Melkøya after grouping:
![Melkøya after grouping](https://statoilsrm.sharepoint.com/:i:/r/sites/Echo3DWeb/Shared%20Documents/RvmSharp/Spikes/Resources/GroupNodesToMakeSectorsLessOblong/melkoya_group.png?csf=1&web=1&e=WnTHoE)

## Conclusion

This method of solving oblong sectors are not satisfactory, and should not be the chosen way forward.

## Next steps

- Find another approach to handle oblong sectors
- Maybe it is possible to handle oblong sectors when they appear during splitting
