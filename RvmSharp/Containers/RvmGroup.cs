namespace RvmSharp.Containers
{
    public abstract class RvmGroup
    {
        public readonly uint Version;

        public RvmGroup(uint version)
        {
            this.Version = version;
        }
    }
}