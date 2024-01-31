namespace CadRevealRvmProvider.Converters.CapVisibilityHelpers;

public static class ComparerHelper
{
    /// <summary>
    ///  Checks if a cap is visible, i.e. check if there are any overlapping caps.
    /// </summary>
    /// <param name="checkFirstPrimitive"></param>
    /// <param name="firstPrimitiveRadius"></param>
    /// <param name="secondPrimitiveSemiMinorRadius"></param>
    /// <param name="secondPrimitiveSemiMajorRadius"></param>
    /// <param name="tolerance"></param>
    /// <returns>True if there are no overlapping caps. False otherwise.</returns>
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

    /// <summary>
    /// Checks if a cap is visible, i.e. check if there are any overlapping caps.
    /// </summary>
    /// <param name="checkFirstPrimitive"></param>
    /// <param name="firstPrimitiveRadius"></param>
    /// <param name="secondPrimitiveRadius"></param>
    /// <param name="tolerance"></param>
    /// <returns>True if there are no overlapping caps. False otherwise.</returns>
    public static bool IsVisible(
        bool checkFirstPrimitive,
        float firstPrimitiveRadius,
        float secondPrimitiveRadius,
        float tolerance
    )
    {
        if (checkFirstPrimitive)
        {
            if (secondPrimitiveRadius + tolerance >= firstPrimitiveRadius)
            {
                return false;
            }
        }
        else
        {
            if (firstPrimitiveRadius + tolerance >= secondPrimitiveRadius)
            {
                return false;
            }
        }

        return true;
    }
}
