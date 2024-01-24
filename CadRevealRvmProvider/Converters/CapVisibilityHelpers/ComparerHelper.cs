namespace CadRevealRvmProvider.Converters.CapVisibilityHelpers;

public static class ComparerHelper
{
    public static bool IsVisible(
        bool checkFirstPrimitive,
        float firstPrimitiveRadius,
        float secondPrimitiveSemiMinorRadius,
        float secondPrimitiveSemiMajorRadius,
        float tolerance
    )
    {
        if (checkFirstPrimitive)
        {
            if (secondPrimitiveSemiMinorRadius + tolerance >= firstPrimitiveRadius)
            {
                return false;
            }
        }
        else
        {
            if (firstPrimitiveRadius + tolerance >= secondPrimitiveSemiMajorRadius)
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsVisible(
        bool checkFirstPrimitive,
        float firstPrimitiveRadius,
        float secondPrimitiveSemiMinorRadius,
        float tolerance
    )
    {
        if (checkFirstPrimitive)
        {
            if (secondPrimitiveSemiMinorRadius + tolerance >= firstPrimitiveRadius)
            {
                return false;
            }
        }
        else
        {
            if (firstPrimitiveRadius + tolerance >= secondPrimitiveSemiMinorRadius)
            {
                return false;
            }
        }

        return true;
    }
}
