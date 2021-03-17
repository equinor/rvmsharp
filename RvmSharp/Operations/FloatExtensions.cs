namespace RvmSharp.Operations
{
    public static class FloatExtensions
    {
        public static bool IsFinite(this float f)
        {
            return !float.IsNaN(f) && !float.IsInfinity(f);
        }
    }
}