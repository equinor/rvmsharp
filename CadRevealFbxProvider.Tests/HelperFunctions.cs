namespace CadRevealFbxProvider.Tests
{
    using static System.Runtime.InteropServices.JavaScript.JSType;

    public static class HelperFunctions
    {
        public static void AssertThrowsCustomScaffoldingException<T>(Delegate fn)
        {
            var exc = Assert.Catch(() => fn.DynamicInvoke());

            Assert.That(exc, Is.Not.Null);
            Assert.That(exc.InnerException, Is.Not.Null);

            // since it is delegated code, the exception will be wrapped in a TargetInvocationException,
            // we have to check the inner exception and its inner exception
            Assert.That(
                exc.InnerException.InnerException,
                Is.InstanceOf<T>().Or.InnerException.InstanceOf<T>(),
                "Neither the exception nor its inner exception is of type ScaffoldingAttributeParsingException"
            );
        }
    }
}
