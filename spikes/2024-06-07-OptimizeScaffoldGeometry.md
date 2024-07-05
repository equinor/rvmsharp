# Optimization of scaffold geometry

## [Reference branch](todo)

## Goal

Today, it is not tractable to display more than one scaffold in Echo3D, due to the scaffolds high level of detail (i.e., the number of faces can reach as high as 22 * 10^6). Hence, we wish to explore different ways of optimizing the geometry of our scaffold models, while aiming to reach a balance between reduced face-count and level of detail.

## Method

We will explore various methods of geometry optimization, such as axes aligned bounding boxes (AABB) and convex hull (CHULL), as well as other algorithms focused on reducing the geometry face-count. The optimization will primarily take place in the FbxProvider::ProcessGeometries() method. In a final implementation we need to decide if the applied optimization is only going to affect scaffold models or also other imported FBX models.

## Results

### Pure conversion to axis aligned boundary

An un-optimized model is shown below, with 11 217 834 faces:

![Non-optimized scaffold 1](./images/OptimizeScaffoldGeometry/figure1.png)

We start by implementing AABB around each object in the model, starting with **only including instanced meshes**, since these seem to be the most prevalent in the scaffolding models. This yields a face count of 6 948 faces, amounting to a 99.94% face-reduction. The result is shown below:

![AABB optimized scaffold 1](./images/OptimizeScaffoldGeometry/figure2.png)

This still looks visually OK. However, AABB may not work on scaffolds that have slanted angles or contain very large objects that themselves have high complexity. Such a scaffold is shown below, with 27 207 628 faces in total:

![non-optimized scaffold 2](./images/OptimizeScaffoldGeometry/figure3.png)

Note that the railings of the scaffold is missing. This is because the CAD budget of 15 000 000 faces was not sufficient to draw the entire model and the more complex railings were excluded. However, by artificially raising the budget to 45 000 000, we get the result below:

![non-optimized scaffold 2 with increased CAD budget](./images/OptimizeScaffoldGeometry/figure9.png)

In the following we will again decrease the CAD budget to 15 000 000. Running AABB on this scaffold results in the following, with 56 191 faces, yielding a reduction of 99.79%.

![AABB optimized scaffold 2](./images/OptimizeScaffoldGeometry/figure4.png)

As can be seen, each object is first subjected to axis aligned bounding boxing and thereby rotated into place. Hence, there will not be any issues with non-axis aligned objects.

### Introducing conversion to convex hull

The stairs in the scaffolding come as single large objects and are therefore represented as large boxes in case of AABB conversion. This simplification is too crude and must be mitigated. One way to achieve a better result is to replace the shapes with convex hulls (CHULL). We will use the MIConvexHull C# package, installed by

``` bash
dotnet add package MIConvexHull --version 1.1.19.1019
```

The first attempt, using a convex hull tolerance of 1E-100, yielded a face-count of 613 055 faces, which is a reduction of 97.75%. The result is shown below:

![Convex hull optimized scaffold 2 with a tolerance of 1E-100](./images/OptimizeScaffoldGeometry/figure5.png)

Reducing the tolerance to 1E-1 yielded a face-count of 80 741 faces, which is a reduction of 99.70% compared to the un-optimized geometry. The result is shown below, but does not visually differ considerably from the case with tolerance 1E-100:

![Convex hull optimized scaffold 2 with a tolerance of 1E-1](./images/OptimizeScaffoldGeometry/figure6.png)

Although the stairs of the scaffold are better than for AABB, they are still somewhat crudely represented. We will therefore seek a method of drawing such object with more detail.

### Volume distribution

Below is a histogram of the volume for parts within the un-optimized scaffold 2 model:

![Histogram of the volume for parts within the un-optimized scaffold 2 model](./images/OptimizeScaffoldGeometry/figure8.png)

It can be seen that the parts with large volume, such as the staircases and their corresponding railings show up in the histogram as small spikes with low frequency, $f$. If we decide only to optimize for $V<8$ using convex hull with tolerance 1E-1, we achieve 345 261, yielding a face reduction of 98.73%. The result is shown below:

![Convex hull optimized scaffold 2 with a tolerance of 1E-1, but only optimize for $V<8$](./images/OptimizeScaffoldGeometry/figure7.png)

### Introducing the Gradient space library and vertex reduction

Increasing from 80 741 to 345 261 faces by including more accurate staircases is still expensive. Hence, we tried to instead apply the Reduce() method from the Gradientspace library to produce a reduced set of vertices for models with $V\geq 8$. However, this did not succeed, since the DMesh3::CheckValidity() function failed (due to non-manifold parts), hence implying too complex geometry for the reducer algorithm to handle.

### Combining AABB, convex hull, and vertex reduction

