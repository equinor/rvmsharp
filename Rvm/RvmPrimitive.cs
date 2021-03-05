namespace rvmsharp.Rvm
{
    public abstract class RvmPrimitive
    {
        public readonly uint Version;
        public readonly RvmPrimitiveKind Kind;
        public readonly float[,] Matrix;
        public readonly float[,] BoundingBoxLocal;

        public RvmPrimitive(uint version, RvmPrimitiveKind kind, float[,] matrix, float[,] bBoxLocal)
        {
            this.Version = version;
            this.Kind = kind;
            this.Matrix = matrix;
            this.BoundingBoxLocal = bBoxLocal;
        }
    }
}