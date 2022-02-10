namespace RvmSharp.Tests
{
    using BatchUtils;
    using Ben.Collections.Specialized;
    using JetBrains.dotMemoryUnit;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Collections.Immutable;

    [TestFixture]
    public class PdmsTextParserTests
    {
        [Test]
        public void ParsesValidAttributeFile()
        {
            var pdmsNodesInFile = PdmsTextParser.GetAllPdmsNodesInFile(TestFileHelpers.BasicTxtAttTestFile);

            var expectedMetadata = new Dictionary<string, string>
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

        [Test]
        public void ParsesValidAttributeFileWithExcludedAttributes()
        {
            var pdmsNodesInFile = PdmsTextParser.GetAllPdmsNodesInFile(
                TestFileHelpers.BasicTxtAttTestFile,
                ImmutableList<string>.Empty.Add("Name").Add("Position"));

            var expectedMetadata = new Dictionary<string, string>
            {
                {"RefNo", "=16821/2"},
                {"Inst", "ABC"},
                {"Type", "SITE"},
                {"PreOwnType", "SITE"},
                {"Orientation", "Y is N and Z is U"},
                {"Description", "PSUP - HC - WEATHER DECK"},
                {"ContentType", "ASB"},
                {"DbDesc", "REMOVED"},
                {"DbName", "ASBDESZ/DESI-Z-HC"},
                {"NumbDb", "437"},
                {"Discipline", "PSUP"},
            };

            Assert.That(pdmsNodesInFile.First().MetadataDict, Is.EquivalentTo(expectedMetadata));
        }

        [DotMemoryUnit(CollectAllocations = true)]
        [Test]
        [Explicit("Must be run under Jetbrains dotMemory Unit")]
        public void AttributesStringDeduplication()
        {
            const int stringAllocationsByLineParser = 81_827;

            var memoryCheckPointA = dotMemory.Check();

            var stringInternPool = new BenStringInternPool(new InternPool(30_000, int.MaxValue));
            _ = PdmsTextParser.GetAllPdmsNodesInFile(
                TestFileHelpers.BasicTxtAttTestFile,
                ImmutableList<string>.Empty,
                stringInternPool);

            dotMemory.Check(memory =>
            {
                var allocatedStringObjectCount = memory
                    .GetTrafficFrom(memoryCheckPointA)
                    .Where(x => x.Type.Is<string>())
                    .AllocatedMemory
                    .ObjectsCount - stringAllocationsByLineParser;

                // REMARK: intern pool varies from time to time due to eviction mechanism
                Assert.That(allocatedStringObjectCount, Is.InRange(21_900, 21_920));
            });

            var memoryCheckPointB = dotMemory.Check();

            _ = PdmsTextParser.GetAllPdmsNodesInFile(
                TestFileHelpers.BasicTxtAttTestFile,
                ImmutableList<string>.Empty,
                new BenStringInternPool(new InternPool()));

            dotMemory.Check(memory =>
            {
                var allocatedStringObjectCount = memory
                    .GetTrafficFrom(memoryCheckPointB)
                    .Where(x => x.Type.Is<string>())
                    .AllocatedMemory
                    .ObjectsCount - stringAllocationsByLineParser;

                Assert.That(allocatedStringObjectCount, Is.EqualTo(137_614));
            });
        }

        private class BenStringInternPool : IStringInternPool
        {
            private readonly IInternPool _internPool;
            public long Considered => _internPool.Considered;
            public long Added => _internPool.Added;
            public long Deduped => _internPool.Deduped;

            public BenStringInternPool(IInternPool internPool)
            {
                _internPool = internPool;
            }

            public string Intern(ReadOnlySpan<char> key)
            {
                return _internPool.Intern(key);
            }
        }
    }
}
