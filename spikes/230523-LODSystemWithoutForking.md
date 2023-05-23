# Implementing a LOD system without forking Reveal

## Goal

To be able to make a LOD system we would like to switch between sectors containing detailed and rough model parts. Ideally we would like to achieve this by only editing the Echo3DWeb code, and leave the Reveal code untouched.

## Method

By utilizing the sector weighting and the fact that we discard the sectors that are not interesting, we can chooses which of the sectors we want based on the weighting criteria. With the detailed sector named "sector_1" and the rough sector named "shadow_sector_1" (the names are arbitrary) we can simply give the shadow sector a weight of 0 when the detailed model should be shown, and the detailed sector a weight of 0 when the rough model should be shown.

This can be achieved in frontend by just doing (prototype):

```js
if (zone === CutoffZone.Near) {
        if (sector.sectorFileName?.includes("shadow")){
            return 0
        }
        return SectorZonePriority.zonePriorityTop + percentOfScreenFilledByLargestNode;
    }


    if (!sector.sectorFileName?.includes("shadow")){        
        return 0;
    }
    return SectorZonePriority.zonePriorityLow + percentOfScreenFilledByLargestNode;
```

## Evidence

![LOD System](images/LOD.gif)

## Conclusion

This works fine, and it seems that we have everything we need to implement a decent LOD system without forking Reveal.

**_NOTE:_**
Sometimes there is a frame between unloading a detailed/shadow sector and loading a shadow/detailed sector. This has probably always been there, but is more visible now when switching between identically placed and sized sectors. It looks like the the model is "blinking".

## Next steps

- This solution doubles the amount of sectors, so it is worth investigating if every sector needs a shadow sector.

- Optimize the weighting for user experience
  - This is most likely only possible when we have an actual rough model available.