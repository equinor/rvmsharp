namespace rvmsharp.Rvm
{
    public class RvmColor
    {
        public readonly uint colorKind;
        public readonly uint colorIndex;
        public readonly byte[] rgb;

        public RvmColor(uint colorKind, uint colorIndex, byte[] rgb)
        {
            this.colorKind = colorKind;
            this.colorIndex = colorIndex;
            this.rgb = rgb;
        }
    }
}