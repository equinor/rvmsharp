namespace RvmSharp.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class RvmParserTests
    {
        [Test]
        public void CanReadBasicRvmFile()
        {
            using var rvmFile = TestFileHelpers.GetTestfile(TestFileHelpers.BasicRvmTestFile);

            var rvm = RvmParser.ReadRvm(rvmFile);

            Assert.That(rvm, Is.Not.Null);
            Assert.That(rvm.Header.Date, Is.EqualTo("Mon Dec 28 16:55:23 2020"));
            Assert.That(rvm.Header.Encoding, Is.EqualTo("Unicode UTF-8"));
            Assert.That(rvm.Header.Info, Is.EqualTo("AVEVA Everything3D Design Mk2.1.0.25[Z21025-12]  (WINDOWS-NT 6.3)  (25 Feb 2020 : 17:59)"));
            Assert.That(rvm.Header.Note, Is.EqualTo("Level 1 to 6"));
            Assert.That(rvm.Header.User, Is.EqualTo("f_pdmsbatch@WS3208"));
            Assert.That(rvm.Header.Version, Is.EqualTo(2));

            rvm.AttachAttributes(TestFileHelpers.BasicTxtAttTestFile);
        }
    }
}
