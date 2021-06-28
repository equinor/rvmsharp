namespace CadRevealComposer.Utils
{
    using System;

    public static class FloatExtensions
    {
        /// <summary>
        /// Most our measurements are in meters. So this 0.00001 value gives us 1 micrometer leeway (usually the floats start having trouble here anyway).
        /// </summary>
        private const decimal NearlyEqualsDefaultTolerance = 0.00001m; // Using decimal here to avoid the "default" inspector value of 0.00001D -> 0.999E-6D

        /// <summary>
        /// Check if the floats are equal within tolerance.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="other"></param>
        /// <param name="acceptableDifference">The tolerance. Defaults to <see cref="NearlyEqualsDefaultTolerance"/></param>
        /// <returns>True if nearly equal.</returns>
        public static bool ApproximatelyEquals(this float self, float other, double acceptableDifference = (double)NearlyEqualsDefaultTolerance)
            => Math.Abs(self - other) < acceptableDifference;
    }
}