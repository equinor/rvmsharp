using NUnit.Framework;

namespace CadRevealComposer.Tests.Primitives.Reflection;

using CadRevealComposer.Primitives;
using System;
using System.Linq;

[TestFixture]
public class APrimitiveReflectionHelpersTests
{
    [Test]
    public void CreatePrimitiveAttributeLookup_RetrievesExpectedProperties()
    {
        // Expected classes
        var expectedTypes = typeof(APrimitive)
            .Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(APrimitive)) && !t.IsAbstract).ToArray();
        Assert.That(expectedTypes, Is.Not.Empty);

        var allAttributeTypes = (I3dfAttribute.AttributeType[])Enum.GetValues(typeof(I3dfAttribute.AttributeType));
        Assert.That(allAttributeTypes, Is.Not.Empty);

        var result = APrimitiveReflectionHelpers.CreatePrimitiveAttributeLookup();

        Assert.That(result.Keys, Is.EquivalentTo(expectedTypes));

        // Assert that all attribute types are found. This will fail if you add a new Attribute kind and never use it.
        Assert.That(result.Values.SelectMany(x => x.Keys).Distinct(), Is.EquivalentTo(allAttributeTypes));

        // This test should be further improved if anything fails.
    }
}