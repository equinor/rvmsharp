namespace CadRevealComposer.Tests.Operations
{
    using CadRevealComposer.Operations;
    using NUnit.Framework;
    using RvmSharp.BatchUtils;

    [TestFixture]
    public class FacesConverterTests
    {
        [Test]
        public void ConvertSimpleMesh()
        {
            // Read rvm, get meshesh, convert to faces
            Workload.ReadRvmData()
            FacesConverter.Convert(Mesh, gr)
        }
    }
}