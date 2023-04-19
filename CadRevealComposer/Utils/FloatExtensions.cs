namespace CadRevealComposer.Utils;

using System;

public static class FloatExtensions
{
    /// <summary>
    /// Most our measurements are in meters. So this 0.00001 value gives us 1 micrometer leeway (usually the floats start having trouble here anyway).
    /// </summary>
    private const decimal NearlyEqualsDefaultTolerance = 0.00001m; // Using decimal here to avoid the "default" inspector value of 0.00001D -> 0.999E-6D

    /// <summary>
    /// Check if the floats are equal within tolerance.
    /// Shorthand for "Math.Abs(self - other) &lt; acceptableDifference"
    /// </summary>
    /// <param name="self"></param>
    /// <param name="other"></param>
    /// <param name="acceptableDifference">The tolerance. Defaults to <see cref="NearlyEqualsDefaultTolerance"/></param>
    /// <returns>True if nearly equal.</returns>
    public static bool ApproximatelyEquals(
        this float self,
        float other,
        double acceptableDifference = (double)NearlyEqualsDefaultTolerance
    ) => Math.Abs(self - other) < acceptableDifference;

    /// <summary>
    /// Check if the floats are equal when rounded to x decimals.
    ///
    /// This avoids an issue with <see cref="ApproximatelyEquals"/> where  (1.10 == 1.15) and (1.15 == 1.20). But (1.10 != 1.20).
    /// that issue can cause an infinite drift of the equality.
    ///
    /// Significant Decimals equals is also called bucketing
    /// The issue with this method is that two nearly equal numbers can be placed in different buckets.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="other"></param>
    /// <param name="decimals">Number of digits following the decimal point, zero means round to int.</param>
    /// <returns>True if nearly equal.</returns>
    public static bool SignificantDecimalsEquals(this float self, float other, int decimals)
    {
        // ReSharper disable once CompareOfFloatsByEqualityOperator -- Equality comparing after rounding should be safe
        return MathF.Round(self, decimals) == MathF.Round(other, decimals);
    }
}
