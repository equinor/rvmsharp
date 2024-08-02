namespace CadRevealRvmProvider;

using CadRevealComposer.Utils;

public class FailedPrimitivesLogObject
{
    public FailedConversionCases FailedBoxes = new("boxes");
    public FailedConversionCases FailedEllipticalDishes = new("elliptical disches");
    public FailedConversionCases FailedPyramids = new("pyramids");
    public FailedConversionCases FailedSpheres = new("spheres");
    public FailedConversionCases FailedSphericalDishes = new("spherical dishes");
    public FailedConversionCases FailedCylinders = new("cylinders");
    public FailedConversionCases FailedCircularToruses = new("circular toruses");
    public FailedConversionCases FailedSnouts = new("snouts");
    public FailedConversionCases FailedRectangularTorus = new("rectangular toruses");

    public struct FailedConversionCases
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