In the following, we will use key strings within the part names to identify which optimization method to use. Also note that parts, such as scaffold spears (FS), will contain geometries that are disjoint. We have implemented an algorithm that can separate out the disjoint parts, such that they can be optimized separately and then be rejoined. The table below shows the first attempt:

| Key string                  | Optimization                                                                                    |
|-----------------------------|-------------------------------------------------------------------------------------------------|
| Plank                       | These are the boards of the scaffold and are replaced by axis aligned bounding boxes.           |
| FS, Stair, Base Element     | Spears, stairs, and base elements are first disjoined and then replaced by convex hulls.        |
| Beam                        | Ledger beams are replaced by ledger beams drawn by RvmSharp, based on their bounding boxes.     |
| Kick board, BRM             | Kick boards and feet of spears are converted to convex hulls.                                   |
| **The rest**                | Other parts are decimated using the gradient space reduce algorithm based on minimum distance.  |

We have also this time included both instanced meshes, as well as triangle meshes. We then achieved a reduction from 27 230 918 to 1 574 223 faces, yielding a reduction of 94.22%. The histogram distribution of separate part groups is shown below before and after optimization:

![Histrogram of face-count for optimized scaffold 2 with the above optimization strategies](./images/OptimizeScaffoldGeometry/figure10.png)

The screenshot below shows the optimized result. Specifically, the ledger beams that are now instead generated by RvmSharp. It can also be seen tha the stairs must be handled differently. Below, these are separated and replaced by convex hulls. However, the left and right support beams for the stairs are in one piece and therefore result in an all-encompassing convex hull. The disjoining of the spears can be observed to work well, also showing clearly the distance markers.

![Optimized scaffold 2 with the above optimization strategies](./images/OptimizeScaffoldGeometry/figure11.png)

We now attempt to fix the stairway guards by utilizing only decimation, as well as to split the stairs into their separate parts, and using decimation for the largest part by volume, where the remaining parts are replaced by axis aligned bounding boxes. It is then assumed that the part with the largest bounding box volume will encompass all other parts of the stairs either fully or partially and that the smaller parts are well approximated by bounding boxes (e.g., the steps and the planks of the staircase). This assumption may not hold for all stairs and should be looked into closer for a real implementation, but perhaps using a similar principle of selection. The updated key strings and their associated optimization methods are listed below:

| Key string                  | Optimization                                                                                      |
|-----------------------------|---------------------------------------------------------------------------------------------------|
| Plank                       | These are the boards of the scaffold and are replaced by axis aligned bounding boxes.             |
| FS, Base Element            | Spears, and base elements are first disjoined and then replaced by convex hulls.                  |
| Stair                       | The largest volume part is decimates, while the rest are replaced by axis aligned bounding boxes. |
| Beam                        | Ledger beams are replaced by ledger beams drawn by RvmSharp, based on their bounding boxes.       |
| Kick board, BRM             | Kick boards and feet of spears are converted to convex hulls.                                     |
| StairwayGuard               | Decimated using the gradient space reduce algorithm based on minimum distance.                    |
| **The rest**                | Other parts are decimated using the gradient space reduce algorithm based on minimum distance.    |

With these optimization strategies we find a reduction of faces for scaffold 2 to 1 607 683 faces, which yields a 94.10% reduction. This is only a slight increase compared to when stairs and their guards were represented by convex hulls.

![Histrogram of face-count for optimized scaffold 2 with the above optimization strategies](./images/OptimizeScaffoldGeometry/figure12.png)

The corresponding scaffold is shown below:

![Optimized scaffold 2 with the above optimization strategies](./images/OptimizeScaffoldGeometry/figure13.png)

## Conclusion

We have tried coarse geometry optimization by applying axis aligned bounding boxes for all parts, as well as convex hull for all parts. These were able to reduce face counts from the order of 27M down to between 60k and 80k faces, but yielded too poor quality of the resulting scaffolds.

The best strategy was found to be by using different optimization strategies based on part name, where we used a combination of axis aligned bounding boxes, convex hulls, mesh decimation, and drawing lower resolution shapes that resemble the originals. We also divided parts into their respective disjoint parts and used geometric criteria to chose optimization methods. Using this, we could reduce face counts from the order of 27M to 1.6M, while still retaining a very detailed view of the scaffolds. However, by continuing this strategy, it should be possible to reduce the face count even further with decent details of the optimized scaffolds.

## Next steps

Create a proper implementation based on the code and findings of the spike, using the same strategy as recommended in the conclusion. To achieve this, a more detailed overview of the scaffold parts must be made based on the scaffolds catalogues such that a secure optimization can be made. For example, it may be that some stairs consist of two large-volume parts which bounding boxes both cover smaller parts of the stairs. In that case, it would not suffice to only exclude the one part with largest bounding box from bounding box conversion, as done in the spike.
