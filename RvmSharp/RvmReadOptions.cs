namespace RvmSharp;

public record RvmReadOptions(
    bool PyramidToBox = false,
    bool SnoutToCylinder = false,
    //bool FacetGroupToBox = false, // will be added in next PR
    float OptimizationTolerance = 0.0001f
    );