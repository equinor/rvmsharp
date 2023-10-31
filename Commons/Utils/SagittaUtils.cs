namespace Commons.Utils;

using System;

public static class SagittaUtils
{
    private const int MinSamples = 3;
    private const int MaxSamples = 100;

    /// <summary>
    /// Calculates the "maximum deviation" in the mesh from the "ideal" primitive.
    /// If we round a cylinder to N segment faces, this method gives us the distance from the extents of a the center
    /// of a flat face to the extents of a perfect cylinder.
    /// Arc is the "completeness" of the circle in radians
    /// See: https://en.wikipedia.org/wiki/Sagitta_(geometry)
    /// </summary>
    public static float SagittaBasedError(double arc, float radius, float scale, int segments)
    {
        var lengthOfSagitta = scale * radius * (1.0f - Math.Cos(arc / segments)); // Length of sagitta
        return (float)lengthOfSagitta;
    }

    /// <summary>
    /// Calculates the amount of segments we need to represent this primitive within a given tolerance.
    /// Arc is the "completeness" of the circle in radians
    /// </summary>
    /// <example>
    /// Example: A small cylinder with a tolerance of 0.1 might be represented with 8 sides, but a large cylinder might need 32
    /// </example>
    public static int SagittaBasedSegmentCount(double arc, float radius, float scale, float tolerance)
    {
        var maximumSagitta = tolerance;
        var samples = arc / Math.Acos(Math.Max(-1.0f, 1.0f - maximumSagitta / (scale * radius)));
        if (double.IsNaN(samples))
        {
            throw new Exception(
                $"Number of samples is calculated as NaN. Diagnostics: ({nameof(scale)}: {scale}, {nameof(arc)}: {arc}, {nameof(radius)}: {radius}, {nameof(tolerance)}: {tolerance} )"
            );
        }

        return Math.Min(MaxSamples, (int)(Math.Max(MinSamples, Math.Ceiling(samples))));
    }

    public static float CalculateSagittaTolerance(float radius)
    {
        if (radius == 0) // Some geometries doesn't have radius, just set an arbitrary default value
            return 1;

        var value = radius * 0.04f + 0.02f; // Arbitrary calculation of tolerance
        return value;
    }
}
