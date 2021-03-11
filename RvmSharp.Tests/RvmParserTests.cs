// unset

using NUnit.Framework;

namespace RvmSharp.Tests
{
    using System.IO;

    [TestFixture]
    public class RvmParserTests
    {
        [Test]
        public void CanReadBasicRvmFile()
        {
            using var rvmFile = TestFileHelpers.GetTestfile(TestFileHelpers.BasicRvmTestFile);

            var rvm = RvmParser.ReadRvm(rvmFile);
            
            Assert.That(rvm, Is.Not.Null);
            Assert.That(rvm.Date, Is.EqualTo("Mon Dec 28 16:55:23 2020"));
            Assert.That(rvm.Encoding, Is.EqualTo("Unicode UTF-8"));
            Assert.That(rvm.Info, Is.EqualTo("AVEVA Everything3D Design Mk2.1.0.25[Z21025-12]  (WINDOWS-NT 6.3)  (25 Feb 2020 : 17:59)"));
            Assert.That(rvm.Note, Is.EqualTo("Level 1 to 6"));
            Assert.That(rvm.User, Is.EqualTo("f_pdmsbatch@WS3208"));
            Assert.That(rvm.Version, Is.EqualTo(2));
            
            
            rvm.AttachAttributes(TestFileHelpers.BasicTxtAttTestFile);
        }
        
    }
}