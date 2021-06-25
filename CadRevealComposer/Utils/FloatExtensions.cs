namespace CadRevealComposer.Utils
{
    using System;

    public static class FloatExtensions
    {
        /// <summary>
        /// Most our measurements are in meters. So this 0.00001 value gives us 1 micrometer leeway (usually the floats start having trouble here anyway).
        /// </summary>
        private const double DefaultTolerance = 0.00001f;

        /// <summary>
        /// Check if two floats are equal within a tolerance.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="tolerance">The tolerance </param>
        /// <returns>True if nearly equal.</returns>
        public static bool NearlyEquals(this float a, float b, double tolerance = DefaultTolerance)
            => Math.Abs(a - b) < tolerance;
    }
}