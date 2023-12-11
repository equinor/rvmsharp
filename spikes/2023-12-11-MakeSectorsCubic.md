# Make every sector cubic

## [Reference branch](https://github.com/equinor/rvmsharp/tree/Spike/MakeEverySectorCubic)

## Goal

Make every sector cubic by calculating and applying a cubic representation of all bounding boxes.
This is done to avoid making unfavorable sector shapes such as oblong sectors, which could enhance user experience.

## Method

In the CreateSector method, return an InternalSector with a cubed subtreeBoundingBox.
The cubification is made by increasing the length of the axis to be equal the length of the longest axis.

## Results

#### Huldra on master:
![Huldra master](https://github.com/equinor/rvmsharp/assets/141636529/1926d8c0-a2b0-4172-8fdc-5b6c4b84d040)


#### Huldra on Spike/MakeEverySectorCubic:
![Branch](https://github.com/equinor/rvmsharp/assets/141636529/74d508e5-743e-4aa9-bd17-212fe3805c70)

The percieved performance was neither worse nor better.
All bounding boxes are equally large or larger than the original bounding box, which follows from the method. Some bounding boxes are much larger.

## Conclusion

The improvements in user experience is negative or at best not satisfactory, and should not be included.

## Next steps
- Investigate if the cubification can be implemented on some bounding boxes instead of all.
Example: If the length increase in any axis is less than a fixed parameter, then cubify the bounding box.
