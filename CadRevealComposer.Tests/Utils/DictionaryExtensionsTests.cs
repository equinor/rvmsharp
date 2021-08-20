namespace CadRevealComposer.Tests.Utils
{
    using CadRevealComposer.Utils;
    using NUnit.Framework;
    using System.Collections.Generic;

    public class DictionaryExtensionsTests
    {
        [Test]
        public void GetMaybeValue_WhenKeyDoesNotExist_ReturnsDefault()
        {
            var emptyDict = new Dictionary<string, object>();
            var result = emptyDict.GetMaybeValue("UnusedKey");
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetMaybeValue_WhenKeyExist_ReturnsValue()
        {
            var emptyDict = new Dictionary<string, object>();
            var myTestObject = new object();
            string myKey = "MyKey";
            emptyDict[myKey] = myTestObject;
            var result = emptyDict.GetMaybeValue(myKey);
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(myTestObject));
        }
    }
}