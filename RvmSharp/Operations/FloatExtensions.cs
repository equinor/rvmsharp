namespace RvmSharp.Operations
{
    using System.Runtime.CompilerServices;

    public static class FloatExtensions
    {

        /// <summary>Determines whether the specified value is finite (Not NaN && Not Infinity).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFinite(this float f)
        {
            return !float.IsNaN(f) && !float.IsInfinity(f);
        }
    }
}