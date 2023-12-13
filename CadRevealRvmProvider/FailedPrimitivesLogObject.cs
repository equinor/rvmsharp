namespace CadRevealRvmProvider;

using CadRevealComposer.Utils;

public class FailedPrimitivesLogObject
{
    public FailedConversionReasons FailedCylinders = new("cylinders");
    public FailedConversionReasons FailedCircularToruses = new("circular toruses");
    public FailedConversionReasons FailedSnouts = new("snouts");
    public FailedConversionReasons FailedRectangularTorus = new("rectangular toruses");

    public struct FailedConversionReasons
    {
        public readonly string PrimitiveType;

        public uint RadiusCounter = 0;
        public uint RotationCounter = 0;
        public uint ScaleCounter = 0;

        public FailedConversionReasons(string primitiveType)
        {
            PrimitiveType = primitiveType;
        }
    }

    public void LogFailedPrimitives()
    {
        using (new TeamCityLogBlock("Failed Primitives"))
        {
            LogFailedPrimitive(FailedCylinders);
            LogFailedPrimitive(FailedSnouts);
            LogFailedPrimitive(FailedCircularToruses);
            LogFailedPrimitive(FailedRectangularTorus);
        }
    }

    private void LogFailedPrimitive(FailedConversionReasons failedConversionReasons)
    {
        if (failedConversionReasons.RadiusCounter > 0)
            Console.WriteLine(
                $"Removed {failedConversionReasons.RadiusCounter} {failedConversionReasons.PrimitiveType} because of invalid radius"
            );
        if (failedConversionReasons.RotationCounter > 0)
            Console.WriteLine(
                $"Removed {failedConversionReasons.RotationCounter} {failedConversionReasons.PrimitiveType} because of invalid rotation"
            );
        if (failedConversionReasons.ScaleCounter > 0)
            Console.WriteLine(
                $"Removed {failedConversionReasons.ScaleCounter} {failedConversionReasons.PrimitiveType} because of invalid scale"
            );

        var totalFails =
            failedConversionReasons.RadiusCounter
            + failedConversionReasons.RotationCounter
            + failedConversionReasons.ScaleCounter;
        if (totalFails > 0)
        {
            Console.WriteLine($"Removed {totalFails} {failedConversionReasons.PrimitiveType} in total");
        }
    }
}
