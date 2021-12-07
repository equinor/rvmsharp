namespace CadRevealComposer.AlgebraExtensions
{
    using System.Numerics;

    public record Bounds(Vector3 Min, Vector3 Max)
    {
        public Vector3 Size => Max - Min;
    }
}