namespace CadRevealRvmProvider.Converters.CapVisibilityHelpers;

using RvmSharp.Primitives;

public record CapData(RvmPrimitive Primitive, uint CapIndex, bool IsCurrentPrimitive) { }
