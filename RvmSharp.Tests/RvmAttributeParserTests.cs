using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace RvmSharp.Tests
{
    [TestFixture]
    public class RvmAttributeParserTests
    {
        [Test]
        public void ParsesValidAttributeFile()
        {
            var pdmsNodesInFile = PdmsTextParser.GetAllPdmsNodesInFile(TestFileHelpers.BasicTxtAttTestFile);

            var expectedMetadata = new Dictionary<string, string>()
            {
                {"Name", "/WD1-PSUP"},
                {"RefNo", "=16821/2"},
                {"Inst", "ABC"},
                {"Type", "SITE"},
                {"PreOwnType", "SITE"},
                {"Orientation", "Y is N and Z is U"},
                {"Position", "E 0mm N 0mm U 0mm"},
                {"Description", "PSUP - HC - WEATHER DECK"},
                {"ContentType", "ASB"},
                {"DbDesc", "REMOVED"},
                {"DbName", "ASBDESZ/DESI-Z-HC"},
                {"NumbDb", "437"},
                {"Discipline", "PSUP"},
            };

            Assert.That(pdmsNodesInFile.First().MetadataDict, Is.EquivalentTo(expectedMetadata));
            Assert.That(pdmsNodesInFile, Is.Not.Null);
        }
    }
}