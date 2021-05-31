namespace CadRevealComposer.Utils
{
    using System.Numerics;

    public static class VectorExtensions
    {
        /// <summary>
        /// Copy the items to a new array of 3 items in XYZ order.
        /// </summary>
        public static float[] CopyToNewArray(this Vector3 vector3)
        {
            var floats = new float[3];
            vector3.CopyTo(floats);
            return floats;
        }

        /// <summary>
        /// Copy the items to an Array of 4 items in XYZW order.
        /// </summary>
        public static float[] CopyToNewArray(this Vector4 vector4)
        {
            var floats = new float[4];
            vector4.CopyTo(floats);
            return floats;
        }
    }
}
