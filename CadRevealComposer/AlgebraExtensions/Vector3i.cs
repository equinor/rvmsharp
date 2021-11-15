namespace CadRevealComposer.AlgebraExtensions
{
    public struct Vector3i
    {
        public int X { get; init; }
        public int Y { get; init; }
        public int Z { get; init; }

        public Vector3i(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}