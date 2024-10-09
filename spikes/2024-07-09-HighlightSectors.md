# Highlight Sectors

## [Reference branch: RvmSharp](https://github.com/equinor/rvmsharp/tree/Spike/HighlightSectors)

## [Reference branch: Hierarchy Service](https://github.com/equinor/EchoHierarchyService/tree/Feature/HighlightSectors)

## [Reference branch: Echo3DWeb](https://github.com/equinor/Echo3DWeb/tree/vegst/highlight-sectors)

## Goal

The goal is to create small and easy to handle sectors with important geometry, that can be forced if highlighted. They should be small to impact the general user experience as little as possible. This will make important highlighted geometry visible from a distance, which earlier would be hidden by sector weighting and budget limits.

These sectors need to be able to coexist with the normal sectors. The hypothesis is that duplicated geometry in sectors should work like usual, and have no impact.

## Method

Accomplishing the goal includes work on RvmSharp, the hierarchy service and the client.

### RvmSharp

Two things are needed to be done in RvmSharp:

- Modifying the hierarchy
- Creating highlight sectors

#### Modifying the Hierachy

The client knows which tree indexes are highlighted, but which tree indexes each sector contains is unknown. This can be solved by creating a new column in the hierarchy database which connects tree index with a sector ids. The database will later be used by the Hierarchy service to give the client a list of highlight sector ids based on the highlighted tree indexes.

#### Creating Highlight Sectors

Creating new sectors of important geometry consists of two steps:

- Filtering out the relevant nodes
- Splitting into sectors

The filtering in this spike is done by setting a priority field on all geometries in a node of type `VALV`. After the normal splitting all the geometries with a set pritiority field is filtered out, and ran through the splitting again. These highlight sectors are given a `highlight_` as prefix to distinguish them from the normal sectors. They still need to be a part of `scene.json` to be able to get hold of them in the client.

**NOTE:**

- The highlight sector ids need to follow the normal sector ids
- The "new" root sector from the second splitting needs to be discarded and all pointers must point to the "old" root sector instead
- The highlight sector splitting should have smaller budgets, and result in smaller sectors
  - This is currenty done with a hack that divides the budgets by a divisor

### Hierarchy Service

The hierarchy service handles the database created in RvmSharp. An endpoint is created here which takes in a list of tree indexes, and returns a list of sector ids which contains these indexes.

### Echo3DWeb - The Client

In the client the list of tree indexes, that are being highlighted, is sent to the hierchy endpoint which returns a list of sector ids.

To get this list of sectors to the `EchoSectorCuller`, which handles the weighting of sectors, a hack is used. The sector ids are added as a value in the AABBs of the collections areas, which can then be accessed in the sector culler by extracting it from the `prioritized areas`. **The functionality of areas and prioritized areas lives in Reveal, and the source code might need to be studied to understand what is happening.**

With the highlight sector ids available in the sector culler, a simple weighting function will add the highlight sector if it exists in the highlighted sectors set. If it isn't, it will be discarded.  

## Results

Pictures of the result can be found [here](https://statoilsrm.sharepoint.com/:f:/r/sites/Echo3DWeb/Shared%20Documents/RvmSharp/Spikes/HighlightSectors?csf=1&web=1&e=QxT9pc).

The results show that the highlighted valves are shown no matter the budget or distance away. The cost does not seem to affect the user experience in any noticeable way (i.e. showing a lot less geometry in other places).

The overlapping geometry, because of geometry exisiting in both normal and highlight sectors seems to work as expected. There is no visible Z-fighting and selecting works as before.

## Conclusion

The first results of the functionality are promising. The reward is cleary visible, and the cost seems to be negligible during initial testing.

## Next steps

- Decide which parts are important enough to get their own highlight sectors
  - Should this global or an input parameter?
- Implementation (quite a large task)

Some challenges might arise if it is implemented:

- Handle all highlighting in the client in the correct way
  - The spike only handled highlighting on click
- The prioritzed areas hack
  - Should this rather be supported from Reveal?
- Refactor splitting to handle both normal and highlight splitting cleanly
  - Or have another splitter for highlight splitting?
- Modify the hierarchy db in a better way
  - The parallelism was broken during the spike, since the hierarchy needed info from the splitting
    - The spike solution does not seem to affect building times, but it is not pretty
