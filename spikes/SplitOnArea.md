# Sector split on area

## [Reference branch](https://github.com/equinor/rvmsharp/tree/Spike/AreasAsSector)

## Goal

Try to split sectors on the area code to improve user experience. The user experience can be improved by prioritizing the drawing of geometries.

## Method

Start by grouping the nodes on the area of the first geometry for each node. Then loop over each group and split the sectors recursively. Make a build on Huldra and document the findings.

## Results
#### Table of sector counts on Huldra
| Huldra        | Sector count |
| -----------   | ------------ |
| Master        | 189          |
| SplitOnArea   | 359          |



#### Huldra on master:
![Huldra master](https://github.com/equinor/rvmsharp/assets/141636529/07edffa5-1fd1-4a18-b2e0-af92d1d266b4)


#### Huldra on Spike/AreasAsSplit:
![image](https://github.com/equinor/rvmsharp/assets/141636529/7a93bc14-1396-41a9-8da4-a0216a1a9cb9)


There are over double the amount of sectors and the sectors are overlapping in Huldra. Additionally, the percieved performance was worse.

Melkøya and Johan Sverdrup was also investigated.

A common theme for all plants is that some areas have outlier geometries, resulting in large bounding boxes. 

## Conclusion

This improvements in user experience is negative or best case not satisfactory, and should not be included.

## Next steps

- Find a solution to manage the outliers in the areas to reduce the size of the bounding boxes
