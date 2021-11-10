namespace RvmSharp.Tests
{
    using Ben.Collections.Specialized;
    using JetBrains.dotMemoryUnit;
    using System;
    using NUnit.Framework;
    using System.Collections.Generic;
    using System.Linq;
    using System.Collections.Immutable;

    [TestFixture]
    public class RvmAttributeParserTests
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
                ImmutableList<string>.Empty.Add("Name").Add("Position"),
                InternPool.Shared);

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
            Assert.That(pdmsNodesInFile, Is.Not.Null);
        }

        [DotMemoryUnit(CollectAllocations = true)]
        [Test]
        [Explicit("Must be run under Jetbrains dotMemory Unit")]
        public void AttributesStringDeduplication()
        {
            const int stringAllocationsByLineParser = 81_827;

            var memoryCheckPointA = dotMemory.Check();

            var stringInternPool = new InternPool(30_000, int.MaxValue);
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
                new FakeInternPoolWithoutInterning());

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

        /// <summary>
        /// Intern pool that doesn't intern at all.
        /// </summary>
        private sealed class FakeInternPoolWithoutInterning : IInternPool
        {
            public bool Contains(string item) => throw new NotImplementedException();
            public string Intern(ReadOnlySpan<char> value) => value.ToString();
            public string? Intern(string? value) => throw new NotImplementedException();
            public string InternAscii(ReadOnlySpan<byte> asciiValue) => throw new NotImplementedException();
            public string InternUtf8(ReadOnlySpan<byte> utf8Value) => throw new NotImplementedException();
            public string Intern(char[] value) => throw new NotImplementedException();
            public string InternAscii(byte[] asciiValue) => throw new NotImplementedException();
            public string InternUtf8(byte[] utf8Value) => throw new NotImplementedException();

            public long Added { get; }
            public long Considered { get; }
            public int Count { get; }
            public long Deduped { get; }
        }
    }
}
