# Implementing a LOD system without forking Reveal

## [Reference branch](https://github.com/equinor/rvmsharp/tree/Spike/LodInReveal)

## Goal

To be able to make a LOD system we would like to switch between sectors containing detailed and course model parts. Ideally we would like to achieve this by only editing the Echo3DWeb code, and leave the Reveal code untouched.

## Method

By utilizing the sector weighting and the fact that we discard the sectors that are not interesting, we can choose which of the sectors we want based on the weighting criteria. With the detailed sector named "sector_1" and the course sector named "shadow_sector_1" (the names are arbitrary) we can simply give the shadow sector a weight of 0 when the detailed model should be shown, and the detailed sector a weight of 0 when the course model should be shown.

This can be achieved in frontend by just doing:

```js
if (zone === CutoffZone.Near) {
  if (sector.sectorFileName?.includes('shadow')) {
    return 0;
  }
  return (
    SectorZonePriority.zonePriorityTop + percentOfScreenFilledByLargestNode
  );
}

if (!sector.sectorFileName?.includes('shadow')) {
  return 0;
}
return SectorZonePriority.zonePriorityLow + percentOfScreenFilledByLargestNode;
```

## Evidence

![LOD System GIF](./images/LODSystemWithoutForking/LOD.gif)

The magenta sectors are hacked in and are just colored duplicates of the detailed sectors. The level of detail of the duplicates, is the same as for the original sectors. This is done just to illustrate the loading and unloading of sectors based on the weighting of the sectors.

The code for creating the magenta sectors can be found in this [branch](https://github.com/equinor/rvmsharp/tree/Temp/Shadow).

## Conclusion

This works fine, and it seems that we have everything we need to implement a decent LOD system without forking Reveal.

**_NOTE:_**
Sometimes there is a frame between unloading a detailed/shadow sector and loading a shadow/detailed sector. This has probably always been there, but is more visible now when switching between identically placed and sized sectors. It looks like the the model is "blinking". This is not that visible in the attached GIF.

## Next steps

- This solution doubles the amount of sectors, so it is worth investigating if every sector needs a shadow sector.

- Optimize the weighting for user experience
  - This is most likely only possible when we have an actual course model available.
