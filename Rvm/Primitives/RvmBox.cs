namespace rvmsharp.Rvm
{
    internal class RvmBox : RvmPrimitive
    {
        public float lengthX;
        public float lengthY;
        public float lengthZ;

        public RvmBox(uint version, RvmPrimitiveKind kind, float[,] matrix, float[,] bBoxLocal, float lengthX, float lengthY, float lengthZ)
            : base(version, kind, matrix, bBoxLocal)
        {
            this.lengthX = lengthX;
            this.lengthY = lengthY;
            this.lengthZ = lengthZ;
        }
    }
}