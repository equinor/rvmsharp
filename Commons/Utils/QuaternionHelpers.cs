namespace Commons.Utils;

using System.Numerics;

public static class QuaternionHelpers
{
    public static bool ContainsInfiniteValue(Quaternion quaternion)
    {
        return (
            !float.IsFinite(quaternion.X)
            || !float.IsFinite(quaternion.Y)
            || !float.IsFinite(quaternion.Z)
            || !float.IsFinite(quaternion.W)
        );
    }
}
