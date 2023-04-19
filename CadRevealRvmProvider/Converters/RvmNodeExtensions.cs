namespace CadRevealRvmProvider.Converters;

using RvmSharp.Primitives;
using System.Drawing;

public static class RvmNodeExtensions
{
    public static Color GetColor(this RvmNode container)
    {
        if (PdmsColors.TryGetColorByCode(container.MaterialId, out var color))
        {
            return color;
        }

        // Fallback color is arbitrarily chosen
        return Color.Magenta;
    }
}
