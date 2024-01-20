namespace CadRevealRvmProvider.Converters.CapVisibilityHelpers;

public static class ComparerHelper
{
    public static bool CheckOverlap(
        bool isCurrentPrimitive,
        float radius1,
        float radius2,
        float radius3,
        float tolerance
    )
    {
        if (isCurrentPrimitive)
        {
            if (radius2 + tolerance >= radius1)
            {
                return false;
            }
        }
        else
        {
            if (radius1 + tolerance >= radius3)
            {
                return false;
            }
        }

        return true;
    }
}
