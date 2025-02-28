namespace CadRevealRvmProvider;

using CadRevealComposer.Utils;

/// <summary>
/// Helps track failed conversions
/// </summary>
public class FailedPrimitivesLogObject
{
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    public readonly FailedConversionCases FailedBoxes = new("boxes");
    public readonly FailedConversionCases FailedEllipticalDishes = new("elliptical dishes");
    public readonly FailedConversionCases FailedPyramids = new("pyramids");
    public readonly FailedConversionCases FailedSpheres = new("spheres");
    public readonly FailedConversionCases FailedSphericalDishes = new("spherical dishes");
    public readonly FailedConversionCases FailedCylinders = new("cylinders");
    public readonly FailedConversionCases FailedCircularToruses = new("circular toruses");
    public readonly FailedConversionCases FailedSnouts = new("snouts");
    public readonly FailedConversionCases FailedRectangularTorus = new("rectangular toruses");

    // ReSharper enable ArrangeObjectCreationWhenTypeEvident

    public record FailedConversionCases
    {
        public readonly string PrimitiveType;

        public uint RotationCounter = 0;
        public uint ScaleCounter = 0;
        public uint SizeCounter = 0;

        public FailedConversionCases(string primitiveType)
        {
            PrimitiveType = primitiveType;
        }
    }

    public void LogFailedPrimitives()
    {
        using (new TeamCityLogBlock("Failed Primitives"))
        {
            LogFailedPrimitive(FailedBoxes);
            LogFailedPrimitive(FailedEllipticalDishes);
            LogFailedPrimitive(FailedPyramids);
            LogFailedPrimitive(FailedSpheres);
            LogFailedPrimitive(FailedSphericalDishes);
            LogFailedPrimitive(FailedCylinders);
            LogFailedPrimitive(FailedSnouts);
            LogFailedPrimitive(FailedCircularToruses);
            LogFailedPrimitive(FailedRectangularTorus);
        }
    }

    private static void LogFailedPrimitive(FailedConversionCases failedConversionReasons)
    {
        if (failedConversionReasons.RotationCounter > 0)
            Console.WriteLine(
                $"Removed {failedConversionReasons.RotationCounter} {failedConversionReasons.PrimitiveType} because of invalid rotation"
            );
        if (failedConversionReasons.ScaleCounter > 0)
            Console.WriteLine(
                $"Removed {failedConversionReasons.ScaleCounter} {failedConversionReasons.PrimitiveType} because of invalid scale"
            );
        if (failedConversionReasons.SizeCounter > 0)
            Console.WriteLine(
                $"Removed {failedConversionReasons.SizeCounter} {failedConversionReasons.PrimitiveType} because of invalid size (height, length, or radius)"
            );

        var totalFails =
            failedConversionReasons.RotationCounter
            + failedConversionReasons.ScaleCounter
            + failedConversionReasons.SizeCounter;
        if (totalFails > 0)
        {
            Console.WriteLine($"Removed {totalFails} {failedConversionReasons.PrimitiveType} in total");
        }
    }
}
