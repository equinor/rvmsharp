namespace CadRevealComposer.Operations.Converters;

using RvmSharp.Primitives;
using System.Drawing;

public static class RvmPrimitiveExtensions
{
    public static Color GetColor(RvmNode container)
    {
        if (PdmsColors.TryGetColorByCode(container.MaterialId, out var color))
        {
            return color;
        }

        // TODO: Fallback color is arbitrarily chosen. It seems we have some issue with the material mapping table, and should have had more colors.
        return Color.Magenta;
    }
}