namespace CadRevealRvmProvider.Converters.CapVisibilityHelpers;

using RvmSharp.Primitives;

public class CapData<T>
    where T : RvmPrimitive
{
    public T Primitive { get; }
    public uint CapIndex { get; }
    public bool IsCurrentPrimitive { get; }

    public CapData(T primitive, uint capIndex, bool isCurrentPrimitive)
    {
        Primitive = primitive;
        CapIndex = capIndex;
        IsCurrentPrimitive = isCurrentPrimitive;
    }
}
