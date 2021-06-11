namespace CadRevealComposer.Tests.Primitives
{
    using CadRevealComposer.Primitives;
    using Newtonsoft.Json;
    using NUnit.Framework;
    using System;
    using System.Linq;
    using System.Reflection;

    [TestFixture]
    public class PrimitiveI3dAttributeTests
    {
        [Test]
        public void CheckAllPrimitivesContainsOnlyI3dfProperties()
        {
            var inheritedTypes = Assembly.GetAssembly(typeof(APrimitive))?.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(APrimitive)));
            
            foreach (Type type in inheritedTypes)
            {
                var i3dfPropertiesCount = type.GetProperties().Count(p => p.GetCustomAttributes(true)
                    .Any(a => a is I3dfAttribute or JsonIgnoreAttribute));
                var totalPropertiesCount = type.GetProperties().Length;
                
                Assert.That(i3dfPropertiesCount, Is.EqualTo(totalPropertiesCount));
            }
        }
        
    }
}