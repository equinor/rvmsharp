namespace CadRevealComposer.Tests.Utils
{
    using CadRevealComposer.Utils;
    using NUnit.Framework;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    [TestFixture]
    public class LinqExtensionsTests
    {
        class TestDataClass
        {
        }

        [Test]
        public void WhereNotNull_WithClass_FiltersNullValues()
        {
            var sampleList = new List<TestDataClass> {new TestDataClass(), null, new TestDataClass(), null};
            var result = sampleList.WhereNotNull().ToImmutableArray();
            Assert.That(result, Has.None.Null);
            Assert.That(result, Has.Exactly(2).Items);
        }
        
        [Test]
        public void WhereNotNull_WithParallelClass_FiltersNullValues()
        {
            var sampleList = new List<TestDataClass> {new TestDataClass(), null, new TestDataClass(), null};
            var result = sampleList.AsParallel().WithExecutionMode(ParallelExecutionMode.ForceParallelism).WhereNotNull().ToImmutableArray();
            Assert.That(result, Has.None.Null);
            Assert.That(result, Has.Exactly(2).Items);
        }

        [Test]
        public void WhereNotNull_WithNullableStruct_FiltersNullValues()
        {
            var sampleList = new List<int?>
            {
                1,
                null,
                2,
                null,
                3
            };
            var result = sampleList.WhereNotNull().ToArray();

            Assert.That(result, Has.None.Null);
            Assert.That(result, Has.Exactly(3).Items);
            Assert.That(result, Is.EqualTo(new[] {1, 2, 3}));
        }
        
        [Test]
        public void WhereNotNull_WithParallelNullableStruct_FiltersNullValues()
        {
            var sampleList = new List<int?>
            {
                1,
                null,
                2,
                null,
                3
            };
            var result = sampleList.AsParallel().WithExecutionMode(ParallelExecutionMode.ForceParallelism).WhereNotNull().ToArray();

            Assert.That(result, Has.None.Null);
            Assert.That(result, Has.Exactly(3).Items);
            Assert.That(result, Is.EquivalentTo(new[] {1, 2, 3})); // Test for same elements, but not same sequence.
        }
    }
}