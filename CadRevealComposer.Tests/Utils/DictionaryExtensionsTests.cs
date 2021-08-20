namespace CadRevealComposer.Tests.Utils
{
    using CadRevealComposer.Utils;
    using NUnit.Framework;
    using System.Collections.Generic;

    public class DictionaryExtensionsTests
    {
        [Test]
        public void GetValueOrNull_WhenKeyDoesNotExist_ReturnsNull()
        {
            var emptyDict = new Dictionary<string, object>();
            var result = emptyDict.GetValueOrNull("UnusedKey");
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetValueOrNull_WhenKeyExist_ReturnsValue()
        {
            var emptyDict = new Dictionary<string, object>();
            var myTestObject = new object();
            string myKey = "MyKey";
            emptyDict[myKey] = myTestObject;
            var result = emptyDict.GetValueOrNull(myKey);
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(myTestObject));
        }
    }
}