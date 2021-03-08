namespace rvmsharp.Rvm
{
    public class RvmColor
    {
        public readonly uint ColorKind;
        public readonly uint ColorIndex;
        public readonly byte[] Rgb;

        public RvmColor(uint colorKind, uint colorIndex, byte[] rgb)
        {
            this.ColorKind = colorKind;
            this.ColorIndex = colorIndex;
            this.Rgb = rgb;
        }
    }
}