namespace rvmsharp.Rvm
{
    internal class RvmPyramid : RvmPrimitive
    {
        private float bottomX;
        private float bottomY;
        private float topX;
        private float topY;
        private float offsetX;
        private float offsetY;
        private float height;

        public RvmPyramid(uint version, RvmPrimitiveKind kind, float[,] matrix, float[,] bBoxLocal,
            float bottomX, float bottomY, float topX, float topY, float offsetX, float offsetY, float height) 
            : base(version, kind, matrix, bBoxLocal)
        {
            this.bottomX = bottomX;
            this.bottomY = bottomY;
            this.topX = topX;
            this.topY = topY;
            this.offsetX = offsetX;
            this.offsetY = offsetY;
            this.height = height;
        }
    }
}