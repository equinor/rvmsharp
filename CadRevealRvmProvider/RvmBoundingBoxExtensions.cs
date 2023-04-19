namespace CadRevealRvmProvider;

using CadRevealComposer;
using RvmSharp.Primitives;

public static class RvmBoundingBoxExtensions
{
    public static BoundingBox ToCadRevealBoundingBox(this RvmBoundingBox box)
    {
        return new BoundingBox(box.Min, box.Max);
    }
}