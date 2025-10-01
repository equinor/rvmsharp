namespace CadRevealFbxProvider.Tests
{
    public static class HelperFunctions
    {
        public static void AssertInnerExceptionType<T>(Exception? exc)
        {
            Assert.That(exc, Is.Not.Null);

            Assert.That(
                exc,
                Is.InstanceOf<T>().Or.InnerException.InstanceOf<T>(),
                "Neither the exception nor its inner exception is of type ScaffoldingAttributeParsingException"
            );
        }
    }
}
