# Facet Group Storage and Matching

Performed by: nih with GPT-6 Astra via Copilot.

## Goal

Reduce allocations and repeated work in facet-group processing without changing matching criteria or introducing a different matching algorithm.

## Changes

### Packed Geometry and Views

- Polygon and contour records are value types backed by shared polygon, contour, and vertex/normal arrays. `ArraySegment<T>` views replace separate child arrays.
- Parsing stays single-pass, using temporary lists before creating the packed buffers. Stable polygon sorting moves views, not vertex data.
- Transforms allocate fresh buffers and use normalized inverse-transpose normals, preserving zero normals. Bounding boxes use a single traversal.
- Tessellation centers positions directly in the LibTess input, avoiding an intermediate contour hierarchy.

Views do not enforce immutability: geometry must be treated as read-only after construction. Retaining a view also retains its entire backing array.

### Prepared Templates and Reusable Candidate Positions

- Templates cache positions in logical polygon/contour order, anchor indices, and template-only transform calculations. Failed anchor selection is cached too.
- Each topology bucket reuses a candidate position buffer. World-space positions are computed once per candidate, without baking unused normals, bounds, or contour objects.
- Full centered geometry is created only when a candidate becomes a new template. Cached data stays attached to its template during promotion.

Four anchors propose a transform; every vertex must still pass the 1 mm tolerance. Anchor selection, topology bucketing, cleanup, filtering, and ranking are unchanged. This reduces the cost per attempt, not the number of attempts; worst-case matching remains quadratic.

## Benchmark

The datasets are private. Only their acronyms, approximate sizes, and rounded relative results are reported here; detailed counts, absolute performance measurements, and input inventories are omitted.

**The comparison isolates matcher optimization. Both versions use packed storage and current normal handling, so these results do not measure parser/storage improvements.**

Measurements used a Release build on Apple Silicon with .NET 10 and workstation GC. A temporary harness reconstructed the baseline matcher from commit `8d00cafd45af628c078907de56d7e4a850631c95` and ran both versions against the same parsed geometry. After warmup, execution order was baseline, current, current, baseline, with explicit garbage collection before each run. Both used the same minimum-instance threshold and template limit.

ASG-ASB contains roughly half a million facet groups, all included in the benchmark. JCA-ASB contains roughly 3.6 million facet groups, of which about 3.4 million were included. For JCA-ASB, memory constraints required discarding topology buckets below the matching threshold during import. All eligible buckets were processed together, preserving comparisons and global template selection; overhead for discarded groups is excluded.

Timing covers grouping, matching, template reduction, and logging, but excludes parsing, tessellation, and export. Allocations are cumulative bytes allocated during matching, not retained or peak memory.

## Results

Rounded results based on the mean of two runs per version:

| Dataset | Matcher speedup | Allocation reduction |
| --- | ---: | ---: |
| ASG-ASB | About 2.4x | About 65% |
| JCA-ASB | About 2.6x | About 60% |

All runs produced identical instance-family assignments, result kinds, template selections, and transforms, with unchanged comparison counts. Focused matcher and algebra tests passed, including randomized transforms, buffer reuse, template promotion, and solver edge cases. Parser and tessellation tests covered shared buffers, ordering, empty contours, and holes.

## Limitations

- Two runs per version provide a quick comparison, not statistical certainty. Matching outputs agreed on these datasets, but rendering equivalence and end-to-end conversion were not measured.
- Prepared templates retain extra positions and solver state in exchange for less repeated work and candidate allocation.
- Planar and degenerate templates remain unsupported. More aggressive filtering must account for supported transforms, including non-uniform scaling.
- Packed-storage and tessellation improvements need separate measurements.

## Conclusion

Retain the packed representation and prepared-template matcher. The matcher was more than twice as fast with substantially fewer allocations and identical observed output. Profile discovery and full-vertex verification before changing matching criteria; measure end-to-end conversion separately.

## Implementation

- [Packed facet-group representation](../RvmSharp/Primitives/RvmFacetGroup.cs)
- [Parser](../RvmSharp/RvmParser.cs)
- [Packed geometry transformation](../RvmSharp/Operations/RvmPrimitiveExtensions.cs)
- [Tessellation bridge](../RvmSharp/Tessellation/TessellatorBridge.cs) and [LibTess input](../RvmSharp/Tessellation/TessNet.cs)
- [Matcher](../CadRevealRvmProvider/Operations/RvmFacetGroupMatcher.cs) and [transform solver](../CadRevealComposer/Utils/AlgebraUtils.cs)
