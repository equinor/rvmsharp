namespace RvmSharp;

public record RvmReadOptions(
    bool PyramidToBox = false,
    bool SnoutToCylinder = false,
    bool FacetGroupToBox = false,
    float OptimizationTolerance = 0.0001f
    );