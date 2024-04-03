# Node Order Weighting

When creating sectors a number of nodes is selected until a budget is filled. Currently, one node equals one geometry. The goal is to order the nodes, before selection, so that the sectors contributes to a good user experience.This means in general that large, cheap objects should be prioritized, because they have high visual gains per performance hit.

Currently the diagonal of the node is used as the ordering key, but other ways of weighting the nodes can be more optimal.

Tested alternatives:

- Diagonal
- TriangleCount
- Diagonal / TriangleCount
- SurfaceArea
- SurfaceArea / TriangleCount
- SurfaceArea / Diagonal / TriangleCount

## Results

**NOTE:** TriangleCount and SurfaceArea was disgarded early in testing, because they looked horrible

Test images with different weightings can be found [here](https://statoilsrm.sharepoint.com/:f:/r/sites/Echo3DWeb/Shared%20Documents/RvmSharp/Spikes/Weighting?csf=1&web=1&e=pacWUI).

## Conclusion

After comparing the different images the conclusion is that there is no clear reason to change from diagonal to something else. The resulting images are either worse or quite similar.
